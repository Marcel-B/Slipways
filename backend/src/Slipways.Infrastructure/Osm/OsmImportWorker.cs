using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Osm;

/// <summary>
/// Runs <see cref="OsmImport"/> when <see cref="OsmImportSchedule"/> says it is due: right away in a database without
/// imported places, otherwise every <see cref="OsmOptions.Interval"/> after the last success, and a failed run again
/// after <see cref="OsmOptions.RetryInterval"/>. The times come from the database, so restarts and deploys do not
/// start a new import.
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
        while (true)
        {
            var now = time.GetUtcNow().UtcDateTime;
            DateTime due;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var (lastSuccess, lastFailure) = await OsmImportSchedule.LoadAsync(
                    scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), stoppingToken);
                // After a restart, the dashboard shows the last success from before it instead of nothing for a month.
                if (lastSuccess is { } success)
                    SlipwaysMetrics.JobLastSuccess.WithLabels(OsmImportSchedule.JobName)
                        .Set(new DateTimeOffset(success, TimeSpan.Zero).ToUnixTimeMilliseconds() / 1000.0);
                due = OsmImportSchedule.NextRun(lastSuccess, lastFailure, options.Value);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "OpenStreetMap import schedule could not be read; trying again in {RetryInterval}", options.Value.RetryInterval);
                due = now + options.Value.RetryInterval;
            }

            if (due > now)
            {
                if (due != announced)
                {
                    logger.LogInformation("Next OpenStreetMap import at {Due:u}", due);
                    announced = due;
                }
                var wait = due - now;
                await Task.Delay(wait < LongestWait ? wait : LongestWait, time, stoppingToken);
                continue;
            }

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var result = await SlipwaysMetrics.MeasureJobAsync(OsmImportSchedule.JobName, time,
                () => scope.ServiceProvider.GetRequiredService<OsmImport>().RunAsync(stoppingToken), stoppingToken);
            Record("slipway", result.Slipways);
            Record("marina", result.Marinas);
            SlipwaysMetrics.OsmMissingElements.Set(result.Missing.Count);
            logger.LogInformation(
                "OpenStreetMap import finished: slipways {SlipwaysAdded} added, {SlipwaysUpdated} updated, {SlipwaysLinked} linked; " +
                "marinas {MarinasAdded} added, {MarinasUpdated} updated, {MarinasLinked} linked",
                result.Slipways.Added, result.Slipways.Updated, result.Slipways.Linked,
                result.Marinas.Added, result.Marinas.Updated, result.Marinas.Linked);
            if (result.UserValuesResolved + result.UserValuesConflicting > 0)
                logger.LogInformation(
                    "Values from users: {Resolved} now in OpenStreetMap as well, {Conflicting} sent back to moderation",
                    result.UserValuesResolved, result.UserValuesConflicting);
            if (result.Missing.Count > 0)
                logger.LogWarning(
                    "{Count} places are no longer in OpenStreetMap and were kept: {Elements}",
                    result.Missing.Count, string.Join(", ", result.Missing));
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Overpass being busy or unreachable must not take the API down.
            logger.LogWarning(ex, "OpenStreetMap import failed; retrying in {RetryInterval}", options.Value.RetryInterval);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await OsmImportSchedule.RecordFailureAsync(
                    scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), time.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception recordEx) when (!stoppingToken.IsCancellationRequested)
            {
                // Without the failure in the database the next loop would start right away; wait here instead.
                logger.LogWarning(recordEx, "OpenStreetMap import failure could not be recorded");
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
