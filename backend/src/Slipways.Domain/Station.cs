namespace Slipways.Domain;

/// <summary>A water level gauge station.</summary>
public class Station : Entity
{
    public required string Name { get; set; }
    public string? Number { get; set; }
    public string? Shortname { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Km { get; set; }
    public string? Agency { get; set; }

    public Guid WaterId { get; set; }
    public Water Water { get; set; } = null!;
}
