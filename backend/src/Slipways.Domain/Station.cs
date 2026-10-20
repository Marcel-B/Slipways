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

    /// <summary>Station id at PEGELONLINE; null until the station was synced once.</summary>
    public Guid? ExternalId { get; set; }

    /// <summary>Latest water level in centimetres at the gauge.</summary>
    public double? WaterLevel { get; set; }
    public DateTime? WaterLevelMeasuredAt { get; set; }

    /// <summary>Level relative to mean low/high water: low, normal, high, unknown, commented or out-dated.</summary>
    public string? WaterLevelState { get; set; }

    public Guid WaterId { get; set; }
    public Water Water { get; set; } = null!;
}
