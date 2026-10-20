namespace Slipways.Domain;

/// <summary>
/// A detail about a slipway or marina that comes neither from OpenStreetMap nor from our own entry, e.g. a phone number
/// found on the marina's website. One row per field, kept apart from the place itself with its <see cref="Source"/>
/// and when it was retrieved, so other sources (such as accepted suggestions from users) can add rows of their own.
/// </summary>
public class PlaceInfo : Entity
{
    /// <summary>One of <see cref="PlaceTypes"/>.</summary>
    public required string PlaceType { get; set; }
    public Guid PlaceId { get; set; }

    /// <summary>
    /// The OpenStreetMap element of the place when the row was written, if it has one. Keeps the row findable should the
    /// import ever create the place anew.
    /// </summary>
    public string? OsmType { get; set; }
    public long? OsmId { get; set; }

    /// <summary>One of <see cref="PlaceInfoFields"/>, or another field name of the place such as "fee".</summary>
    public required string Field { get; set; }
    public required string Value { get; set; }

    /// <summary>
    /// The place's own value of the field when the row was written, e.g. from OpenStreetMap; lets a later comparison
    /// tell whether the source has caught up. Not set by the website check, which only adds what is missing elsewhere.
    /// </summary>
    public string? BaseValue { get; set; }

    /// <summary>One of <see cref="Sources"/>, e.g. <see cref="Sources.Website"/>; open for more, such as user suggestions.</summary>
    public required string Source { get; set; }

    /// <summary>The page the value was read from, if it came from the web.</summary>
    public string? SourceUrl { get; set; }

    public DateTime Retrieved { get; set; }
}

public static class PlaceTypes
{
    public const string Slipway = "slipway";
    public const string Marina = "marina";
}

public static class PlaceInfoFields
{
    public const string Phone = "phone";
    public const string Email = "email";

    /// <summary>Free text, e.g. "Mo-Fr 08:00-18:00", as the source states it.</summary>
    public const string OpeningHours = "opening_hours";
}
