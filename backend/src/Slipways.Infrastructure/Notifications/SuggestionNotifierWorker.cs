using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;

namespace Slipways.Infrastructure.Notifications;

/// <summary>Runs <see cref="SuggestionNotifier"/> on start and then every <see cref="NotificationOptions.Interval"/>.</summary>
public sealed class SuggestionNotifierWorker(
    IServiceScopeFactory scopes,
    IOptions<NotificationOptions> options,
    TimeProvider time,
    ILogger<SuggestionNotifierWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || string.IsNullOrWhiteSpace(options.Value.Topic))
            return;

        if (!NotificationSchedule.TryParse(options.Value, out var schedule, out var error))
        {
            // A typo in the schedule must not cost a message; sending right away is the safe side.
            logger.LogError("{Error} New suggestions are notified right away instead", error);
            schedule = NotificationSchedule.Immediately;
        }

        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await SlipwaysMetrics.MeasureJobAsync(SuggestionNotifier.JobName, time,
                    () => scope.ServiceProvider.GetRequiredService<SuggestionNotifier>().RunAsync(schedule, stoppingToken), stoppingToken);
                if (result.Sent)
                {
                    SlipwaysMetrics.NotificationsSent.Inc();
                    logger.LogInformation("Notified about {New} new suggestions, {Pending} pending", result.New, result.Pending);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // The next tick tries again and still finds the same suggestions new.
                logger.LogWarning(ex, "Notification about new suggestions failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
