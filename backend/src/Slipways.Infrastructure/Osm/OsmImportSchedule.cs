using Microsoft.EntityFrameworkCore;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Osm;

/// <summary>Last success and failure of the import of one country.</summary>
public sealed record OsmCountryState(string Country, DateTime? LastSuccess, DateTime? LastFailure);

/// <summary>
/// Decides which country the OpenStreetMap import takes next and when, from the <see cref="JobState"/> rows in the
/// database (one per country, e.g. "osm:DE"). A restart therefore does not start a new import; only a country that
/// was never imported is due right away. The countries take turns, one every <see cref="OsmOptions.Interval"/>
/// divided by their number, so Overpass and the server get small runs spread over the interval instead of one big one.
/// </summary>
public static class OsmImportSchedule
{
    /// <summary>Name of the job in <see cref="JobState"/> and the <c>task</c> label of its metrics.</summary>
    public const string JobName = "osm";

    public static string StateName(string country) => $"{JobName}:{country}";

    /// <summary>
    /// Name of the row that records the waters of a country were imported. Waters came to the import later than the
    /// places; countries imported before that catch up on them once, see <see cref="WithoutWatersAsync"/>.
    /// </summary>
    public static string WatersStateName(string country) => $"{JobName}-waters:{country}";

    /// <summary>
    /// The first of the <paramref name="countries"/> whose waters were never imported, leaving out one that failed
    /// less than <see cref="OsmOptions.RetryInterval"/> ago; null when there is none.
    /// </summary>
    public static async Task<string?> WithoutWatersAsync(
        SlipwaysDbContext db, IReadOnlyList<string> countries, DateTime now, OsmOptions options, CancellationToken cancellationToken = default)
    {
        var prefix = WatersStateName("");
        var states = await db.JobStates.AsNoTracking()
            .Where(s => s.Name.StartsWith(prefix))
            .ToDictionaryAsync(s => s.Name, cancellationToken);
        return countries.FirstOrDefault(c => !states.TryGetValue(WatersStateName(c), out var state)
            || (state.LastSuccess is null && (state.LastFailure is null || state.LastFailure + options.RetryInterval <= now)));
    }

    /// <summary>
    /// States of the <paramref name="countries"/>. Before the import ran per country, a single row "osm" covered all
    /// of them, and before that row existed the newest imported place stood in for it. That last success still counts
    /// for the countries it imported, i.e. those with imported slipways, so they are not all fetched again at once.
    /// </summary>
    public static async Task<IReadOnlyList<OsmCountryState>> LoadAsync(
        SlipwaysDbContext db, IReadOnlyList<string> countries, CancellationToken cancellationToken = default)
    {
        var states = await db.JobStates.AsNoTracking().ToDictionaryAsync(s => s.Name, cancellationToken);
        if (countries.All(c => states.ContainsKey(StateName(c))))
            return countries.Select(c => From(c, states[StateName(c)])).ToList();

        var legacy = states.TryGetValue(JobName, out var shared) ? shared.LastSuccess : await NewestImportedAsync(db, cancellationToken);
        var imported = (await db.Slipways.AsNoTracking()
                .Where(s => s.Source == Sources.Osm && s.Country != null)
                .Select(s => s.Country!)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return countries
            .Select(c => states.TryGetValue(StateName(c), out var state)
                ? From(c, state)
                : new OsmCountryState(c, imported.Contains(c) ? legacy : null, null))
            .ToList();
    }

    /// <summary>
    /// The country whose import is due first, and when. A country imported before waits at least
    /// <see cref="OsmOptions.Interval"/> divided by the number of countries after the newest success of any country;
    /// countries that run back to back, e.g. after a new installation, thereby spread out in the following round.
    /// </summary>
    public static (string Country, DateTime Due) NextRun(IReadOnlyList<OsmCountryState> states, OsmOptions options)
    {
        var next = states.MinBy(s => NextRun(s.LastSuccess, s.LastFailure, options))
            ?? throw new InvalidOperationException("No countries to import.");
        var due = NextRun(next.LastSuccess, next.LastFailure, options);
        if (next.LastSuccess is not null && states.Max(s => s.LastSuccess) is { } newest)
            due = Max(due, newest + options.Interval / states.Count);
        return (next.Country, due);
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
    public static async Task MarkSuccessAsync(
        SlipwaysDbContext db, string country, DateTime now, CancellationToken cancellationToken = default) =>
        (await FindOrAddAsync(db, StateName(country), cancellationToken)).LastSuccess = now;

    public static async Task RecordFailureAsync(
        SlipwaysDbContext db, string country, DateTime now, CancellationToken cancellationToken = default)
    {
        (await FindOrAddAsync(db, StateName(country), cancellationToken)).LastFailure = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Marks the waters of a country as imported in <paramref name="db"/>; saved together with the waters.</summary>
    public static async Task MarkWatersImportedAsync(
        SlipwaysDbContext db, string country, DateTime now, CancellationToken cancellationToken = default) =>
        (await FindOrAddAsync(db, WatersStateName(country), cancellationToken)).LastSuccess = now;

    public static async Task RecordWatersFailureAsync(
        SlipwaysDbContext db, string country, DateTime now, CancellationToken cancellationToken = default)
    {
        (await FindOrAddAsync(db, WatersStateName(country), cancellationToken)).LastFailure = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static OsmCountryState From(string country, JobState state) => new(country, state.LastSuccess, state.LastFailure);

    private static async Task<DateTime?> NewestImportedAsync(SlipwaysDbContext db, CancellationToken cancellationToken)
    {
        var newestSlipway = await db.Slipways.Where(s => s.Source == Sources.Osm).MaxAsync(s => (DateTime?)s.Created, cancellationToken);
        var newestMarina = await db.Marinas.Where(m => m.Source == Sources.Osm).MaxAsync(m => (DateTime?)m.Created, cancellationToken);
        return Max(newestSlipway, newestMarina);
    }

    private static async Task<JobState> FindOrAddAsync(SlipwaysDbContext db, string name, CancellationToken cancellationToken)
    {
        return await db.JobStates.SingleOrDefaultAsync(s => s.Name == name, cancellationToken)
            ?? db.JobStates.Add(new JobState { Name = name }).Entity;
    }

    private static DateTime? Max(DateTime? a, DateTime? b) => a > b || b is null ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
