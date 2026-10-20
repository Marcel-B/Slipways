namespace Slipways.Domain;

/// <summary>When the website of a place was last read for <see cref="PlaceInfo"/>, and with what outcome.</summary>
public class WebsiteCheck
{
    /// <summary>One of <see cref="PlaceTypes"/>.</summary>
    public required string PlaceType { get; set; }
    public Guid PlaceId { get; set; }

    /// <summary>The address that was checked; a different one on the place makes the check due again.</summary>
    public required string Url { get; set; }

    public DateTime Checked { get; set; }

    /// <summary>One of <see cref="WebsiteCheckResults"/>.</summary>
    public required string Result { get; set; }
}

public static class WebsiteCheckResults
{
    /// <summary>At least one detail was found.</summary>
    public const string Found = "found";

    /// <summary>The page was read but had nothing we take.</summary>
    public const string Empty = "empty";

    /// <summary>robots.txt does not allow us to read the page.</summary>
    public const string Disallowed = "disallowed";

    /// <summary>The address is not a public web address, e.g. one in a private network.</summary>
    public const string Refused = "refused";

    /// <summary>The site could not be reached or answered with an error.</summary>
    public const string Failed = "failed";

    /// <summary>Set by hand, e.g. when the site's owner objects; the site is never read again.</summary>
    public const string Excluded = "excluded";

    /// <summary>Results of a check; <see cref="Excluded"/> is no result but a decision.</summary>
    public static readonly string[] All = [Found, Empty, Disallowed, Refused, Failed];
}
