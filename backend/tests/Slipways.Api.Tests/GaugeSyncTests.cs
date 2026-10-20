using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Slipways.Infrastructure.Gauges;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public sealed class GaugeSyncTests : IAsyncLifetime
{
    private static readonly Guid JadeStationUuid = Guid.Parse("8e4f6a2c-1b3d-4c5e-9f70-112233445566");
    private static readonly Guid NewStationUuid = Guid.Parse("0a1b2c3d-4e5f-4061-8728-394a5b6c7d8e");

    // Trimmed PEGELONLINE response: a seeded station (matched by number), a new station on a new water, one without coordinates.
    private const string StationsJson = """
        [
          {
            "uuid": "8e4f6a2c-1b3d-4c5e-9f70-112233445566", "number": "9440030",
            "shortname": "WHV NEUER VORHAFEN", "longname": "WHV NEUER VORHAFEN",
            "km": 4.053, "agency": "WILHELMSHAVEN", "longitude": 8.160729, "latitude": 53.530306,
            "water": { "shortname": "JADE", "longname": "JADE" },
            "timeseries": [{ "shortname": "W", "unit": "cm",
              "currentMeasurement": { "timestamp": "2026-10-20T11:45:00+02:00", "value": 512.0, "stateMnwMhw": "normal" } }]
          },
          {
            "uuid": "0a1b2c3d-4e5f-4061-8728-394a5b6c7d8e", "number": "99999999",
            "shortname": "NEUDORF", "longname": "NEUDORF",
            "km": 12.5, "agency": "TESTAMT", "longitude": 10.0, "latitude": 52.0,
            "water": { "shortname": "TESTFLUSS", "longname": "TESTFLUSS" },
            "timeseries": [{ "shortname": "W", "unit": "cm",
              "currentMeasurement": { "timestamp": "2026-10-20T11:45:00+02:00", "value": 117.0, "stateMnwMhw": "low" } }]
          },
          {
            "uuid": "ffffffff-0000-4000-8000-000000000000", "number": "88888888",
            "shortname": "OHNE ORT", "longname": "OHNE ORT", "km": 1.0, "agency": "TESTAMT",
            "water": { "shortname": "TESTFLUSS", "longname": "TESTFLUSS" }
          }
        ]
        """;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly TimeProvider _time = new FixedTime(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
    private SlipwaysDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new SlipwaysDbContext(new DbContextOptionsBuilder<SlipwaysDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(_db);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Updates_water_level_of_seeded_station_matched_by_number()
    {
        var result = await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        var station = await _db.Stations.AsNoTracking().SingleAsync(s => s.Number == "9440030", TestContext.Current.CancellationToken);
        Assert.Equal(Guid.Parse("F77317D9-654F-4F51-925E-004C592049DA"), station.Id);
        Assert.Equal(JadeStationUuid, station.ExternalId);
        Assert.Equal(512.0, station.WaterLevel);
        Assert.Equal(new DateTime(2026, 10, 20, 9, 45, 0, DateTimeKind.Utc), station.WaterLevelMeasuredAt);
        Assert.Equal("normal", station.WaterLevelState);
        Assert.Equal(2, result.WaterLevelsUpdated);
    }

    [Fact]
    public async Task Adds_new_station_and_water_and_skips_stations_without_coordinates()
    {
        var stationsBefore = await _db.Stations.CountAsync(TestContext.Current.CancellationToken);

        var result = await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new GaugeSyncResult(StationsAdded: 1, WatersAdded: 1, WaterLevelsUpdated: 2), result);
        Assert.Equal(stationsBefore + 1, await _db.Stations.CountAsync(TestContext.Current.CancellationToken));
        var station = await _db.Stations.AsNoTracking().Include(s => s.Water)
            .SingleAsync(s => s.ExternalId == NewStationUuid, TestContext.Current.CancellationToken);
        Assert.Equal("Testfluss", station.Water.Name);
        Assert.Equal(117.0, station.WaterLevel);
        Assert.False(await _db.Stations.AnyAsync(s => s.Number == "88888888", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Second_run_with_same_data_changes_nothing()
    {
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        var result = await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new GaugeSyncResult(0, 0, 0), result);
    }

    private GaugeSync CreateSync()
    {
        var http = new HttpClient(new StubHandler(StationsJson)) { BaseAddress = new Uri("https://pegelonline.test/") };
        return new GaugeSync(_db, new PegelonlineClient(http), _time);
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
