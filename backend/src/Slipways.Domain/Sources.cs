namespace Slipways.Domain;

/// <summary>Where a slipway or marina, or a <see cref="PlaceInfo"/> about it, comes from.</summary>
public static class Sources
{
    /// <summary>Entered by hand; our own data.</summary>
    public const string Manual = "manual";

    /// <summary>Imported from OpenStreetMap and licensed under the ODbL, so it is kept apart from our own data.</summary>
    public const string Osm = "osm";

    /// <summary>Read from the place's own website; only used for <see cref="PlaceInfo"/>.</summary>
    public const string Website = "website";
}
