namespace Slipways.Domain;

public class Water : Entity
{
    public required string Name { get; set; }
    public string? Shortname { get; set; }

    /// <summary>
    /// <see cref="Sources.Manual"/> for our own waters and those from PEGELONLINE, <see cref="Sources.Osm"/> for waters
    /// the OpenStreetMap import found next to slipways and marinas; their names are under the ODbL.
    /// </summary>
    public string Source { get; set; } = Sources.Manual;

    /// <summary>OpenStreetMap elements that make up the water, such as "relation/123924"; empty when it has no shape.</summary>
    public List<string> OsmElements { get; set; } = [];

    /// <summary>Simplified GeoJSON geometry of <see cref="OsmElements"/>; null until it was fetched.</summary>
    public string? Shape { get; set; }
    /// <summary>When the shape was last fetched, even if Nominatim had nothing for it.</summary>
    public DateTime? ShapeUpdated { get; set; }
}
