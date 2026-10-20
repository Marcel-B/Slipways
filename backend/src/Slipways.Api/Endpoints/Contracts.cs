using System.Text.Json;
using System.Text.Json.Nodes;

namespace Slipways.Api.Endpoints;

public record WaterDto(Guid Id, string Name);

public record PositionDto(double Latitude, double Longitude);

/// <summary>Rectangle around everything with coordinates on a water, to fit a map to it.</summary>
public record BoundsDto(double South, double West, double North, double East);

/// <summary>
/// A water for the overview. <paramref name="Position"/> is a slipway, gauge or marina near the middle of the
/// water and null when nothing on it has coordinates. <paramref name="Gauge"/> is set only when the water has
/// exactly one gauge with a reading.
/// </summary>
public record WaterSummaryDto(
    Guid Id,
    string Name,
    string? Shortname,
    int SlipwayCount,
    int StationCount,
    PositionDto? Position,
    GaugeReadingDto? Gauge);

public record GaugeReadingDto(Guid StationId, string StationName, WaterLevelDto WaterLevel);

/// <summary>
/// <paramref name="Shape"/> is a simplified GeoJSON geometry of the water from OpenStreetMap (ODbL), made of
/// <paramref name="OsmElements"/> such as "relation/123924"; null when the water has none.
/// <paramref name="Bounds"/> covers the shape and everything on the water.
/// </summary>
/// <summary>GeoJSON FeatureCollection of water shapes (OpenStreetMap, ODbL); feature ids are water ids.</summary>
public record WaterShapeCollectionDto(IReadOnlyList<WaterShapeDto> Features)
{
    public string Type => "FeatureCollection";
}

public record WaterShapeDto(Guid Id, WaterShapePropertiesDto Properties, JsonNode Geometry)
{
    public string Type => "Feature";
}

public record WaterShapePropertiesDto(string Name);

public record WaterDetailsDto(
    Guid Id,
    string Name,
    string? Shortname,
    BoundsDto? Bounds,
    JsonElement? Shape,
    IReadOnlyList<string> OsmElements,
    IReadOnlyList<SlipwaySummaryDto> Slipways,
    IReadOnlyList<StationDto> Stations,
    IReadOnlyList<MarinaDto> Marinas);

public record SlipwaySummaryDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? PostalCode,
    string? City,
    string? Country,
    WaterDto Water);

public record MarinaSummaryDto(Guid Id, string Name, string? Url);

public record SlipwayDetailsDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? Street,
    string? PostalCode,
    string? City,
    string? Country,
    string? Comment,
    int Rating,
    decimal? Costs,
    string? Pro,
    string? Contra,
    WaterDto Water,
    MarinaSummaryDto? Marina,
    IReadOnlyList<string> Extras,
    DateTime Created,
    DateTime? Updated);

public record MarinaDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? Url,
    string? Street,
    string? PostalCode,
    string? City,
    string? Phone,
    string? Email,
    WaterDto Water);

public record ExtraDto(Guid Id, string Name);

public record ServiceDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? Url,
    string? Street,
    string? PostalCode,
    string? City,
    string? Phone,
    string? Email,
    IReadOnlyList<string> Manufacturers);

public record StationDto(
    Guid Id,
    string Name,
    string? Number,
    double Latitude,
    double Longitude,
    double Km,
    string? Agency,
    WaterDto Water,
    WaterLevelDto? WaterLevel);

/// <summary>Latest reading at a gauge; <paramref name="Value"/> is in centimetres.</summary>
public record WaterLevelDto(double Value, DateTime MeasuredAt, string? State);

public record UserDto(string Id, string Email, string? DisplayName);
