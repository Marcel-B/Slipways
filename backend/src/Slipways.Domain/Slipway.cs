namespace Slipways.Domain;

public class Slipway : Entity
{
    public required string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Comment { get; set; }
    public int Rating { get; set; }

    /// <summary>Launch fee in euros; null when unknown.</summary>
    public decimal? Costs { get; set; }

    public string? Pro { get; set; }
    public string? Contra { get; set; }

    public Guid WaterId { get; set; }
    public Water Water { get; set; } = null!;
    public Guid? MarinaId { get; set; }
    public Marina? Marina { get; set; }
    public ICollection<Extra> Extras { get; set; } = [];
}
