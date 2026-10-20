using Prometheus;

namespace Slipways.Infrastructure.Monitoring;

/// <summary>
/// Metrics of the server for Prometheus. Labels only take a few fixed values; <c>/metrics</c> has no login, so
/// nothing about users or content goes in here.
/// </summary>
public static class SlipwaysMetrics
{
    // The label is called "task" because Prometheus sets "job" itself, to the scrape job, and renames a "job"
    // label of the app to "exported_job".
    public static readonly string[] Jobs = ["gauges", "shapes", "osm"];

    public static readonly Counter JobRuns = Metrics.CreateCounter(
        "slipways_job_runs_total", "Runs of background jobs, by task and result (success, failure).", "task", "result");

    public static readonly Histogram JobDuration = Metrics.CreateHistogram(
        "slipways_job_duration_seconds", "Duration of background job runs.",
        new HistogramConfiguration { LabelNames = ["task"], Buckets = [0.1, 0.5, 1, 5, 15, 60, 300, 900, 1800] });

    public static readonly Gauge JobLastSuccess = Metrics.CreateGauge(
        "slipways_job_last_success_timestamp_seconds", "Unix time of the last successful run of a background job, by task.", "task");

    public static readonly Counter WaterLevelsUpdated = Metrics.CreateCounter(
        "slipways_gauge_water_levels_updated_total", "Water levels taken over from PEGELONLINE.");

    public static readonly Counter OsmImportChanges = Metrics.CreateCounter(
        "slipways_osm_import_changes_total",
        "Places changed by the OpenStreetMap import, by kind (slipway, marina) and change (added, updated, linked).",
        "kind", "change");

    // Absent until the first import is done, so a dashboard does not read 0 as "nothing missing" before that.
    public static readonly Gauge OsmMissingElements = Metrics.CreateGauge(
        "slipways_osm_missing_elements", "Places whose OpenStreetMap element the last import no longer found.",
        new GaugeConfiguration { SuppressInitialValue = true });

    public static readonly Counter OverpassBusyResponses = Metrics.CreateCounter(
        "slipways_overpass_busy_responses_total", "Overpass answers that it is busy (429, 502, 503, 504), each followed by a retry.");

    public static readonly Gauge Places = Metrics.CreateGauge(
        "slipways_places", "Slipways, marinas and gauge stations in the database, by kind and source.", "kind", "source");

    public static readonly Gauge DatabaseSize = Metrics.CreateGauge(
        "slipways_database_size_bytes", "Size of the SQLite database file including its write-ahead log.");

    // prometheus-net calls it process_working_set_bytes; the shared overview dashboard expects this name.
    // On Linux, the working set is the resident set.
    private static readonly Gauge ResidentMemory = Metrics.CreateGauge(
        "process_resident_memory_bytes", "Resident memory size in bytes.");

    public static readonly Counter LokiLinesPushed = Metrics.CreateCounter(
        "slipways_loki_lines_pushed_total", "Log lines pushed to Loki.");

    public static readonly Counter LokiLinesDropped = Metrics.CreateCounter(
        "slipways_loki_lines_dropped_total", "Log lines dropped, by reason (queue_full, push_failed).", "reason");

    static SlipwaysMetrics()
    {
        // Series that exist from the start read as 0 instead of missing, which keeps rate() and alerts simple.
        foreach (var job in Jobs)
        {
            JobRuns.WithLabels(job, "success");
            JobRuns.WithLabels(job, "failure");
            JobDuration.WithLabels(job);
        }
        foreach (var kind in new[] { "slipway", "marina" })
        foreach (var change in new[] { "added", "updated", "linked" })
            OsmImportChanges.WithLabels(kind, change);
        LokiLinesDropped.WithLabels("queue_full");
        LokiLinesDropped.WithLabels("push_failed");

        Metrics.DefaultRegistry.AddBeforeCollectCallback(() => ResidentMemory.Set(Environment.WorkingSet));
    }

    /// <summary>Registers all metrics, so they show up from the first scrape on and not only once they are used.</summary>
    public static void Register()
    {
        // Calling any member runs the static constructor; there is nothing else to do.
    }

    /// <summary>Runs a background job and records its result and duration; a run cut short by shutdown counts as neither.</summary>
    public static async Task<T> MeasureJobAsync<T>(string job, TimeProvider time, Func<Task<T>> run, CancellationToken cancellationToken)
    {
        var started = time.GetTimestamp();
        try
        {
            var result = await run();
            JobRuns.WithLabels(job, "success").Inc();
            JobLastSuccess.WithLabels(job).Set(time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0);
            JobDuration.WithLabels(job).Observe(time.GetElapsedTime(started).TotalSeconds);
            return result;
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            JobRuns.WithLabels(job, "failure").Inc();
            JobDuration.WithLabels(job).Observe(time.GetElapsedTime(started).TotalSeconds);
            throw;
        }
    }
}
