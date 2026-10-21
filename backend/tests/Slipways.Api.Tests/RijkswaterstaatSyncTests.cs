using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Gauges;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public sealed class RijkswaterstaatSyncTests : IAsyncLifetime
{
    private static readonly Guid DonauId = Guid.Parse("D19708CA-AC42-4AD4-8D62-E85722F946F6");

    // Stands in for a Dutch river: a line along 52.0° N.
    private const string RiverShape = """{"type":"LineString","coordinates":[[5.0,52.0],[6.0,52.0]]}""";

    // Trimmed catalogue: "lobith" (next to the river) and "noordzee" (at sea) measure the water level relative to
    // NAP; "golfhoogte" only measures waves.
    private const string CatalogueJson = """
        {
          "Succesvol": true,
          "AquoMetadataLijst": [
            { "AquoMetadata_MessageID": 1, "Compartiment": { "Code": "OW" }, "Eenheid": { "Code": "cm" },
              "Grootheid": { "Code": "WATHTE" }, "Hoedanigheid": { "Code": "NAP" }, "ProcesType": "meting" },
            { "AquoMetadata_MessageID": 2, "Compartiment": { "Code": "OW" }, "Eenheid": { "Code": "cm" },
              "Grootheid": { "Code": "H1/3" }, "Hoedanigheid": { "Code": "NVT" }, "ProcesType": "meting" },
            { "AquoMetadata_MessageID": 3, "Compartiment": { "Code": "OW" }, "Eenheid": { "Code": "cm" },
              "Grootheid": { "Code": "WATHTE" }, "Hoedanigheid": { "Code": "NAP" }, "ProcesType": "verwachting" }
          ],
          "LocatieLijst": [
            { "Locatie_MessageID": 10, "Code": "lobith", "Naam": "Lobith", "Lat": 52.001, "Lon": 5.5, "Coordinatenstelsel": "EPSG:4258" },
            { "Locatie_MessageID": 11, "Code": "noordzee", "Naam": "Noordzee", "Lat": 54.0, "Lon": 3.0, "Coordinatenstelsel": "EPSG:4258" },
            { "Locatie_MessageID": 12, "Code": "golfhoogte", "Naam": "Golfhoogte", "Lat": 52.0, "Lon": 5.6, "Coordinatenstelsel": "EPSG:4258" }
          ],
          "AquoMetadataLocatieLijst": [
            { "Locatie_MessageID": 10, "AquoMetaData_MessageID": 1 },
            { "Locatie_MessageID": 10, "AquoMetaData_MessageID": 3 },
            { "Locatie_MessageID": 11, "AquoMetaData_MessageID": 1 },
            { "Locatie_MessageID": 12, "AquoMetaData_MessageID": 2 }
          ]
        }
        """;

    // An old series that ended long ago, the current one with a newer value marked as missing (99), and a forecast.
    private const string LatestJson = """
        {
          "Succesvol": true,
          "WaarnemingenLijst": [
            { "Locatie": { "Code": "lobith" }, "AquoMetadata": { "ProcesType": "meting" },
              "MetingenLijst": [{ "Tijdstip": "1998-03-01T10:00:00.000+01:00", "Meetwaarde": { "Waarde_Numeriek": 990.0 },
                "WaarnemingMetadata": { "Kwaliteitswaardecode": "00" } }] },
            { "Locatie": { "Code": "lobith" }, "AquoMetadata": { "ProcesType": "meting" },
              "MetingenLijst": [
                { "Tijdstip": "2026-10-20T11:40:00.000+02:00", "Meetwaarde": { "Waarde_Numeriek": 812.0 },
                  "WaarnemingMetadata": { "Kwaliteitswaardecode": "00" } },
                { "Tijdstip": "2026-10-20T11:50:00.000+02:00", "Meetwaarde": { "Waarde_Numeriek": 999999999.0 },
                  "WaarnemingMetadata": { "Kwaliteitswaardecode": "99" } }] },
            { "Locatie": { "Code": "lobith" }, "AquoMetadata": { "ProcesType": "verwachting" },
              "MetingenLijst": [{ "Tijdstip": "2026-10-20T14:00:00.000+02:00", "Meetwaarde": { "Waarde_Numeriek": 700.0 },
                "WaarnemingMetadata": { "Kwaliteitswaardecode": "00" } }] }
          ]
        }
        """;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly TimeProvider _time = new FixedTime(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
    private readonly StubHandler _rijkswaterstaat = new();
    private SlipwaysDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new SlipwaysDbContext(new DbContextOptionsBuilder<SlipwaysDbContext>().UseSqlite(_connection).Options);
        await _db.Database.MigrateAsync();
        await SeedData.SeedAsync(_db);

        var river = await _db.Waters.SingleAsync(w => w.Id == DonauId);
        river.Shape = RiverShape;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Adds_water_level_locations_on_a_known_water_with_their_latest_level()
    {
        var result = await CreateSync().RunAsync(refreshStations: true, TestContext.Current.CancellationToken);

        Assert.Equal(new RijkswaterstaatSyncResult(StationsAdded: 1, LocationsWithoutWater: 1, WaterLevelsUpdated: 1), result);
        var lobith = await _db.Stations.AsNoTracking().SingleAsync(s => s.ExternalCode == "lobith", TestContext.Current.CancellationToken);
        Assert.Equal(("Lobith", RijkswaterstaatSync.Agency, DonauId), (lobith.Name, lobith.Agency, lobith.WaterId));
        Assert.Equal((52.001, 5.5), (lobith.Latitude, lobith.Longitude));
        Assert.Equal(812.0, lobith.WaterLevel);
        Assert.Equal(new DateTime(2026, 10, 20, 9, 40, 0, DateTimeKind.Utc), lobith.WaterLevelMeasuredAt);
        Assert.Null(lobith.WaterLevelState);
        Assert.False(await _db.Stations.AnyAsync(s => s.ExternalCode == "noordzee" || s.ExternalCode == "golfhoogte", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Asks_for_water_levels_relative_to_nap_of_known_stations_only()
    {
        await CreateSync().RunAsync(refreshStations: true, TestContext.Current.CancellationToken);
        _rijkswaterstaat.Requests.Clear();

        var result = await CreateSync().RunAsync(refreshStations: false, TestContext.Current.CancellationToken);

        var request = Assert.Single(_rijkswaterstaat.Requests);
        Assert.Equal("/ONLINEWAARNEMINGENSERVICES/OphalenLaatsteWaarnemingen", request.Path);
        var body = JsonNode.Parse(request.Body)!;
        Assert.Equal(["lobith"], body["LocatieLijst"]!.AsArray().Select(l => (string?)l!["Code"]));
        var metadata = body["AquoPlusWaarnemingMetadataLijst"]![0]!["AquoMetadata"]!;
        Assert.Equal(("WATHTE", "NAP", "OW"), ((string?)metadata["Grootheid"]!["Code"], (string?)metadata["Hoedanigheid"]!["Code"], (string?)metadata["Compartiment"]!["Code"]));
        // The same measurement as before changes nothing.
        Assert.Equal(new RijkswaterstaatSyncResult(0, 0, 0), result);
    }

    private RijkswaterstaatSync CreateSync()
    {
        _db.ChangeTracker.Clear();
        var options = Options.Create(new RijkswaterstaatOptions { RequestSpacing = TimeSpan.Zero });
        var http = new HttpClient(_rijkswaterstaat) { BaseAddress = new Uri("https://rijkswaterstaat.test/") };
        return new RijkswaterstaatSync(_db, new RijkswaterstaatClient(http, options, _time), options, _time);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<(string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add((path, await request.Content!.ReadAsStringAsync(cancellationToken)));
            var json = path.EndsWith("/OphalenCatalogus", StringComparison.Ordinal) ? CatalogueJson : LatestJson;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
