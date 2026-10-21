using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Osm;

/// <summary>
/// Runs <see cref="OsmImport"/> for one country at a time when <see cref="OsmImportSchedule"/> says it is due: right
/// away for a country never imported, otherwise every <see cref="OsmOptions.Interval"/> after its last success, with
/// the countries spread over that interval, and a failed run again after <see cref="OsmOptions.RetryInterval"/>.
/// The times come from the database, so restarts and deploys do not start a new import.
/// </summary>
public sealed class OsmImportWorker(
    IServiceScopeFactory scopes,
    IOptions<OsmOptions> options,
    TimeProvider time,
    ILogger<OsmImportWorker> logger) : BackgroundService
{
    // Task.Delay takes at most about 49 days, and waking up daily also notices a schedule changed in the database.
    private static readonly TimeSpan LongestWait = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        var announced = DateTime.MinValue;
        var ran = false;
        while (true)
        {
            var now = time.GetUtcNow().UtcDateTime;
            string? country = null, withoutWaters = null;
            DateTime due;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var states = await OsmImportSchedule.LoadAsync(
                    scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), options.Value.CountriesToImport, stoppingToken);
                // After a restart, the dashboard shows the last success from before it instead of nothing for days.
                if (states.Max(s => s.LastSuccess) is { } success)
                    SlipwaysMetrics.JobLastSuccess.WithLabels(OsmImportSchedule.JobName)
                        .Set(new DateTimeOffset(success, TimeSpan.Zero).ToUnixTimeMilliseconds() / 1000.0);
                (country, due) = OsmImportSchedule.NextRun(states, options.Value);
                withoutWaters = await OsmImportSchedule.WithoutWatersAsync(
                    scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), options.Value.CountriesToImport, now, options.Value, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "OpenStreetMap import schedule could not be read; trying again in {RetryInterval}", options.Value.RetryInterval);
                due = now + options.Value.RetryInterval;
            }

            // A full run takes the waters as well, so catching up only fills the time until the next one.
            if (due > now && withoutWaters is not null)
            {
                if (ran)
                    await Task.Delay(options.Value.RequestSpacing, time, stoppingToken);
                await RunWatersAsync(withoutWaters, stoppingToken);
                ran = true;
                continue;
            }

            if (due > now)
            {
                if (due != announced)
                {
                    logger.LogInformation("Next OpenStreetMap import at {Due:u} for {Country}", due, country);
                    announced = due;
                }
                var wait = due - now;
                await Task.Delay(wait < LongestWait ? wait : LongestWait, time, stoppingToken);
                continue;
            }

            // Countries due one after another, e.g. after a new installation, still keep Overpass' spacing.
            if (ran)
                await Task.Delay(options.Value.RequestSpacing, time, stoppingToken);
            await RunAsync(country!, stoppingToken);
            ran = true;
        }
    }

    private async Task RunAsync(string country, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var result = await SlipwaysMetrics.MeasureJobAsync(OsmImportSchedule.JobName, time,
                () => scope.ServiceProvider.GetRequiredService<OsmImport>().RunAsync(country, stoppingToken), stoppingToken);
            Record("slipway", result.Slipways);
            Record("marina", result.Marinas);
            SlipwaysMetrics.OsmImportChanges.WithLabels("water", "added").Inc(result.WatersAdded);
            SlipwaysMetrics.OsmMissingElements.WithLabels(country).Set(result.Missing.Count);
            logger.LogInformation(
                "OpenStreetMap import of {Country} finished: slipways {SlipwaysAdded} added, {SlipwaysUpdated} updated, {SlipwaysLinked} linked; " +
                "marinas {MarinasAdded} added, {MarinasUpdated} updated, {MarinasLinked} linked; waters {WatersAdded} added",
                country, result.Slipways.Added, result.Slipways.Updated, result.Slipways.Linked,
                result.Marinas.Added, result.Marinas.Updated, result.Marinas.Linked, result.WatersAdded);
            if (result.UserValuesResolved + result.UserValuesConflicting > 0)
                logger.LogInformation(
                    "Values from users: {Resolved} now in OpenStreetMap as well, {Conflicting} sent back to moderation",
                    result.UserValuesResolved, result.UserValuesConflicting);
            if (result.Missing.Count > 0)
                logger.LogWarning(
                    "{Count} places in {Country} are no longer in OpenStreetMap and were kept: {Elements}",
                    result.Missing.Count, country, string.Join(", ", result.Missing));
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Overpass being busy or unreachable must not take the API down.
            logger.LogWarning(ex, "OpenStreetMap import of {Country} failed; retrying in {RetryInterval}", country, options.Value.RetryInterval);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await OsmImportSchedule.RecordFailureAsync(
                    scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), country, time.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception recordEx) when (!stoppingToken.IsCancellationRequested)
            {
                // Without the failure in the database the next loop would start right away; wait here instead.
                logger.LogWarning(recordEx, "OpenStreetMap import failure could not be recorded");
                await Task.Delay(options.Value.RetryInterval, time, stoppingToken);
            }
        }
    }

    private async Task RunWatersAsync(string country, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var added = await SlipwaysMetrics.MeasureJobAsync(OsmImportSchedule.JobName, time,
                () => scope.ServiceProvider.GetRequiredService<OsmImport>().RunWatersAsync(country, stoppingToken), stoppingToken);
            SlipwaysMetrics.OsmImportChanges.WithLabels("water", "added").Inc(added);
            logger.LogInformation("OpenStreetMap waters of {Country} caught up: {WatersAdded} added", country, added);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "OpenStreetMap waters of {Country} failed; retrying in {RetryInterval}", country, options.Value.RetryInterval);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await OsmImportSchedule.RecordWatersFailureAsync(
                    scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), country, time.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception recordEx) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(recordEx, "OpenStreetMap waters failure could not be recorded");
                await Task.Delay(options.Value.RetryInterval, time, stoppingToken);
            }
        }
    }

    private static void Record(string kind, OsmImportCounts counts)
    {
        SlipwaysMetrics.OsmImportChanges.WithLabels(kind, "added").Inc(counts.Added);
        SlipwaysMetrics.OsmImportChanges.WithLabels(kind, "updated").Inc(counts.Updated);
        SlipwaysMetrics.OsmImportChanges.WithLabels(kind, "linked").Inc(counts.Linked);
    }
}
