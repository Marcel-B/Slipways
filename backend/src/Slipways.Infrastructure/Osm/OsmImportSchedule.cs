using Microsoft.EntityFrameworkCore;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Osm;

/// <summary>
/// Decides when the OpenStreetMap import is due, from the <see cref="JobState"/> in the database. A restart therefore
/// does not start a new import; only a database without imported places gets one right away.
/// </summary>
public static class OsmImportSchedule
{
    /// <summary>Name of the job in <see cref="JobState"/> and the <c>task</c> label of its metrics.</summary>
    public const string JobName = "osm";

    /// <summary>
    /// Last success and failure. Without a recorded state, e.g. in a database from before it was recorded or a restored
    /// backup, the newest imported place stands in for the last success: every import saves its places all at once.
    /// </summary>
    public static async Task<(DateTime? LastSuccess, DateTime? LastFailure)> LoadAsync(
        SlipwaysDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.JobStates.AsNoTracking().SingleOrDefaultAsync(s => s.Name == JobName, cancellationToken) is { } state)
            return (state.LastSuccess, state.LastFailure);

        var newestSlipway = await db.Slipways.Where(s => s.Source == Sources.Osm).MaxAsync(s => (DateTime?)s.Created, cancellationToken);
        var newestMarina = await db.Marinas.Where(m => m.Source == Sources.Osm).MaxAsync(m => (DateTime?)m.Created, cancellationToken);
        return (Max(newestSlipway, newestMarina), null);
    }

    /// <summary>
    /// <see cref="OsmOptions.Interval"/> after the last success; after a later failure not before
    /// <see cref="OsmOptions.RetryInterval"/> has passed. Without either, right away (<see cref="DateTime.MinValue"/>).
    /// </summary>
    public static DateTime NextRun(DateTime? lastSuccess, DateTime? lastFailure, OsmOptions options)
    {
        var due = lastSuccess is { } success ? success + options.Interval : DateTime.MinValue;
        if (lastFailure is { } failure && (lastSuccess is null || failure > lastSuccess))
            due = Max(due, failure + options.RetryInterval);
        return due;
    }

    /// <summary>Marks a success in <paramref name="db"/>; it is saved together with the imported places.</summary>
    public static async Task MarkSuccessAsync(SlipwaysDbContext db, DateTime now, CancellationToken cancellationToken = default) =>
        (await FindOrAddAsync(db, cancellationToken)).LastSuccess = now;

    public static async Task RecordFailureAsync(SlipwaysDbContext db, DateTime now, CancellationToken cancellationToken = default)
    {
        (await FindOrAddAsync(db, cancellationToken)).LastFailure = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<JobState> FindOrAddAsync(SlipwaysDbContext db, CancellationToken cancellationToken) =>
        await db.JobStates.SingleOrDefaultAsync(s => s.Name == JobName, cancellationToken)
        ?? db.JobStates.Add(new JobState { Name = JobName }).Entity;

    private static DateTime? Max(DateTime? a, DateTime? b) => a > b || b is null ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
