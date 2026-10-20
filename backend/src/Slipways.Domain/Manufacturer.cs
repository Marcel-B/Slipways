namespace Slipways.Domain;

/// <summary>A boat engine manufacturer.</summary>
public class Manufacturer : Entity
{
    public required string Name { get; set; }
    public ICollection<Service> Services { get; set; } = [];
}
