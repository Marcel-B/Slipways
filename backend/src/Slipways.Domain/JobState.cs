namespace Slipways.Domain;

/// <summary>
/// When a background job last succeeded or failed. Kept in the database, so a restart does not repeat a long job
/// such as the OpenStreetMap import, and a restored backup brings its state along.
/// </summary>
public class JobState
{
    /// <summary>Name of the job, the same as the <c>task</c> label of its metrics, e.g. "osm".</summary>
    public required string Name { get; set; }

    public DateTime? LastSuccess { get; set; }

    /// <summary>Only set by a run that failed; a run cut short by a shutdown leaves no trace.</summary>
    public DateTime? LastFailure { get; set; }
}
