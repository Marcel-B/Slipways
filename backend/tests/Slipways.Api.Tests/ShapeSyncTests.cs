using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Shapes;

namespace Slipways.Api.Tests;

public sealed class ShapeSyncTests : IAsyncLifetime
{
    private const string RheinGeometry = """{"type":"MultiLineString","coordinates":[[[7.6,47.6],[8.2,49.0],[6.1,51.9]]]}""";
    private const string JadeGeometry = """{"type":"LineString","coordinates":[[8.1,53.4],[8.1,53.7]]}""";
    private const string JadebusenGeometry = """{"type":"Polygon","coordinates":[[[8.1,53.4],[8.3,53.4],[8.3,53.5],[8.1,53.4]]]}""";

    // Nominatim knows these elements; the Jade fairway (way/501379182) is missing on purpose.
    private static readonly Dictionary<string, string> Known = new()
    {
        ["R123924"] = RheinGeometry,
        ["R7340040"] = JadeGeometry,
        ["R3108697"] = JadebusenGeometry,
    };

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly MutableTime _time = new(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
    private readonly StubHandler _nominatim = new();
    private SlipwaysDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new SlipwaysDbContext(new DbContextOptionsBuilder<SlipwaysDbContext>().UseSqlite(_connection).Options);
        await _db.Database.MigrateAsync();
        await SeedData.SeedAsync(_db);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Assigns_elements_by_short_name_and_stores_their_shape()
    {
        var result = await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        var rhein = await _db.Waters.AsNoTracking().SingleAsync(w => w.Shortname == "RHEIN", TestContext.Current.CancellationToken);
        Assert.Equal(["relation/123924"], rhein.OsmElements);
        Assert.Equal(JsonNode.Parse(RheinGeometry)!.ToJsonString(), JsonNode.Parse(rhein.Shape!)!.ToJsonString());
        Assert.Equal(_time.GetUtcNow().UtcDateTime, rhein.ShapeUpdated);
        Assert.Equal(2, result.ShapesUpdated);
        Assert.True(result.AssignmentsChanged > 100);
    }

    [Fact]
    public async Task Combines_several_elements_and_reports_the_missing_ones()
    {
        var result = await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        var jade = await _db.Waters.AsNoTracking().SingleAsync(w => w.Shortname == "JADE", TestContext.Current.CancellationToken);
        var shape = JsonNode.Parse(jade.Shape!)!;
        Assert.Equal("GeometryCollection", (string?)shape["type"]);
        Assert.Equal(["LineString", "Polygon"], shape["geometries"]!.AsArray().Select(g => (string?)g!["type"]));
        Assert.Contains("way/501379182", result.MissingElements);
    }

    [Fact]
    public async Task Leaves_waters_without_a_shape_alone()
    {
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        var nordsee = await _db.Waters.AsNoTracking().SingleAsync(w => w.Shortname == "NORDSEE", TestContext.Current.CancellationToken);
        Assert.Empty(nordsee.OsmElements);
        Assert.Null(nordsee.Shape);
        Assert.DoesNotContain(_nominatim.RequestedIds, id => id.StartsWith('N'));
    }

    [Fact]
    public async Task Fetches_shapes_again_only_once_they_are_due()
    {
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);
        var requests = _nominatim.Requests;

        var second = await CreateSync().RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, second.AssignmentsChanged);
        Assert.Equal(0, second.ShapesUpdated);
        Assert.Equal(requests, _nominatim.Requests);

        _time.Advance(new NominatimOptions().RefreshAfter + TimeSpan.FromHours(1));
        var third = await CreateSync().RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, third.ShapesUpdated);
    }

    private ShapeSync CreateSync()
    {
        var http = new HttpClient(_nominatim) { BaseAddress = new Uri("https://nominatim.test/") };
        var options = Options.Create(new NominatimOptions { RequestSpacing = TimeSpan.Zero });
        return new ShapeSync(_db, new NominatimClient(http, options, _time), options, _time);
    }

    /// <summary>Answers lookups like Nominatim, with a feature for every known id that was asked for.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public List<string> RequestedIds { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            var ids = HttpUtility.ParseQueryString(request.RequestUri!.Query)["osm_ids"]!.Split(',');
            RequestedIds.AddRange(ids);

            var features = new JsonArray(ids.Where(Known.ContainsKey).Select(id => (JsonNode)new JsonObject
            {
                ["type"] = "Feature",
                ["properties"] = new JsonObject
                {
                    ["osm_type"] = id[0] switch { 'R' => "relation", 'W' => "way", _ => "node" },
                    ["osm_id"] = long.Parse(id[1..]),
                },
                ["geometry"] = JsonNode.Parse(Known[id]),
            }).ToArray());
            var json = new JsonObject { ["type"] = "FeatureCollection", ["features"] = features }.ToJsonString();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class MutableTime(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
