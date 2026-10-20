namespace Slipways.Api.Endpoints;

public record WaterDto(Guid Id, string Name);

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
