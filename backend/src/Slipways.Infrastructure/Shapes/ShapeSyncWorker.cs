using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;

namespace Slipways.Infrastructure.Shapes;

/// <summary>Runs <see cref="ShapeSync"/> on start and then every <see cref="NominatimOptions.Interval"/>; clients never call Nominatim.</summary>
public sealed class ShapeSyncWorker(
    IServiceScopeFactory scopes,
    IOptions<NominatimOptions> options,
    TimeProvider time,
    ILogger<ShapeSyncWorker> logger) : BackgroundService
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
                var result = await SlipwaysMetrics.MeasureJobAsync("shapes", time,
                    () => scope.ServiceProvider.GetRequiredService<ShapeSync>().RunAsync(stoppingToken), stoppingToken);
                logger.LogInformation(
                    "Shape sync finished: {Assignments} assignments changed, {Shapes} shapes updated, {Places} places put on a water",
                    result.AssignmentsChanged, result.ShapesUpdated, result.PlacesAssigned);
                if (result.MissingElements.Count > 0)
                    logger.LogWarning("Nominatim does not know {Elements}", string.Join(", ", result.MissingElements));
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Nominatim being unreachable must not take the API down; the next tick retries.
                logger.LogWarning(ex, "Shape sync failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
