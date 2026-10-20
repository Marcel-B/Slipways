namespace Slipways.Domain;

public class Marina : Entity
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

    public Guid WaterId { get; set; }
    public Water Water { get; set; } = null!;
    public ICollection<Slipway> Slipways { get; set; } = [];
}
