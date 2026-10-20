using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Slipways.Api.Endpoints;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public class CatalogEndpointsTests(SlipwaysApiFactory factory) : IClassFixture<SlipwaysApiFactory>
{
    private static readonly Guid KelheimId = Guid.Parse("EAC96A93-FD47-4A9A-8771-0005805B9655");
    private static readonly Guid DonauId = Guid.Parse("D19708CA-AC42-4AD4-8D62-E85722F946F6");
    private static readonly Guid SchwingeId = Guid.Parse("714B47C3-C63B-426F-ACCC-BCB7EBDDE0E3");
    private static readonly Guid BloemerId = Guid.Parse("C64F3DA2-F392-4FB3-8A70-170B5A52B54F");

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Lists_all_seeded_slipways()
    {
        var slipways = await _client.GetFromJsonAsync<List<SlipwaySummaryDto>>("/api/slipways", TestContext.Current.CancellationToken);

        Assert.NotNull(slipways);
        Assert.Equal(106, slipways.Count);
        Assert.Equal(slipways.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => s.Id), slipways.Select(s => s.Id));
    }

    [Fact]
    public async Task Filters_slipways_by_search_term()
    {
        var slipways = await _client.GetFromJsonAsync<List<SlipwaySummaryDto>>("/api/slipways?search=kelheim", TestContext.Current.CancellationToken);

        Assert.NotNull(slipways);
        Assert.Contains(slipways, s => s.Id == KelheimId);
        Assert.All(slipways, s => Assert.Contains("kelheim", $"{s.Name} {s.City} {s.Water?.Name}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Returns_slipway_details()
    {
        var slipway = await _client.GetFromJsonAsync<SlipwayDetailsDto>($"/api/slipways/{KelheimId}", TestContext.Current.CancellationToken);

        Assert.NotNull(slipway);
        Assert.Equal("Am Jachthafen, Kelheim", slipway.Name);
        Assert.Equal("93309", slipway.PostalCode);
        Assert.Null(slipway.Costs);
        Assert.NotNull(slipway.Marina);
    }

    [Fact]
    public async Task Returns_404_for_unknown_slipway()
    {
        var response = await _client.GetAsync($"/api/slipways/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Lists_waters_with_readable_names()
    {
        var waters = await _client.GetFromJsonAsync<List<WaterSummaryDto>>("/api/waters", TestContext.Current.CancellationToken);

        Assert.NotNull(waters);
        Assert.Equal(109, waters.Count);
        Assert.Contains(waters, w => w.Name == "Oste");
        Assert.Equal(waters.OrderBy(w => w.Name, StringComparer.Ordinal).Select(w => w.Id), waters.Select(w => w.Id));
    }

    [Fact]
    public async Task Places_waters_on_something_that_lies_on_them()
    {
        var waters = await _client.GetFromJsonAsync<List<WaterSummaryDto>>("/api/waters", TestContext.Current.CancellationToken);
        var donau = await _client.GetFromJsonAsync<WaterDetailsDto>($"/api/waters/{DonauId}", TestContext.Current.CancellationToken);

        Assert.NotNull(waters);
        Assert.NotNull(donau);
        var summary = Assert.Single(waters, w => w.Id == DonauId);
        Assert.Equal(9, summary.SlipwayCount);
        Assert.Equal(17, summary.StationCount);
        Assert.NotNull(summary.Position);
        Assert.Contains(summary.Position,
            donau.Slipways.Select(s => new PositionDto(s.Latitude, s.Longitude))
                .Concat(donau.Stations.Select(s => new PositionDto(s.Latitude, s.Longitude)))
                .Concat(donau.Marinas.Select(m => new PositionDto(m.Latitude, m.Longitude))));
        Assert.Null(Assert.Single(waters, w => w.Id == BloemerId).Position);
    }

    [Fact]
    public async Task Filters_waters_by_search_term()
    {
        var waters = await _client.GetFromJsonAsync<List<WaterSummaryDto>>("/api/waters?search=donau", TestContext.Current.CancellationToken);

        Assert.NotNull(waters);
        Assert.Contains(waters, w => w.Id == DonauId);
        Assert.All(waters, w => Assert.Contains("donau", $"{w.Name} {w.Shortname}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Shows_the_reading_of_the_only_gauge_on_a_water()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>();
            var station = await db.Stations.SingleAsync(s => s.WaterId == SchwingeId, TestContext.Current.CancellationToken);
            station.WaterLevel = 412;
            station.WaterLevelMeasuredAt = new DateTime(2026, 10, 20, 9, 45, 0, DateTimeKind.Utc);
            station.WaterLevelState = "normal";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var waters = await _client.GetFromJsonAsync<List<WaterSummaryDto>>("/api/waters", TestContext.Current.CancellationToken);

        Assert.NotNull(waters);
        var gauge = Assert.Single(waters, w => w.Id == SchwingeId).Gauge;
        Assert.NotNull(gauge);
        Assert.Equal(412, gauge.WaterLevel.Value);
        Assert.Equal("normal", gauge.WaterLevel.State);
        // With several gauges there is no single reading to show.
        Assert.Null(Assert.Single(waters, w => w.Id == DonauId).Gauge);
    }

    [Fact]
    public async Task Returns_water_details_with_everything_on_it()
    {
        var water = await _client.GetFromJsonAsync<WaterDetailsDto>($"/api/waters/{DonauId}", TestContext.Current.CancellationToken);

        Assert.NotNull(water);
        Assert.Equal("Donau", water.Name);
        Assert.Contains(water.Slipways, s => s.Id == KelheimId);
        Assert.Equal(9, water.Slipways.Count);
        Assert.Equal(17, water.Stations.Count);
        Assert.Equal(4, water.Marinas.Count);
        Assert.All(water.Stations, s => Assert.Equal(DonauId, s.Water.Id));
        Assert.All(water.Marinas, m => Assert.Equal(DonauId, m.Water?.Id));

        Assert.NotNull(water.Bounds);
        Assert.All(water.Slipways, s =>
        {
            Assert.InRange(s.Latitude, water.Bounds.South, water.Bounds.North);
            Assert.InRange(s.Longitude, water.Bounds.West, water.Bounds.East);
        });
    }

    [Fact]
    public async Task Returns_water_without_bounds_when_nothing_on_it_has_coordinates()
    {
        var water = await _client.GetFromJsonAsync<WaterDetailsDto>($"/api/waters/{BloemerId}", TestContext.Current.CancellationToken);

        Assert.NotNull(water);
        Assert.Null(water.Bounds);
        Assert.Empty(water.Slipways);
    }

    [Fact]
    public async Task Returns_the_shape_of_a_water_and_places_it_by_its_shape_when_nothing_lies_on_it()
    {
        const string shape = """{"type":"LineString","coordinates":[[8.2,51.2],[8.25,51.3],[8.3,51.35]]}""";
        Guid henne;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>();
            var water = await db.Waters.SingleAsync(w => w.Shortname == "HENNE", TestContext.Current.CancellationToken);
            water.OsmElements = ["relation/10692053"];
            water.Shape = shape;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            henne = water.Id;
        }

        var details = await _client.GetFromJsonAsync<WaterDetailsDto>($"/api/waters/{henne}", TestContext.Current.CancellationToken);
        var waters = await _client.GetFromJsonAsync<List<WaterSummaryDto>>("/api/waters", TestContext.Current.CancellationToken);

        Assert.NotNull(details);
        Assert.Equal("LineString", details.Shape?.GetProperty("type").GetString());
        Assert.Equal(["relation/10692053"], details.OsmElements);
        Assert.Equal(new BoundsDto(51.2, 8.2, 51.35, 8.3), details.Bounds);
        Assert.NotNull(waters);
        Assert.Equal(new PositionDto(51.3, 8.25), Assert.Single(waters, w => w.Id == henne).Position);
    }

    [Fact]
    public async Task Places_every_slipway_in_europe()
    {
        var slipways = await _client.GetFromJsonAsync<List<SlipwaySummaryDto>>("/api/slipways", TestContext.Current.CancellationToken);

        Assert.NotNull(slipways);
        Assert.All(slipways, s =>
        {
            Assert.InRange(s.Latitude, 45, 56);
            Assert.InRange(s.Longitude, 2, 20);
        });
    }

    [Fact]
    public async Task Lists_the_shapes_of_the_waters_a_search_finds()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>();
            foreach (var water in await db.Waters.Where(w => w.Shortname == "ALLER" || w.Shortname == "EMS").ToListAsync(TestContext.Current.CancellationToken))
                water.Shape = """{"type":"LineString","coordinates":[[9.0,52.6],[9.3,52.7],[9.6,52.8]]}""";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var all = await _client.GetFromJsonAsync<JsonObject>("/api/waters/shapes", TestContext.Current.CancellationToken);
        var aller = await _client.GetFromJsonAsync<JsonObject>("/api/waters/shapes?search=aller", TestContext.Current.CancellationToken);

        Assert.NotNull(all);
        Assert.Equal("FeatureCollection", (string?)all["type"]);
        var names = all["features"]!.AsArray().Select(f => (string?)f!["properties"]!["name"]).ToList();
        Assert.Contains("Aller", names);
        Assert.Contains("Ems", names);
        Assert.DoesNotContain("Nordsee", names);

        Assert.NotNull(aller);
        var feature = Assert.Single(aller["features"]!.AsArray())!;
        Assert.Equal("Feature", (string?)feature["type"]);
        Assert.Equal("Aller", (string?)feature["properties"]!["name"]);
        // The middle vertex lies on the line and goes.
        Assert.Equal("""{"type":"LineString","coordinates":[[9,52.6],[9.6,52.8]]}""", feature["geometry"]!.ToJsonString());
    }

    [Fact]
    public async Task Leaves_out_stations_without_coordinates()
    {
        var stations = await _client.GetFromJsonAsync<List<StationDto>>("/api/stations", TestContext.Current.CancellationToken);

        Assert.NotNull(stations);
        Assert.DoesNotContain(stations, s => s.Latitude == 0 && s.Longitude == 0);
    }

    [Fact]
    public async Task Returns_404_for_unknown_water()
    {
        var response = await _client.GetAsync($"/api/waters/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_marina_details_with_its_slipways()
    {
        var slipway = await _client.GetFromJsonAsync<SlipwayDetailsDto>($"/api/slipways/{KelheimId}", TestContext.Current.CancellationToken);
        var marinaId = slipway!.Marina!.Id;

        var marina = await _client.GetFromJsonAsync<MarinaDetailsDto>($"/api/marinas/{marinaId}", TestContext.Current.CancellationToken);

        Assert.NotNull(marina);
        Assert.Equal(slipway.Marina.Name, marina.Name);
        Assert.Contains(marina.Slipways, s => s.Id == KelheimId);
    }

    [Fact]
    public async Task Filters_marinas_by_search_term()
    {
        var slipway = await _client.GetFromJsonAsync<SlipwayDetailsDto>($"/api/slipways/{KelheimId}", TestContext.Current.CancellationToken);
        var term = slipway!.Marina!.Name.Split(' ')[0];

        var marinas = await _client.GetFromJsonAsync<List<MarinaDto>>($"/api/marinas?search={Uri.EscapeDataString(term)}", TestContext.Current.CancellationToken);

        Assert.NotNull(marinas);
        Assert.Contains(marinas, m => m.Id == slipway.Marina.Id);
        Assert.All(marinas, m => Assert.Contains(term, $"{m.Name} {m.City} {m.Water?.Name}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Returns_404_for_unknown_marina()
    {
        var response = await _client.GetAsync($"/api/marinas/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/marinas")]
    [InlineData("/api/extras")]
    [InlineData("/api/services")]
    [InlineData("/api/stations")]
    [InlineData("/health")]
    public async Task Other_endpoints_respond(string url)
    {
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
