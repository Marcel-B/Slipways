namespace Slipways.Infrastructure.Gauges;

public sealed class RijkswaterstaatOptions
{
    public const string SectionName = "Rijkswaterstaat";

    /// <summary>Turns the periodic sync of Dutch gauges off, e.g. for tests or offline development.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>WaterWebservices (DDL) of Rijkswaterstaat; free, without a key.</summary>
    public Uri BaseUrl { get; set; } = new("https://ddapi20-waterwebservices.rijkswaterstaat.nl/");

    /// <summary>Rijkswaterstaat measures every 10 minutes; we read along with PEGELONLINE.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>The catalogue of locations is large and changes rarely; it is read once at start and then this often.</summary>
    public TimeSpan StationsInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Metres from a water's shape within which a location becomes a gauge on that water. Locations further away,
    /// e.g. at sea, are left out, because every station needs a water.
    /// </summary>
    public double WaterDistance { get; set; } = 1000;

    /// <summary>Locations per request for the latest values, and the pause between two requests.</summary>
    public int BatchSize { get; set; } = 100;

    public TimeSpan RequestSpacing { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>A value older than this is not taken; the service also returns old series that ended long ago.</summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(2);
}
