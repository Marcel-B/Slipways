using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;

namespace Slipways.Infrastructure.Websites;

/// <summary>Runs <see cref="WebsiteSync"/> on start and then every <see cref="WebsiteOptions.Interval"/>; clients never read the websites.</summary>
public sealed class WebsiteSyncWorker(
    IServiceScopeFactory scopes,
    IOptions<WebsiteOptions> options,
    TimeProvider time,
    ILogger<WebsiteSyncWorker> logger) : BackgroundService
{
    public const string JobName = "websites";

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
                var result = await SlipwaysMetrics.MeasureJobAsync(JobName, time,
                    () => scope.ServiceProvider.GetRequiredService<WebsiteSync>().RunAsync(stoppingToken), stoppingToken);
                if (result.Checked > 0)
                    logger.LogInformation("Website check finished: {Checked} websites read, details found on {Found}", result.Checked, result.Found);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Single sites failing is normal and handled per site; this is about the database.
                logger.LogWarning(ex, "Website check failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
