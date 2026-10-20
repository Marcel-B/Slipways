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
    public async Task A_new_database_without_imported_places_has_no_state()
    {
        Assert.Equal((null, null), await OsmImportSchedule.LoadAsync(_db, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Without_a_state_the_newest_imported_place_stands_in_for_the_last_success()
    {
        _db.Slipways.Add(Imported(Monday.AddDays(-2)));
        _db.Slipways.Add(Imported(Monday));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((Monday, null), await OsmImportSchedule.LoadAsync(_db, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_recorded_state_wins_over_the_imported_places()
    {
        _db.Slipways.Add(Imported(Monday));
        _db.JobStates.Add(new JobState { Name = OsmImportSchedule.JobName, LastSuccess = Monday.AddDays(-10) });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((Monday.AddDays(-10), null), await OsmImportSchedule.LoadAsync(_db, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Records_a_failure_and_keeps_the_last_success()
    {
        _db.JobStates.Add(new JobState { Name = OsmImportSchedule.JobName, LastSuccess = Monday });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _db.ChangeTracker.Clear();

        await OsmImportSchedule.RecordFailureAsync(_db, Monday.AddDays(31), TestContext.Current.CancellationToken);

        Assert.Equal((Monday, Monday.AddDays(31)), await OsmImportSchedule.LoadAsync(_db, TestContext.Current.CancellationToken));
    }

    private static Slipway Imported(DateTime created) => new()
    {
        Id = Guid.NewGuid(),
        Created = created,
        Name = "Slipanlage",
        Source = Sources.Osm,
        OsmType = "node",
        OsmId = created.Ticks,
    };
}
