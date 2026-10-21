namespace Slipways.Infrastructure.Osm;

public sealed class OsmOptions
{
    public const string SectionName = "Osm";

    private static readonly string[] DefaultCountries = ["DE", "AT", "CH", "NL", "BE", "FR", "ES", "IT", "HR", "DK", "SE", "NO"];

    /// <summary>Turns the periodic import off, e.g. for tests or offline development.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Overpass API endpoint; for heavy use, a self-hosted instance is preferred over the public one.</summary>
    public Uri BaseUrl { get; set; } = new("https://overpass-api.de/api/");

    /// <summary>
    /// ISO 3166-1 codes of the countries to import; empty means <see cref="DefaultCountries"/>. A default list in
    /// the property itself would not work, because the configuration binder appends to it instead of replacing it.
    /// </summary>
    public string[] Countries { get; set; } = [];

    public IReadOnlyList<string> CountriesToImport => Countries.Length > 0 ? Countries : DefaultCountries;

    /// <summary>
    /// How often each country is imported. Slipways and marinas change rarely, and every run asks Overpass for a whole
    /// country. The countries take turns, so one runs every <c>Interval</c> divided by their number.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(30);

    /// <summary>How long to wait after a failed run before trying again, instead of waiting a whole <see cref="Interval"/>.</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Sent to Overpass as the query timeout, so it can plan its resources; a country takes well under a minute.</summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Pause between two queries, also between two countries due at the same time; Overpass asks clients not to send
    /// queries in parallel or in quick succession.
    /// </summary>
    public TimeSpan RequestSpacing { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>First wait when Overpass is busy (429, 502, 503, 504); it doubles with every further attempt.</summary>
    public TimeSpan BusyBackoff { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxAttempts { get; set; } = 4;
}
