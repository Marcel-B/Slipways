using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
    private readonly StubHandler _ntfy = new();
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
        var body = JsonNode.Parse(Assert.Single(_ntfy.Requests))!;
        Assert.Equal("slipways-test", (string?)body["topic"]);
        Assert.Equal("Slipways: 3 neue Vorschläge", (string?)body["title"]);
        Assert.Equal("2 neue Slipanlagen, 1 Meldung. Insgesamt warten 3 Vorschläge.", (string?)body["message"]);
        Assert.Equal("https://slipways-1.example.ts.net:9465/moderation", (string?)body["click"]);

        _time.Advance(TimeSpan.FromMinutes(1));
        await AddAsync(SuggestionKinds.Tip);
        var second = await RunAsync(new NotificationOptions { Topic = "slipways-test" });

        Assert.Equal(new NotificationResult(Due: true, New: 1, Pending: 4, Sent: true), second);
        Assert.Equal("1 Tipp. Insgesamt warten 4 Vorschläge.", (string?)JsonNode.Parse(_ntfy.Requests[1])!["message"]);

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.False((await RunAsync(new NotificationOptions { Topic = "slipways-test" })).Sent);
        Assert.Equal(2, _ntfy.Requests.Count);
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
        Assert.Equal("2 Änderungen. Insgesamt warten 2 Vorschläge.", (string?)JsonNode.Parse(Assert.Single(_ntfy.Requests))!["message"]);
    }

    [Fact]
    public async Task Failed_send_reports_the_same_suggestions_on_the_next_check()
    {
        await AddAsync(SuggestionKinds.Report);
        _ntfy.Status = HttpStatusCode.ServiceUnavailable;

        await Assert.ThrowsAsync<HttpRequestException>(() => RunAsync(new NotificationOptions { Topic = "slipways-test" }));

        _ntfy.Status = HttpStatusCode.OK;
        var retry = await RunAsync(new NotificationOptions { Topic = "slipways-test" });
        Assert.Equal(1, retry.New);
        Assert.Equal("Slipways: 1 neuer Vorschlag", (string?)JsonNode.Parse(_ntfy.Requests[^1])!["title"]);
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
        var http = new HttpClient(_ntfy) { BaseAddress = new Uri("https://ntfy.test/") };
        var wrapped = Options.Create(options);
        return new SuggestionNotifier(_db, new NtfyClient(http, wrapped), wrapped, _time).RunAsync(schedule, TestContext.Current.CancellationToken);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/", request.RequestUri!.AbsolutePath);
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            // Nothing a user wrote leaves the server.
            Assert.DoesNotContain("geheim", body);
            if (Status == HttpStatusCode.OK)
                Requests.Add(body);
            return new HttpResponseMessage(Status);
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
