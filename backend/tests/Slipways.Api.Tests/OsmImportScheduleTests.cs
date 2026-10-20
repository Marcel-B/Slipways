using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Slipways.Domain;
using Slipways.Infrastructure.Osm;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public sealed class OsmImportScheduleTests : IAsyncLifetime
{
    private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly OsmOptions Options = new();

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
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
    public void Is_due_right_away_without_success_or_failure() =>
        Assert.Equal(DateTime.MinValue, OsmImportSchedule.NextRun(null, null, Options));

    [Fact]
    public void Is_due_an_interval_after_the_last_success() =>
        Assert.Equal(Monday + Options.Interval, OsmImportSchedule.NextRun(Monday, Monday.AddDays(-3), Options));

    [Fact]
    public void Waits_for_the_retry_after_a_failure()
    {
        Assert.Equal(Monday + Options.RetryInterval, OsmImportSchedule.NextRun(null, Monday, Options));
        // A failure shortly after a success does not bring the next regular run forward.
        Assert.Equal(Monday + Options.Interval, OsmImportSchedule.NextRun(Monday, Monday.AddHours(1), Options));
        // A failure long after the last success is retried.
        var failure = Monday + Options.Interval + TimeSpan.FromMinutes(5);
        Assert.Equal(failure + Options.RetryInterval, OsmImportSchedule.NextRun(Monday, failure, Options));
    }

    [Fact]
    public void Takes_the_country_due_first_and_a_new_one_right_away()
    {
        OsmCountryState[] states = [new("DE", Monday, null), new("AT", Monday.AddDays(-20), null), new("HR", null, null)];

        Assert.Equal(("HR", DateTime.MinValue), OsmImportSchedule.NextRun(states, Options));
    }

    [Fact]
    public void Spreads_the_countries_over_the_interval()
    {
        var gap = Options.Interval / 3;
        // All three ran back to back, e.g. after a new installation: the next round leaves a gap between them.
        OsmCountryState[] states = [new("DE", Monday, null), new("AT", Monday.AddMinutes(1), null), new("CH", Monday.AddMinutes(2), null)];
        Assert.Equal(("DE", Monday + Options.Interval), OsmImportSchedule.NextRun(states, Options));

        states[0] = new("DE", Monday + Options.Interval, null);
        Assert.Equal(("AT", Monday + Options.Interval + gap), OsmImportSchedule.NextRun(states, Options));
    }

    [Fact]
    public async Task A_new_database_without_imported_places_has_no_state()
    {
        Assert.Equal([new("DE", null, null), new("AT", null, null)], await LoadAsync("DE", "AT"));
    }

    [Fact]
    public async Task Without_a_state_the_newest_imported_place_stands_in_for_the_countries_it_imported()
    {
        _db.Slipways.Add(Imported(Monday.AddDays(-2)));
        _db.Slipways.Add(Imported(Monday));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal([new("DE", Monday, null), new("HR", null, null)], await LoadAsync("DE", "HR"));
    }

    [Fact]
    public async Task The_state_from_before_the_import_by_country_counts_for_the_countries_it_imported()
    {
        _db.Slipways.Add(Imported(Monday));
        _db.JobStates.Add(new JobState { Name = OsmImportSchedule.JobName, LastSuccess = Monday.AddDays(-10) });
        _db.JobStates.Add(new JobState { Name = "osm:AT", LastSuccess = Monday.AddDays(-3) });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [new("DE", Monday.AddDays(-10), null), new("AT", Monday.AddDays(-3), null), new("HR", null, null)],
            await LoadAsync("DE", "AT", "HR"));
    }

    [Fact]
    public async Task Records_a_failure_and_keeps_the_last_success()
    {
        _db.JobStates.Add(new JobState { Name = "osm:DE", LastSuccess = Monday });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _db.ChangeTracker.Clear();

        await OsmImportSchedule.RecordFailureAsync(_db, "DE", Monday.AddDays(31), TestContext.Current.CancellationToken);

        Assert.Equal([new("DE", Monday, Monday.AddDays(31))], await LoadAsync("DE"));
    }

    private Task<IReadOnlyList<OsmCountryState>> LoadAsync(params string[] countries) =>
        OsmImportSchedule.LoadAsync(_db, countries, TestContext.Current.CancellationToken);

    private static Slipway Imported(DateTime created) => new()
    {
        Id = Guid.NewGuid(),
        Created = created,
        Name = "Slipanlage",
        Country = "DE",
        Source = Sources.Osm,
        OsmType = "node",
        OsmId = created.Ticks,
    };
}
