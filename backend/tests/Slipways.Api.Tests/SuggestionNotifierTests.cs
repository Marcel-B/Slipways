using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Notifications;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public sealed class SuggestionNotifierTests : IAsyncLifetime
{
    // Tuesday, 21 October 2026, 11:30 in Berlin.
    private static readonly DateTimeOffset Tuesday = new(2026, 10, 21, 9, 30, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly StubHandler _onkelBummel = new();
    private readonly MovableTime _time = new(Tuesday);
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
    public async Task Sends_counts_per_kind_right_away_and_each_suggestion_only_once()
    {
        await AddAsync(SuggestionKinds.NewSlipway, SuggestionKinds.NewSlipway, SuggestionKinds.Report);

        var first = await RunAsync(new NotificationOptions { Topic = "slipways-test", ModerationUrl = "https://slipways-1.example.ts.net:9465/moderation" });

        Assert.Equal(new NotificationResult(Due: true, New: 3, Pending: 3, Sent: true), first);
        var body = JsonNode.Parse(Assert.Single(_onkelBummel.Requests))!;
        Assert.Equal("slipways-test", (string?)body["topic"]);
        Assert.Equal("Slipways: 3 neue Vorschläge", (string?)body["title"]);
        Assert.Equal("2 neue Slipanlagen, 1 Meldung. Insgesamt warten 3 Vorschläge.", (string?)body["message"]);
        Assert.Equal("https://slipways-1.example.ts.net:9465/moderation", (string?)body["click"]);
        Assert.Equal(3, (int?)body["priority"]);
        Assert.Equal("anchor", (string?)Assert.Single(body["tags"]!.AsArray()));

        _time.Advance(TimeSpan.FromMinutes(1));
        await AddAsync(SuggestionKinds.Tip);
        var second = await RunAsync(new NotificationOptions { Topic = "slipways-test" });

        Assert.Equal(new NotificationResult(Due: true, New: 1, Pending: 4, Sent: true), second);
        Assert.Equal("1 Tipp. Insgesamt warten 4 Vorschläge.", (string?)JsonNode.Parse(_onkelBummel.Requests[1])!["message"]);

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.False((await RunAsync(new NotificationOptions { Topic = "slipways-test" })).Sent);
        Assert.Equal(2, _onkelBummel.Requests.Count);
    }

    [Fact]
    public async Task Collects_suggestions_until_the_next_configured_time()
    {
        var options = new NotificationOptions { Topic = "slipways-test", Days = "Mo,Do", Times = "18:00" };
        // The first check catches up on Monday 18:00 and finds nothing.
        Assert.Equal(new NotificationResult(Due: true, New: 0, Pending: 0, Sent: false), await RunAsync(options));

        await AddAsync(SuggestionKinds.Edit, SuggestionKinds.Edit);
        _time.Advance(TimeSpan.FromHours(6));
        Assert.False((await RunAsync(options)).Due);

        // Thursday, 18:00 in Berlin.
        _time.Set(new DateTimeOffset(2026, 10, 22, 16, 0, 0, TimeSpan.Zero));
        var due = await RunAsync(options);
        _time.Advance(TimeSpan.FromMinutes(1));
        var after = await RunAsync(options);

        Assert.Equal(new NotificationResult(Due: true, New: 2, Pending: 2, Sent: true), due);
        Assert.False(after.Due);
        Assert.Equal("2 Änderungen. Insgesamt warten 2 Vorschläge.", (string?)JsonNode.Parse(Assert.Single(_onkelBummel.Requests))!["message"]);
    }

    [Fact]
    public async Task Failed_send_reports_the_same_suggestions_on_the_next_check()
    {
        await AddAsync(SuggestionKinds.Report);
        _onkelBummel.Status = HttpStatusCode.ServiceUnavailable;

        await Assert.ThrowsAsync<HttpRequestException>(() => RunAsync(new NotificationOptions { Topic = "slipways-test" }));

        _onkelBummel.Status = HttpStatusCode.Created;
        var retry = await RunAsync(new NotificationOptions { Topic = "slipways-test" });
        Assert.Equal(1, retry.New);
        Assert.Equal("Slipways: 1 neuer Vorschlag", (string?)JsonNode.Parse(_onkelBummel.Requests[^1])!["title"]);
        // The same suggestions go out under the same key, so a send that did arrive is not stored twice.
        Assert.Equal(2, _onkelBummel.Keys.Count);
        Assert.Equal(_onkelBummel.Keys[0], _onkelBummel.Keys[1]);
    }

    [Fact]
    public async Task Sends_with_bearer_token_and_a_new_key_for_new_suggestions()
    {
        await AddAsync(SuggestionKinds.Tip);
        await RunAsync(new NotificationOptions { Topic = "slipways-test" });
        _time.Advance(TimeSpan.FromMinutes(1));
        await AddAsync(SuggestionKinds.Tip);
        await RunAsync(new NotificationOptions { Topic = "slipways-test" });

        Assert.All(_onkelBummel.Authorizations, a => Assert.Equal("Bearer ob_test", a));
        Assert.Equal(2, _onkelBummel.Keys.Distinct().Count());
        Assert.All(_onkelBummel.Keys, k => Assert.StartsWith("slipways-suggestions-", k));
    }

    [Fact]
    public async Task Rejected_send_names_the_problem()
    {
        await AddAsync(SuggestionKinds.Report);
        _onkelBummel.Status = HttpStatusCode.Forbidden;
        _onkelBummel.Problem = """{"type":"about:blank","title":"Topic nicht erlaubt","status":403,"detail":"Der Token darf nicht an slipways-test senden."}""";

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => RunAsync(new NotificationOptions { Topic = "slipways-test" }));

        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
        Assert.Contains("403", error.Message);
        Assert.Contains("Der Token darf nicht an slipways-test senden.", error.Message);
        Assert.DoesNotContain("ob_test", error.Message);
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("https://onkel-bummel.example.ts.net", "", false)]
    [InlineData("", "ob_test", false)]
    [InlineData("https://onkel-bummel.example.ts.net", "ob_test", true)]
    public void Is_off_without_server_or_token(string baseUrl, string token, bool configured)
    {
        // Empty values as they come from unset variables in deploy/compose.lxc.yaml.
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Notifications:BaseUrl"] = baseUrl,
            ["Notifications:Token"] = token,
        }).Build();

        var options = config.GetSection(NotificationOptions.SectionName).Get<NotificationOptions>() ?? new NotificationOptions();

        Assert.Equal(configured, options.IsConfigured);
        Assert.Equal("slipways", options.Topic);
    }

    [Theory]
    [InlineData("Monday,Freitag", "07:30")]
    [InlineData("", "8:00, 18:00")]
    public void Parses_weekdays_in_german_and_english(string days, string times)
    {
        Assert.True(NotificationSchedule.TryParse(new NotificationOptions { Days = days, Times = times }, out var schedule, out _));
        Assert.False(schedule.IsImmediate);
    }

    [Theory]
    [InlineData("Mo", "18 Uhr")]
    [InlineData("Montags", "18:00")]
    public void Rejects_unknown_weekdays_and_times(string days, string times)
    {
        Assert.False(NotificationSchedule.TryParse(new NotificationOptions { Days = days, Times = times }, out _, out var error));
        Assert.Contains("Notifications:", error);
    }

    [Fact]
    public void Last_slot_is_the_latest_configured_time_in_local_time()
    {
        Assert.True(NotificationSchedule.TryParse(new NotificationOptions { Days = "Mo,Do", Times = "08:00,18:00" }, out var schedule, out _));

        // Tuesday 11:30 → Monday 18:00 in summer time (16:00 UTC).
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 16, 0, 0, TimeSpan.Zero), schedule.LastSlot(Tuesday));
        // Thursday 29 October after the switch to winter time → 08:00 is 07:00 UTC.
        Assert.Equal(new DateTimeOffset(2026, 10, 29, 7, 0, 0, TimeSpan.Zero), schedule.LastSlot(new DateTimeOffset(2026, 10, 29, 9, 0, 0, TimeSpan.Zero)));
    }

    private async Task AddAsync(params string[] kinds)
    {
        foreach (var kind in kinds)
            _db.Suggestions.Add(new Suggestion { Id = Guid.NewGuid(), Kind = kind, Created = _time.GetUtcNow().UtcDateTime, Text = "geheim" });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private Task<NotificationResult> RunAsync(NotificationOptions options)
    {
        _db.ChangeTracker.Clear();
        Assert.True(NotificationSchedule.TryParse(options, out var schedule, out _));
        options.BaseUrl = new Uri("https://onkel-bummel.test/");
        options.Token = "ob_test";
        var http = new HttpClient(_onkelBummel) { BaseAddress = options.BaseUrl };
        var wrapped = Options.Create(options);
        return new SuggestionNotifier(_db, new OnkelBummelClient(http, wrapped), wrapped, _time).RunAsync(schedule, TestContext.Current.CancellationToken);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        /// <summary>Bodies of the messages Onkel Bummel stored.</summary>
        public List<string> Requests { get; } = [];

        /// <summary>Idempotency keys of every attempt, stored or not.</summary>
        public List<string> Keys { get; } = [];

        public List<string> Authorizations { get; } = [];

        public HttpStatusCode Status { get; set; } = HttpStatusCode.Created;

        public string? Problem { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/messages", request.RequestUri!.AbsolutePath);
            Assert.Equal("application/json", request.Content!.Headers.ContentType?.MediaType);
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            // Nothing a user wrote leaves the server.
            Assert.DoesNotContain("geheim", body);
            Authorizations.Add(request.Headers.Authorization?.ToString() ?? "");
            Keys.Add(Assert.Single(request.Headers.GetValues("Idempotency-Key")));
            if (Status == HttpStatusCode.Created)
                Requests.Add(body);
            var response = new HttpResponseMessage(Status);
            if (Problem is not null)
                response.Content = new StringContent(Problem, System.Text.Encoding.UTF8, "application/problem+json");
            return response;
        }
    }

    private sealed class MovableTime(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;

        public void Set(DateTimeOffset to) => _now = to;
    }
}
