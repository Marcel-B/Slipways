using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Osm;

/// <summary>
/// Runs <see cref="OsmImport"/> on start and then every <see cref="OsmOptions.Interval"/>. A failed run is repeated
/// after <see cref="OsmOptions.RetryInterval"/>, since waiting for the next interval would take a month.
/// </summary>
public sealed class OsmImportWorker(
    IServiceScopeFactory scopes,
    IOptions<OsmOptions> options,
    TimeProvider time,
    ILogger<OsmImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        while (true)
        {
            var wait = options.Value.Interval;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<OsmImport>().RunAsync(stoppingToken);
                logger.LogInformation(
                    "OpenStreetMap import finished: slipways {SlipwaysAdded} added, {SlipwaysUpdated} updated, {SlipwaysLinked} linked; " +
                    "marinas {MarinasAdded} added, {MarinasUpdated} updated, {MarinasLinked} linked",
                    result.Slipways.Added, result.Slipways.Updated, result.Slipways.Linked,
                    result.Marinas.Added, result.Marinas.Updated, result.Marinas.Linked);
                if (result.Missing.Count > 0)
                    logger.LogWarning(
                        "{Count} places are no longer in OpenStreetMap and were kept: {Elements}",
                        result.Missing.Count, string.Join(", ", result.Missing));
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Overpass being busy or unreachable must not take the API down.
                logger.LogWarning(ex, "OpenStreetMap import failed; retrying in {RetryInterval}", options.Value.RetryInterval);
                wait = options.Value.RetryInterval;
            }

            await Task.Delay(wait, time, stoppingToken);
        }
    }
}
