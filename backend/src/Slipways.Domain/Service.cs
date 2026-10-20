namespace Slipways.Domain;

/// <summary>A workshop that services boat engines.</summary>
public class Service : Entity
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
    public ICollection<Manufacturer> Manufacturers { get; set; } = [];
}
