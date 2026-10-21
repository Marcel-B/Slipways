using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Gauges;

/// <summary>
/// Reads water levels (Grootheid WATHTE, in cm relative to NAP, surface water) from the WaterWebservices of
/// Rijkswaterstaat, published as open data without a licence condition. Requests go one after another.
/// </summary>
public sealed class RijkswaterstaatClient(HttpClient http, IOptions<RijkswaterstaatOptions> options, TimeProvider time)
{
    private const string WaterLevel = "WATHTE";
    private const string Nap = "NAP";
    private const string SurfaceWater = "OW";

    // The service expects its Dutch names as they are (PascalCase), not in camel case as the web defaults write them.
    private static readonly JsonSerializerOptions Request = new() { PropertyNamingPolicy = null };

    /// <summary>Locations that measure the water level, from the catalogue.</summary>
    public async Task<IReadOnlyList<RijkswaterstaatLocation>> GetWaterLevelLocationsAsync(CancellationToken cancellationToken = default)
    {
        var request = new { CatalogusFilter = new { Grootheden = true, Hoedanigheden = true, Compartimenten = true, Eenheden = true, ProcesTypes = true } };
        using var response = await http.PostAsJsonAsync("METADATASERVICES/OphalenCatalogus", request, Request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var catalogue = await response.Content.ReadFromJsonAsync<Catalogue>(JsonSerializerOptions.Web, cancellationToken)
            ?? throw new InvalidOperationException("Rijkswaterstaat returned an empty catalogue.");

        var metadata = catalogue.AquoMetadataLijst
            .Where(m => m.Grootheid?.Code == WaterLevel && m.Hoedanigheid?.Code == Nap
                && m.Compartiment?.Code == SurfaceWater && m.ProcesType is null or "meting")
            .Select(m => m.MessageId)
            .ToHashSet();
        var locationIds = catalogue.AquoMetadataLocatieLijst
            .Where(l => metadata.Contains(l.MetadataMessageId))
            .Select(l => l.LocationMessageId)
            .ToHashSet();
        return catalogue.LocatieLijst
            .Where(l => locationIds.Contains(l.MessageId) && l.Lat is not null && l.Lon is not null)
            .Select(l => new RijkswaterstaatLocation(l.Code, l.Naam ?? l.Code, l.Lat!.Value, l.Lon!.Value))
            .DistinctBy(l => l.Code)
            .ToList();
    }

    /// <summary>The newest water level per location code; locations without one are left out.</summary>
    public async Task<IReadOnlyDictionary<string, RijkswaterstaatMeasurement>> GetLatestWaterLevelsAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        var latest = new Dictionary<string, RijkswaterstaatMeasurement>();
        foreach (var (batch, index) in codes.Chunk(options.Value.BatchSize).Select((b, i) => (b, i)))
        {
            if (index > 0)
                await Task.Delay(options.Value.RequestSpacing, time, cancellationToken);

            var request = new
            {
                LocatieLijst = batch.Select(code => new { Code = code }),
                AquoPlusWaarnemingMetadataLijst = new[]
                {
                    new { AquoMetadata = new { Compartiment = new { Code = SurfaceWater }, Grootheid = new { Code = WaterLevel }, Hoedanigheid = new { Code = Nap } } },
                },
            };
            using var response = await http.PostAsJsonAsync("ONLINEWAARNEMINGENSERVICES/OphalenLaatsteWaarnemingen", request, Request, cancellationToken);
            // No values for any of the locations.
            if (response.StatusCode == HttpStatusCode.NoContent)
                continue;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Observations>(JsonSerializerOptions.Web, cancellationToken);

            foreach (var observation in result?.WaarnemingenLijst ?? [])
            {
                if (observation.Locatie?.Code is not { } code || observation.AquoMetadata?.ProcesType is not (null or "meting"))
                    continue;
                foreach (var measurement in observation.MetingenLijst ?? [])
                {
                    // Quality code 99 and the value 999999999 both mean "no value".
                    if (measurement.Meetwaarde?.WaardeNumeriek is not { } value || value > 9999
                        || measurement.WaarnemingMetadata?.IsMissing == true)
                        continue;
                    if (!latest.TryGetValue(code, out var known) || measurement.Tijdstip > known.MeasuredAt)
                        latest[code] = new RijkswaterstaatMeasurement(value, measurement.Tijdstip);
                }
            }
        }
        return latest;
    }

    private sealed record Catalogue(
        IReadOnlyList<CatalogueMetadata> AquoMetadataLijst,
        IReadOnlyList<CatalogueLocation> LocatieLijst,
        IReadOnlyList<CatalogueLink> AquoMetadataLocatieLijst);

    private sealed record CatalogueMetadata(
        [property: JsonPropertyName("AquoMetadata_MessageID")] long MessageId,
        CodeValue? Compartiment,
        CodeValue? Grootheid,
        CodeValue? Hoedanigheid,
        string? ProcesType);

    // Lat and Lon are ETRS89, which matches WGS 84 to well under a metre.
    private sealed record CatalogueLocation(
        [property: JsonPropertyName("Locatie_MessageID")] long MessageId,
        string Code,
        string? Naam,
        double? Lat,
        double? Lon);

    // The service spells it "AquoMetaData_MessageID" here; the serializer ignores case.
    private sealed record CatalogueLink(
        [property: JsonPropertyName("Locatie_MessageID")] long LocationMessageId,
        [property: JsonPropertyName("AquoMetadata_MessageID")] long MetadataMessageId);

    private sealed record CodeValue(string? Code);

    private sealed record Observations(IReadOnlyList<Observation>? WaarnemingenLijst);

    private sealed record Observation(ObservationLocation? Locatie, ObservationMetadata? AquoMetadata, IReadOnlyList<Measurement>? MetingenLijst);

    private sealed record ObservationLocation(string? Code);

    private sealed record ObservationMetadata(string? ProcesType);

    private sealed record Measurement(DateTimeOffset Tijdstip, MeasuredValue? Meetwaarde, MeasurementMetadata? WaarnemingMetadata);

    private sealed record MeasuredValue([property: JsonPropertyName("Waarde_Numeriek")] double? WaardeNumeriek);

    // Older versions of the service sent a list of one code instead of the code itself.
    private sealed record MeasurementMetadata(JsonElement Kwaliteitswaardecode)
    {
        public bool IsMissing => Kwaliteitswaardecode.ValueKind switch
        {
            JsonValueKind.String => Kwaliteitswaardecode.GetString() == "99",
            JsonValueKind.Array => Kwaliteitswaardecode.EnumerateArray().Any(c => c.ValueKind == JsonValueKind.String && c.GetString() == "99"),
            _ => false,
        };
    }
}

/// <param name="Code">Location code of Rijkswaterstaat, e.g. "delfzijl".</param>
public sealed record RijkswaterstaatLocation(string Code, string Name, double Latitude, double Longitude);

/// <param name="Value">Water level in centimetres relative to NAP.</param>
public sealed record RijkswaterstaatMeasurement(double Value, DateTimeOffset MeasuredAt);
