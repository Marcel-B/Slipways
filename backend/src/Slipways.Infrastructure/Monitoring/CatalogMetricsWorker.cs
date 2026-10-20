using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Monitoring;

/// <summary>
/// Counts slipways, marinas and stations and measures the database every <see cref="MonitoringOptions.CatalogMetricsInterval"/>,
/// so a scrape never waits for the database.
/// </summary>
public sealed class CatalogMetricsWorker(
    IServiceScopeFactory scopes,
    IOptions<MonitoringOptions> options,
    TimeProvider time,
    ILogger<CatalogMetricsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.CatalogMetricsEnabled)
            return;

        using var timer = new PeriodicTimer(options.Value.CatalogMetricsInterval, time);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await UpdateAsync(scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>(), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Catalog metrics could not be updated");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static async Task UpdateAsync(SlipwaysDbContext db, CancellationToken cancellationToken = default)
    {
        var slipways = await db.Slipways.GroupBy(s => s.Source).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var marinas = await db.Marinas.GroupBy(m => m.Source).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        foreach (var source in new[] { Sources.Manual, Sources.Osm })
        {
            SlipwaysMetrics.Places.WithLabels("slipway", source).Set(slipways.SingleOrDefault(s => s.Key == source)?.Count ?? 0);
            SlipwaysMetrics.Places.WithLabels("marina", source).Set(marinas.SingleOrDefault(m => m.Key == source)?.Count ?? 0);
        }
        SlipwaysMetrics.Places.WithLabels("station", "pegelonline").Set(await db.Stations.CountAsync(cancellationToken));
        SlipwaysMetrics.SuggestionsPending.Set(
            await db.Suggestions.CountAsync(s => s.Status == SuggestionStatuses.Pending, cancellationToken));

        var file = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (file is { Length: > 0 } && file != ":memory:" && File.Exists(file))
            SlipwaysMetrics.DatabaseSize.Set(new FileInfo(file).Length + (File.Exists(file + "-wal") ? new FileInfo(file + "-wal").Length : 0));
    }
}
