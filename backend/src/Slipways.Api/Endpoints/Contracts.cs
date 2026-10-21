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
/// exactly one gauge with a reading. <paramref name="Countries"/> are the ISO codes of the countries its slipways,
/// marinas and gauges lie in, the one with the most first; empty when nothing lies on it.
/// </summary>
public record WaterSummaryDto(
    Guid Id,
    string Name,
    string? Shortname,
    int SlipwayCount,
    int StationCount,
    PositionDto? Position,
    GaugeReadingDto? Gauge,
    IReadOnlyList<string> Countries);

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

/// <summary>
/// <paramref name="Source"/> is "manual" for our own data and "osm" for data imported from OpenStreetMap (ODbL).
/// <paramref name="Water"/> is null when the slipway lies on none of the known waters.
/// </summary>
public record SlipwaySummaryDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? PostalCode,
    string? City,
    string? Country,
    WaterDto? Water,
    string Source);

public record MarinaSummaryDto(Guid Id, string Name, string? Url);

/// <summary>
/// <paramref name="Operator"/> to <paramref name="Surface"/> are OpenStreetMap tags as they are, e.g. fee "yes".
/// <paramref name="OsmElement"/>, such as "node/123", is set for imported slipways and for our own ones that were
/// matched to OpenStreetMap; see <see cref="SlipwaySummaryDto"/> for <paramref name="Source"/>.
/// </summary>
public record SlipwayDetailsDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? Street,
    string? PostalCode,
    string? City,
    string? Country,
    string? Url,
    string? Comment,
    int Rating,
    decimal? Costs,
    string? Pro,
    string? Contra,
    string? Operator,
    string? Fee,
    string? Access,
    string? OpeningHours,
    string? Surface,
    WaterDto? Water,
    MarinaSummaryDto? Marina,
    IReadOnlyList<string> Extras,
    string Source,
    string? OsmElement,
    DateTime Created,
    DateTime? Updated,
    IReadOnlyList<PlaceInfoDto> Infos);

/// <summary>
/// Fields and <paramref name="Source"/> as in <see cref="SlipwayDetailsDto"/>. <paramref name="Country"/> is the ISO
/// code of the country the import found it in, for our own marinas that of their slipways.
/// </summary>
public record MarinaDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string? Url,
    string? Street,
    string? PostalCode,
    string? City,
    string? Country,
    string? Phone,
    string? Email,
    string? Operator,
    string? Fee,
    string? Access,
    string? OpeningHours,
    WaterDto? Water,
    string Source,
    string? OsmElement);

/// <summary>
/// A marina as in <see cref="MarinaDto"/>, with the slipways that belong to it and <paramref name="Infos"/> from other
/// sources than the marina's own fields, such as its website.
/// </summary>
public record MarinaDetailsDto(
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
    string? Operator,
    string? Fee,
    string? Access,
    string? OpeningHours,
    WaterDto? Water,
    string Source,
    string? OsmElement,
    IReadOnlyList<SlipwaySummaryDto> Slipways,
    IReadOnlyList<PlaceInfoDto> Infos);

/// <summary>
/// A detail such as a phone number from <paramref name="Source"/> ("website": read from <paramref name="SourceUrl"/>),
/// or "user" (an accepted suggestion), as it was on <paramref name="Retrieved"/>. <paramref name="Field"/> is e.g.
/// "phone", "email", "opening_hours", "fee" or "tip"; values from users win over the place's own fields.
/// </summary>
public record PlaceInfoDto(string Field, string Value, string Source, string? SourceUrl, DateTime Retrieved);

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

/// <summary>A proof-of-work challenge in the format of ALTCHA; see <see cref="SuggestionRequestDto.Proof"/>.</summary>
public record ProofChallengeDto(string Algorithm, string Challenge, int MaxNumber, string Salt, string Signature);

/// <summary>The challenge as it came, with the <paramref name="Number"/> for which SHA-256(salt + number) is the challenge.</summary>
public record ProofSolutionDto(string Algorithm, string Challenge, int Number, string Salt, string Signature);

/// <summary>
/// A suggestion from a user. <paramref name="Kind"/> is "new_slipway" (with <paramref name="Name"/> and position),
/// "edit" (with <paramref name="Fields"/>), "tip" or "report" (with <paramref name="Text"/>); the last three need
/// <paramref name="PlaceType"/> ("slipway" or "marina") and <paramref name="PlaceId"/>. For the others,
/// <paramref name="Text"/> is an optional note for the moderator. <paramref name="Homepage"/> must stay empty: forms
/// hide it from people, so only bots fill it in.
/// </summary>
public record SuggestionRequestDto(
    string Kind,
    string? PlaceType,
    Guid? PlaceId,
    string? Name,
    double? Latitude,
    double? Longitude,
    Dictionary<string, string>? Fields,
    string? Text,
    bool ConfirmNew,
    string? Homepage,
    ProofSolutionDto? Proof);

/// <summary><paramref name="Token"/> is shown only once; with it the sender can look at or withdraw the suggestion.</summary>
public record SuggestionCreatedDto(Guid Id, string Token);

/// <summary>Slipways close to a suggested new one; sent with 409, after which the sender may confirm with ConfirmNew.</summary>
public record NearbySlipwaysDto(IReadOnlyList<NearbySlipwayDto> Slipways);

/// <summary><paramref name="Distance"/> in metres.</summary>
public record NearbySlipwayDto(Guid Id, string Name, double Distance, string Source);

/// <summary><paramref name="Status"/> is "pending", "accepted" or "rejected".</summary>
public record SuggestionStatusDto(Guid Id, string Kind, string Status, DateTime Created, DateTime? Decided);

/// <summary>Values from users on places imported from OpenStreetMap, published under the ODbL like the places themselves.</summary>
public record OdblContributionsDto(string License, string Attribution, IReadOnlyList<OdblContributionDto> Values);

/// <summary><paramref name="OsmElement"/> such as "node/123".</summary>
public record OdblContributionDto(string PlaceType, string OsmElement, string Field, string Value, DateTime Retrieved);

/// <summary>A suggestion as the moderator sees it.</summary>
public record ModerationSuggestionDto(
    Guid Id,
    string Kind,
    string Status,
    DateTime Created,
    DateTime? Decided,
    string? PlaceType,
    Guid? PlaceId,
    string? PlaceName,
    string? PlaceSource,
    string? Name,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<ModerationFieldDto> Fields,
    string? Text,
    bool FromServer,
    IReadOnlyList<NearbySlipwayDto> Nearby);

/// <summary><paramref name="Current"/> is what the place shows now: a value from users if there is one, else its own.</summary>
public record ModerationFieldDto(string Field, string Value, string? Current);
