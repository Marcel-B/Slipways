using System.Net;
using System.Text;
using System.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Osm;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public sealed class OsmImportTests : IAsyncLifetime
{
    private static readonly Guid KelheimId = Guid.Parse("EAC96A93-FD47-4A9A-8771-0005805B9655");
    private static readonly Guid SaalMarinaId = Guid.Parse("4CFBA36B-39A9-4F63-B155-32D6279DE801");
    private static readonly Guid DonauId = Guid.Parse("D19708CA-AC42-4AD4-8D62-E85722F946F6");

    // A line along 48.0° N, so the way below (about 110 m north of it) lies on the Donau.
    private const string DonauShape = """{"type":"LineString","coordinates":[[12.0,48.0],[12.1,48.0]]}""";

    // Trimmed Overpass response: a node about 10 m from the seeded Kelheim slipway, a way without a name but with
    // an address and every tag we take, and a bare node far from any water.
    private const string SlipwaysJson = """
        {
          "version": 0.6,
          "elements": [
            { "type": "node", "id": 1001, "lat": 48.93165, "lon": 11.99140,
              "tags": { "leisure": "slipway", "name": "Slipanlage Jachthafen", "fee": "yes" } },
            { "type": "way", "id": 2001, "center": { "lat": 48.001, "lon": 12.05 },
              "tags": { "leisure": "slipway", "addr:street": "Uferweg", "addr:housenumber": "3",
                "addr:postcode": "12345", "addr:city": "Teststadt", "operator": "Stadt Teststadt",
                "website": "https://teststadt.example/slip", "fee": "no", "access": "yes",
                "opening_hours": "24/7", "surface": "concrete" } },
            { "type": "node", "id": 1002, "lat": 54.5, "lon": 13.0, "tags": { "leisure": "slipway" } }
          ]
        }
        """;

    // A node about 15 m from the seeded Saal marina and a new marina.
    private const string MarinasJson = """
        {
          "version": 0.6,
          "elements": [
            { "type": "node", "id": 3001, "lat": 48.90940, "lon": 11.92680,
              "tags": { "leisure": "marina", "name": "Yachthafen Saal" } },
            { "type": "way", "id": 4001, "center": { "lat": 53.0, "lon": 8.0 },
              "tags": { "leisure": "marina", "name": "Testhafen", "website": "https://testhafen.example",
                "contact:phone": "+49 4401 12345", "email": "hafen@testhafen.example" } }
          ]
        }
        """;

    private const string EmptyJson = """{ "version": 0.6, "elements": [] }""";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly TimeProvider _time = new FixedTime(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
    private readonly StubHandler _overpass = new();
    private SlipwaysDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new SlipwaysDbContext(new DbContextOptionsBuilder<SlipwaysDbContext>().UseSqlite(_connection).Options);
        await _db.Database.MigrateAsync();
        await SeedData.SeedAsync(_db);

        var donau = await _db.Waters.SingleAsync(w => w.Id == DonauId);
        donau.Shape = DonauShape;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        _overpass.Respond("DE", "slipway", SlipwaysJson);
        _overpass.Respond("DE", "marina", MarinasJson);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Adds_elements_with_their_tags_as_osm_places()
    {
        var slipwaysBefore = await _db.Slipways.CountAsync(TestContext.Current.CancellationToken);

        var result = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(new OsmImportCounts(Added: 2, Updated: 0, Linked: 1), result.Slipways);
        Assert.Equal(new OsmImportCounts(Added: 1, Updated: 0, Linked: 1), result.Marinas);
        Assert.Equal(slipwaysBefore + 2, await _db.Slipways.CountAsync(TestContext.Current.CancellationToken));

        var slipway = await FindSlipwayAsync("way", 2001);
        Assert.Equal(Sources.Osm, slipway.Source);
        Assert.Equal("Slipanlage Teststadt", slipway.Name);
        Assert.Equal((48.001, 12.05), (slipway.Latitude, slipway.Longitude));
        Assert.Equal(("Uferweg 3", "12345", "Teststadt", "DE"), (slipway.Street, slipway.PostalCode, slipway.City, slipway.Country));
        Assert.Equal("https://teststadt.example/slip", slipway.Url);
        Assert.Equal(("Stadt Teststadt", "no", "yes", "24/7", "concrete"),
            (slipway.Operator, slipway.Fee, slipway.Access, slipway.OpeningHours, slipway.Surface));
        Assert.Equal(DonauId, slipway.WaterId);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, slipway.Created);
        Assert.Null(slipway.Updated);

        var bare = await FindSlipwayAsync("node", 1002);
        Assert.Equal("Slipanlage", bare.Name);
        Assert.Null(bare.WaterId);

        var marina = await _db.Marinas.AsNoTracking().SingleAsync(m => m.OsmType == "way" && m.OsmId == 4001, TestContext.Current.CancellationToken);
        Assert.Equal((Sources.Osm, "Testhafen", "https://testhafen.example"), (marina.Source, marina.Name, marina.Url));
        Assert.Equal(("+49 4401 12345", "hafen@testhafen.example"), (marina.Phone, marina.Email));
    }

    [Fact]
    public async Task Records_its_success_for_the_schedule()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        var state = await _db.JobStates.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(("osm:DE", _time.GetUtcNow().UtcDateTime, (DateTime?)null), (state.Name, state.LastSuccess, state.LastFailure));
    }

    [Fact]
    public async Task Links_manual_places_closer_than_50_m_instead_of_adding_them()
    {
        var kelheimBefore = await _db.Slipways.AsNoTracking().Include(s => s.Extras).SingleAsync(s => s.Id == KelheimId, TestContext.Current.CancellationToken);
        var marinaBefore = await _db.Marinas.AsNoTracking().SingleAsync(m => m.Id == SaalMarinaId, TestContext.Current.CancellationToken);

        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        var kelheim = await _db.Slipways.AsNoTracking().Include(s => s.Extras).SingleAsync(s => s.Id == KelheimId, TestContext.Current.CancellationToken);
        Assert.Equal((Sources.Manual, "node", 1001L), (kelheim.Source, kelheim.OsmType, kelheim.OsmId));
        AssertManualFieldsUnchanged(kelheimBefore, kelheim);
        Assert.Equal(kelheimBefore.Name, kelheim.Name);
        Assert.Null(kelheim.Fee);
        Assert.Equal(1, await _db.Slipways.CountAsync(s => s.OsmType == "node" && s.OsmId == 1001, TestContext.Current.CancellationToken));

        var marina = await _db.Marinas.AsNoTracking().SingleAsync(m => m.Id == SaalMarinaId, TestContext.Current.CancellationToken);
        Assert.Equal((Sources.Manual, "node", 3001L), (marina.Source, marina.OsmType, marina.OsmId));
        Assert.Equal(marinaBefore.Name, marina.Name);
    }

    [Fact]
    public async Task Updates_osm_fields_by_element_and_keeps_manual_ones()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        // Someone rates the imported slipway and adds an extra; the linked manual slipway keeps its own data.
        var imported = await _db.Slipways.SingleAsync(s => s.OsmType == "way" && s.OsmId == 2001, TestContext.Current.CancellationToken);
        imported.Comment = "Gut erreichbar";
        imported.Rating = 4;
        imported.Costs = 5;
        imported.Pro = "Breit";
        imported.Contra = "Steil";
        imported.Extras.Add(await _db.Extras.FirstAsync(TestContext.Current.CancellationToken));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _db.ChangeTracker.Clear();
        var importedBefore = await _db.Slipways.AsNoTracking().Include(s => s.Extras).SingleAsync(s => s.Id == imported.Id, TestContext.Current.CancellationToken);
        var kelheimBefore = await _db.Slipways.AsNoTracking().Include(s => s.Extras).SingleAsync(s => s.Id == KelheimId, TestContext.Current.CancellationToken);

        _overpass.Respond("DE", "slipway", SlipwaysJson
            .Replace("\"leisure\": \"slipway\", \"addr:street\"", "\"leisure\": \"slipway\", \"name\": \"Slip am Uferweg\", \"addr:street\"")
            .Replace("\"fee\": \"no\", ", "")
            .Replace("\"lat\": 48.93165", "\"lat\": 48.93160")
            .Replace("\"name\": \"Slipanlage Jachthafen\"", "\"name\": \"Neuer Name\""));
        var result = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(new OsmImportCounts(Added: 0, Updated: 1, Linked: 0), result.Slipways);
        var updated = await _db.Slipways.AsNoTracking().Include(s => s.Extras).SingleAsync(s => s.Id == imported.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Slip am Uferweg", updated.Name);
        Assert.Null(updated.Fee);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, updated.Updated);
        AssertManualFieldsUnchanged(importedBefore, updated);

        var kelheim = await _db.Slipways.AsNoTracking().Include(s => s.Extras).SingleAsync(s => s.Id == KelheimId, TestContext.Current.CancellationToken);
        Assert.Equal(kelheimBefore.Name, kelheim.Name);
        Assert.Equal((kelheimBefore.Latitude, kelheimBefore.Longitude), (kelheim.Latitude, kelheim.Longitude));
        AssertManualFieldsUnchanged(kelheimBefore, kelheim);
    }

    [Fact]
    public async Task Second_run_with_same_data_changes_nothing()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);
        var before = await _db.Slipways.AsNoTracking().OrderBy(s => s.Id).Select(s => new { s.Id, s.Updated }).ToListAsync(TestContext.Current.CancellationToken);

        var result = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(new OsmImportCounts(0, 0, 0), result.Slipways);
        Assert.Equal(new OsmImportCounts(0, 0, 0), result.Marinas);
        Assert.Empty(result.Missing);
        var after = await _db.Slipways.AsNoTracking().OrderBy(s => s.Id).Select(s => new { s.Id, s.Updated }).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Keeps_and_reports_places_that_are_gone_from_osm()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        _overpass.Respond("DE", "slipway", EmptyJson);
        var result = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(["node/1001", "node/1002", "way/2001"], result.Missing.Order());
        Assert.True(await _db.Slipways.AnyAsync(s => s.OsmType == "node" && s.OsmId == 1002, TestContext.Current.CancellationToken));
        Assert.True(await _db.Slipways.AnyAsync(s => s.Id == KelheimId && s.OsmId == 1001, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Skips_overseas_territories()
    {
        // The area of France also covers Réunion and Martinique.
        _overpass.Respond("FR", "marina", """
            {
              "version": 0.6,
              "elements": [
                { "type": "node", "id": 5001, "lat": -20.94, "lon": 55.29, "tags": { "leisure": "marina", "name": "Port de Saint-Denis" } },
                { "type": "node", "id": 5002, "lat": 14.6, "lon": -61.07, "tags": { "leisure": "marina", "name": "Marina du Marin" } },
                { "type": "node", "id": 5003, "lat": 43.29, "lon": 5.37, "tags": { "leisure": "marina", "name": "Vieux-Port" } }
              ]
            }
            """);

        var result = await CreateImport().RunAsync("FR", TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Marinas.Added);
        Assert.True(await _db.Marinas.AnyAsync(m => m.OsmId == 5003, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Queries_both_kinds_of_one_country_one_after_another_with_a_timeout()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(["DE slipway", "DE marina"], _overpass.Queries.Select(q => $"{q.Country} {q.Leisure}"));
        Assert.All(_overpass.Queries, q => Assert.Contains("[timeout:180]", q.Text));
        Assert.All(_overpass.Queries, q => Assert.EndsWith("out center tags;", q.Text));
        Assert.Equal(1, _overpass.MaxConcurrent);
    }

    [Fact]
    public async Task Leaves_elements_on_a_border_to_the_country_that_found_them_first()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        // Austria's area also contains the slipways, but with other tags; Germany no longer has them.
        _overpass.Respond("AT", "slipway", SlipwaysJson.Replace("\"name\": \"Slipanlage Jachthafen\"", "\"name\": \"Slip Österreich\""));
        var austria = await CreateImport().RunAsync("AT", TestContext.Current.CancellationToken);
        _overpass.Respond("DE", "slipway", EmptyJson);
        var germany = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(new OsmImportCounts(0, 0, 0), austria.Slipways);
        // Only the places Germany found count as missing there, not Austria's or those of other countries.
        Assert.Empty(austria.Missing);
        Assert.Equal(["node/1001", "node/1002", "way/2001"], germany.Missing.Order());
        Assert.Equal("DE", (await FindSlipwayAsync("way", 2001)).OsmCountry);
        Assert.Equal("DE", (await FindSlipwayAsync("node", 1001)).OsmCountry);
    }

    [Fact]
    public async Task Gives_places_from_before_the_import_by_country_their_country()
    {
        await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);
        await _db.Slipways.ExecuteUpdateAsync(s => s.SetProperty(x => x.OsmCountry, (string?)null), TestContext.Current.CancellationToken);
        var before = await FindSlipwayAsync("way", 2001);

        var result = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        var after = await FindSlipwayAsync("way", 2001);
        Assert.Equal("DE", after.OsmCountry);
        Assert.Equal(before.Updated, after.Updated);
        Assert.Equal(new OsmImportCounts(0, 0, 0), result.Slipways);
    }

    [Fact]
    public async Task Repeats_a_query_while_overpass_is_busy()
    {
        _overpass.BusyResponses = 2;

        var result = await CreateImport().RunAsync("DE", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Slipways.Added);
        Assert.Equal(4, _overpass.Queries.Count);
    }

    private static void AssertManualFieldsUnchanged(Slipway before, Slipway after)
    {
        Assert.Equal(before.Comment, after.Comment);
        Assert.Equal(before.Rating, after.Rating);
        Assert.Equal(before.Costs, after.Costs);
        Assert.Equal(before.Pro, after.Pro);
        Assert.Equal(before.Contra, after.Contra);
        Assert.Equal(before.Extras.Select(e => e.Id).Order(), after.Extras.Select(e => e.Id).Order());
        Assert.Equal(before.MarinaId, after.MarinaId);
    }

    private Task<Slipway> FindSlipwayAsync(string type, long id) =>
        _db.Slipways.AsNoTracking().SingleAsync(s => s.OsmType == type && s.OsmId == id, TestContext.Current.CancellationToken);

    private OsmImport CreateImport()
    {
        _db.ChangeTracker.Clear();
        var options = Options.Create(new OsmOptions
        {
            RequestSpacing = TimeSpan.Zero,
            BusyBackoff = TimeSpan.Zero,
        });
        var http = new HttpClient(_overpass) { BaseAddress = new Uri("https://overpass.test/api/") };
        return new OsmImport(_db, new OverpassClient(http, options, _time), options, _time);
    }

    /// <summary>Answers Overpass queries by country and leisure value; anything not set up gets no elements.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<(string Country, string Leisure), string> _responses = [];
        private int _concurrent;

        public List<(string Country, string Leisure, string Text)> Queries { get; } = [];
        public int MaxConcurrent { get; private set; }
        public int BusyResponses { get; set; }

        public void Respond(string country, string leisure, string json) => _responses[(country, leisure)] = json;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            MaxConcurrent = Math.Max(MaxConcurrent, Interlocked.Increment(ref _concurrent));
            try
            {
                await Task.Yield();
                var query = HttpUtility.ParseQueryString(await request.Content!.ReadAsStringAsync(cancellationToken))["data"]!;
                var country = query.Split("\"ISO3166-1\"=\"")[1][..2];
                var leisure = query.Split("leisure=")[1].Split(']')[0];
                Queries.Add((country, leisure, query));

                if (BusyResponses > 0)
                {
                    BusyResponses--;
                    return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responses.GetValueOrDefault((country, leisure), EmptyJson), Encoding.UTF8, "application/json"),
                };
            }
            finally
            {
                Interlocked.Decrement(ref _concurrent);
            }
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
