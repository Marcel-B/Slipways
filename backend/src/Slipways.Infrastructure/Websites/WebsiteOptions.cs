namespace Slipways.Infrastructure.Websites;

/// <summary>
/// Settings for reading contact details and opening hours from the websites of marinas. There is no base URL: every
/// marina links to its own site.
/// </summary>
public sealed class WebsiteOptions
{
    public const string SectionName = "Websites";

    /// <summary>Turns reading websites off, e.g. for tests or offline development.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often to look for websites that are due. Each run reads at most <see cref="BatchSize"/> of them.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Websites per run; with <see cref="RequestSpacing"/>, a run takes well under an hour.</summary>
    public int BatchSize { get; set; } = 200;

    /// <summary>Contact details change rarely; reading a site again after this long picks up changes.</summary>
    public TimeSpan RefreshAfter { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Pause between two requests, robots.txt included; requests never run in parallel.</summary>
    public TimeSpan RequestSpacing { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>A site that takes longer than this is skipped until the next check.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Only this much of a page is read; home pages are rarely larger, and the rest is ignored.</summary>
    public int MaxPageBytes { get; set; } = 1024 * 1024;
}
