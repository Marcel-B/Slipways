using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;

namespace Slipways.Infrastructure.Gauges;

/// <summary>
/// Runs <see cref="RijkswaterstaatSync"/> on start and then every <see cref="RijkswaterstaatOptions.Interval"/>, with
/// the catalogue of locations on start and every <see cref="RijkswaterstaatOptions.StationsInterval"/>.
/// </summary>
public sealed class RijkswaterstaatWorker(
    IServiceScopeFactory scopes,
    IOptions<RijkswaterstaatOptions> options,
    TimeProvider time,
    ILogger<RijkswaterstaatWorker> logger) : BackgroundService
{
    /// <summary>Name of the job in the <c>task</c> label of its metrics.</summary>
    public const string JobName = "rijkswaterstaat";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        var stationsRead = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            try
            {
                var refreshStations = time.GetUtcNow() - stationsRead >= options.Value.StationsInterval;
                await using var scope = scopes.CreateAsyncScope();
                var result = await SlipwaysMetrics.MeasureJobAsync(JobName, time,
                    () => scope.ServiceProvider.GetRequiredService<RijkswaterstaatSync>().RunAsync(refreshStations, stoppingToken), stoppingToken);
                if (refreshStations)
                    stationsRead = time.GetUtcNow();
                SlipwaysMetrics.WaterLevelsUpdated.Inc(result.WaterLevelsUpdated);
                logger.LogInformation(
                    "Rijkswaterstaat sync finished: {WaterLevels} water levels updated, {Stations} stations added, {WithoutWater} locations without a water",
                    result.WaterLevelsUpdated, result.StationsAdded, result.LocationsWithoutWater);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Rijkswaterstaat being unreachable must not take the API down; the next tick retries.
                logger.LogWarning(ex, "Rijkswaterstaat sync failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
