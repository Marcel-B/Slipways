namespace Slipways.Infrastructure.Gauges;

public sealed class PegelonlineOptions
{
    public const string SectionName = "Pegelonline";

    /// <summary>Turns the periodic sync off, e.g. for tests or offline development.</summary>
    public bool Enabled { get; set; } = true;

    public Uri BaseUrl { get; set; } = new("https://www.pegelonline.wsv.de/webservices/rest-api/v2/");

    /// <summary>PEGELONLINE publishes water levels every 15 minutes.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
}
