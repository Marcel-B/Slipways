namespace Slipways.Infrastructure.Shapes;

public sealed class NominatimOptions
{
    public const string SectionName = "Nominatim";

    /// <summary>Turns fetching water shapes off, e.g. for tests or offline development.</summary>
    public bool Enabled { get; set; } = true;

    public Uri BaseUrl { get; set; } = new("https://nominatim.openstreetmap.org/");

    /// <summary>Nominatim's usage policy allows one request per second.</summary>
    public TimeSpan RequestSpacing { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>How often to look for shapes that are missing or due for a refresh.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Shapes change rarely; refreshing them now and then picks up corrections in OpenStreetMap.</summary>
    public TimeSpan RefreshAfter { get; set; } = TimeSpan.FromDays(30);
}
