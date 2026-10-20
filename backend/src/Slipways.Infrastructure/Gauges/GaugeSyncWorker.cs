using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Gauges;

/// <summary>Runs <see cref="GaugeSync"/> on start and then every <see cref="PegelonlineOptions.Interval"/>, so clients never call PEGELONLINE themselves.</summary>
public sealed class GaugeSyncWorker(
    IServiceScopeFactory scopes,
    IOptions<PegelonlineOptions> options,
    TimeProvider time,
    ILogger<GaugeSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<GaugeSync>().RunAsync(stoppingToken);
                logger.LogInformation(
                    "Gauge sync finished: {WaterLevels} water levels updated, {Stations} stations and {Waters} waters added",
                    result.WaterLevelsUpdated, result.StationsAdded, result.WatersAdded);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // PEGELONLINE being unreachable must not take the API down; the next tick retries.
                logger.LogWarning(ex, "Gauge sync failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
