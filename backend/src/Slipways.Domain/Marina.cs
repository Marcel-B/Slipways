namespace Slipways.Domain;

public class Marina : Entity, IOsmPlace
{
    public required string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Url { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Taken from OpenStreetMap as is, e.g. fee "yes" or opening hours "Mo-Fr 08:00-18:00".
    public string? Operator { get; set; }
    public string? Fee { get; set; }
    public string? Access { get; set; }
    public string? OpeningHours { get; set; }

    public string Source { get; set; } = Sources.Manual;
    public string? OsmType { get; set; }
    public long? OsmId { get; set; }

    public Guid? WaterId { get; set; }
    public Water? Water { get; set; }
    public ICollection<Slipway> Slipways { get; set; } = [];
}
