namespace Slipways.Domain;

/// <summary>
/// A contribution sent in without an account, waiting for moderation. Nothing of it is public before it is accepted.
/// Holds no address, device or name of the sender; the sender only gets a token to look at or withdraw it.
/// </summary>
public class Suggestion : Entity
{
    /// <summary>One of <see cref="SuggestionKinds"/>.</summary>
    public required string Kind { get; set; }

    /// <summary>One of <see cref="SuggestionStatuses"/>.</summary>
    public string Status { get; set; } = SuggestionStatuses.Pending;

    /// <summary>The place the suggestion is about, one of <see cref="PlaceTypes"/>; null for a new place.</summary>
    public string? PlaceType { get; set; }
    public Guid? PlaceId { get; set; }

    /// <summary>Name and position of a new place.</summary>
    public string? Name { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>The suggested values as a JSON object of field name to value, see <c>SuggestionFields</c>.</summary>
    public string? Fields { get; set; }

    /// <summary>A tip, a problem report, or where the sender knows the values from.</summary>
    public string? Text { get; set; }

    /// <summary>SHA-256 of the sender's status token, hex; null for suggestions the server made itself.</summary>
    public string? TokenHash { get; set; }

    public DateTime? Decided { get; set; }
}

public static class SuggestionKinds
{
    /// <summary>A slipway we do not have yet; becomes one of our own when accepted.</summary>
    public const string NewSlipway = "new_slipway";

    /// <summary>Values for fields of an existing place.</summary>
    public const string Edit = "edit";

    /// <summary>A short, dated tip shown on the place.</summary>
    public const string Tip = "tip";

    /// <summary>Something wrong with the place, e.g. it is closed; only for the moderator, never shown.</summary>
    public const string Report = "report";

    public static readonly string[] All = [NewSlipway, Edit, Tip, Report];
}

public static class SuggestionStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
}
