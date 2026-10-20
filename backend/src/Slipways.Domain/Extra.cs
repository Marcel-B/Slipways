namespace Slipways.Domain;

/// <summary>An amenity a slipway can offer, e.g. a campsite or parking.</summary>
public class Extra : Entity
{
    public required string Name { get; set; }
    public ICollection<Slipway> Slipways { get; set; } = [];
}
