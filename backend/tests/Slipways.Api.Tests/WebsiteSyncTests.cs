using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Websites;

namespace Slipways.Api.Tests;

public sealed class WebsiteSyncTests : IAsyncLifetime
{
    private static readonly Guid SaalMarinaId = Guid.Parse("4CFBA36B-39A9-4F63-B155-32D6279DE801");

    private const string HomePage = """
        <html><head><script type="application/ld+json">
        { "@type": "Marina", "telephone": "+49 9441 1234", "openingHours": "Mo-Su 08:00-20:00" }
        </script></head>
        <body><a href="mailto:hafen@saal.example">Mail</a></body></html>
        """;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly StubHandler _web = new();
    private DateTimeOffset _now = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    private SlipwaysDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new SlipwaysDbContext(new DbContextOptionsBuilder<SlipwaysDbContext>().UseSqlite(_connection).Options);
        await _db.Database.MigrateAsync();
        await SeedData.SeedAsync(_db);
        await _db.Marinas.ExecuteUpdateAsync(m => m.SetProperty(x => x.Url, (string?)null), TestContext.Current.CancellationToken);
        await SetUrlAsync("https://saal.example/");
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Keeps_details_from_the_home_page_apart_from_the_marina()
    {
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Respond("https://saal.example/", HttpStatusCode.OK, HomePage);

        var result = await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new WebsiteSyncResult(Checked: 1, Found: 1), result);
        var infos = await Infos();
        Assert.Equal(
            [("email", "hafen@saal.example"), ("opening_hours", "Mo-Su 08:00-20:00"), ("phone", "+49 9441 1234")],
            infos.Select(i => (i.Field, i.Value)));
        Assert.All(infos, i => Assert.Equal((Sources.Website, "https://saal.example/", _now.UtcDateTime), (i.Source, i.SourceUrl, i.Retrieved)));
        var marina = await _db.Marinas.AsNoTracking().SingleAsync(m => m.Id == SaalMarinaId, TestContext.Current.CancellationToken);
        Assert.Null(marina.OpeningHours);
        var check = await _db.WebsiteChecks.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal((WebsiteCheckResults.Found, "https://saal.example/"), (check.Result, check.Url));
    }

    [Fact]
    public async Task Reads_a_site_again_only_when_it_is_due_or_its_address_changed()
    {
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Respond("https://saal.example/", HttpStatusCode.OK, HomePage);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new WebsiteSyncResult(0, 0), await CreateSync().RunAsync(TestContext.Current.CancellationToken));

        _now += TimeSpan.FromDays(31);
        Assert.Equal(1, (await CreateSync().RunAsync(TestContext.Current.CancellationToken)).Checked);

        await SetUrlAsync("https://neu.saal.example/");
        _web.Respond("https://neu.saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Respond("https://neu.saal.example/", HttpStatusCode.OK, "<p>Willkommen</p>");
        Assert.Equal(new WebsiteSyncResult(1, 0), await CreateSync().RunAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await Infos());
    }

    [Fact]
    public async Task Respects_robots_txt_and_drops_what_was_found_before()
    {
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Respond("https://saal.example/", HttpStatusCode.OK, HomePage);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        _now += TimeSpan.FromDays(31);
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.OK, "User-agent: *\nDisallow: /\n");
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await Infos());
        Assert.Equal(WebsiteCheckResults.Disallowed, (await _db.WebsiteChecks.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Result);
        Assert.Equal(1, _web.Requested.Count(u => u == "https://saal.example/"));
    }

    [Fact]
    public async Task Keeps_what_was_found_while_a_site_is_down()
    {
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Respond("https://saal.example/", HttpStatusCode.OK, HomePage);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        _now += TimeSpan.FromDays(31);
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.ServiceUnavailable);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, (await Infos()).Count);
        Assert.Equal(WebsiteCheckResults.Failed, (await _db.WebsiteChecks.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Result);
    }

    [Fact]
    public async Task Follows_redirects_and_checks_robots_txt_of_every_host()
    {
        await SetUrlAsync("www.saal.example");
        _web.Respond("http://www.saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Redirect("http://www.saal.example/", "https://hafen-saal.example/start");
        _web.Respond("https://hafen-saal.example/robots.txt", HttpStatusCode.OK, "User-agent: *\nDisallow: /intern\n");
        _web.Respond("https://hafen-saal.example/start", HttpStatusCode.OK, HomePage);

        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal("https://hafen-saal.example/start", (await Infos()).First().SourceUrl);
        Assert.Equal(
            ["http://www.saal.example/robots.txt", "http://www.saal.example/", "https://hafen-saal.example/robots.txt", "https://hafen-saal.example/start"],
            _web.Requested);
    }

    [Fact]
    public async Task Forgets_websites_a_marina_no_longer_links()
    {
        _web.Respond("https://saal.example/robots.txt", HttpStatusCode.NotFound);
        _web.Respond("https://saal.example/", HttpStatusCode.OK, HomePage);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        await SetUrlAsync(null);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await Infos());
        Assert.False(await _db.WebsiteChecks.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Never_reads_an_excluded_site_again()
    {
        _db.WebsiteChecks.Add(new WebsiteCheck
        {
            PlaceType = PlaceTypes.Marina, PlaceId = SaalMarinaId, Url = "https://alt.saal.example/",
            Checked = _now.UtcDateTime.AddDays(-100), Result = WebsiteCheckResults.Excluded,
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, (await CreateSync().RunAsync(TestContext.Current.CancellationToken)).Checked);
        await SetUrlAsync(null);
        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_web.Requested);
        Assert.True(await _db.WebsiteChecks.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Refuses_addresses_that_are_no_web_addresses()
    {
        await SetUrlAsync("ftp://saal.example/");

        await CreateSync().RunAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_web.Requested);
        Assert.Equal(WebsiteCheckResults.Refused, (await _db.WebsiteChecks.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Result);
    }

    private Task<List<PlaceInfo>> Infos() =>
        _db.PlaceInfos.AsNoTracking().Where(i => i.PlaceId == SaalMarinaId).OrderBy(i => i.Field).ToListAsync(TestContext.Current.CancellationToken);

    private Task SetUrlAsync(string? url) =>
        _db.Marinas.Where(m => m.Id == SaalMarinaId)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Url, url), TestContext.Current.CancellationToken);

    private WebsiteSync CreateSync()
    {
        _db.ChangeTracker.Clear();
        var options = Options.Create(new WebsiteOptions { RequestSpacing = TimeSpan.Zero });
        var time = new FixedTime(_now);
        var client = new WebsiteClient(new HttpClient(_web), options, time);
        return new WebsiteSync(_db, client, options, time);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = [];

        public List<string> Requested { get; } = [];

        public void Respond(string url, HttpStatusCode status, string body = "") =>
            _responses[url] = () => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, url.EndsWith("robots.txt", StringComparison.Ordinal) ? "text/plain" : "text/html"),
            };

        public void Redirect(string url, string location) =>
            _responses[url] = () => new HttpResponseMessage(HttpStatusCode.MovedPermanently) { Headers = { Location = new Uri(location) } };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requested.Add(url);
            return Task.FromResult(_responses.TryGetValue(url, out var response)
                ? response()
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
