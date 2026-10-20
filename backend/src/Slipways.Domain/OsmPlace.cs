namespace Slipways.Domain;

/// <summary>A place on a water that can come from OpenStreetMap or be linked to an element there.</summary>
public interface IOsmPlace
{
    Guid Id { get; }
    string Name { get; set; }
    double Latitude { get; set; }
    double Longitude { get; set; }

    /// <summary>One of <see cref="Sources"/>.</summary>
    string Source { get; set; }

    /// <summary>"node", "way" or "relation"; set for imported places and for manual ones matched to OpenStreetMap.</summary>
    string? OsmType { get; set; }
    long? OsmId { get; set; }

    /// <summary>
    /// ISO 3166-1 code of the country whose import found the element first. Only that country's import updates the
    /// place or reports it missing, so an element on a border is not taken over by its neighbour every month.
    /// </summary>
    string? OsmCountry { get; set; }

    /// <summary>Null when the place lies on none of the known waters, which is common for imported ones.</summary>
    Guid? WaterId { get; set; }
}
