using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Shapes;

/// <summary>
/// Reads geometries of OpenStreetMap elements (ODbL) from Nominatim. Lookups are batched and spaced out by
/// <see cref="NominatimOptions.RequestSpacing"/>.
/// </summary>
public sealed class NominatimClient(HttpClient http, IOptions<NominatimOptions> options, TimeProvider time)
{
    private const int MaxIdsPerLookup = 50;

    // Simplifies to roughly 100 m, which keeps the Rhine at about 30 KB and is plenty for highlighting.
    private const string Lookup = "lookup?format=geojson&polygon_geojson=1&polygon_threshold=0.001&osm_ids=";

    /// <summary>Geometries by element, such as "relation/123924"; elements Nominatim does not know are left out.</summary>
    public async Task<IReadOnlyDictionary<string, JsonElement>> GetGeometriesAsync(
        IReadOnlyCollection<string> elements, CancellationToken cancellationToken = default)
    {
        var geometries = new Dictionary<string, JsonElement>();
        foreach (var (batch, index) in elements.Chunk(MaxIdsPerLookup).Select((b, i) => (b, i)))
        {
            if (index > 0)
                await Task.Delay(options.Value.RequestSpacing, time, cancellationToken);

            var ids = string.Join(',', batch.Select(ToNominatimId));
            var result = await http.GetFromJsonAsync<FeatureCollection>(Lookup + ids, JsonSerializerOptions.Web, cancellationToken);
            foreach (var feature in result?.Features ?? [])
                geometries[$"{feature.Properties.OsmType}/{feature.Properties.OsmId}"] = feature.Geometry;
        }
        return geometries;
    }

    /// <summary>"relation/123924" becomes "R123924".</summary>
    private static string ToNominatimId(string element)
    {
        var (type, id) = element.Split('/') is [var t, var i]
            ? (t, i)
            : throw new ArgumentException($"'{element}' is not an OpenStreetMap element like 'relation/123'.", nameof(element));
        return char.ToUpper(type[0], CultureInfo.InvariantCulture) + id;
    }

    private sealed record FeatureCollection(IReadOnlyList<Feature> Features);

    private sealed record Feature(FeatureProperties Properties, JsonElement Geometry);

    private sealed record FeatureProperties(
        [property: System.Text.Json.Serialization.JsonPropertyName("osm_type")] string OsmType,
        [property: System.Text.Json.Serialization.JsonPropertyName("osm_id")] long OsmId);
}
