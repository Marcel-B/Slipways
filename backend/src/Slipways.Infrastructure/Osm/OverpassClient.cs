using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;

namespace Slipways.Infrastructure.Osm;

/// <summary>
/// Reads elements with a <c>leisure</c> tag from the Overpass API (OpenStreetMap, ODbL), one country per query.
/// When Overpass is busy, the query is repeated with exponential backoff, as its usage policy asks.
/// </summary>
public sealed class OverpassClient(HttpClient http, IOptions<OsmOptions> options, TimeProvider time)
{
    public async Task<IReadOnlyList<OverpassElement>> GetElementsAsync(
        string country, string leisure, CancellationToken cancellationToken = default)
    {
        var timeout = (int)options.Value.QueryTimeout.TotalSeconds;
        // `out center` gives ways and relations a middle point, so every element has a position.
        var query = string.Create(CultureInfo.InvariantCulture,
            $"""[out:json][timeout:{timeout}];area["ISO3166-1"="{country}"][admin_level=2]->.a;nwr[leisure={leisure}](area.a);out center tags;""");

        var backoff = options.Value.BusyBackoff;
        for (var attempt = 1; ; attempt++)
        {
            using var response = await http.PostAsync("interpreter", new FormUrlEncodedContent([new("data", query)]), cancellationToken);
            if (IsBusy(response.StatusCode) && attempt < options.Value.MaxAttempts)
            {
                SlipwaysMetrics.OverpassBusyResponses.Inc();
                await Task.Delay(backoff, time, cancellationToken);
                backoff *= 2;
                continue;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OverpassResponse>(JsonSerializerOptions.Web, cancellationToken)
                ?? throw new InvalidOperationException("Overpass returned an empty response.");

            // A query that runs out of time or memory still answers 200, with a remark and incomplete elements.
            if (result.Remark is { } remark && remark.Contains("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Overpass could not finish the query for {country}: {remark}");

            return result.Elements;
        }
    }

    private static bool IsBusy(HttpStatusCode status) => status is HttpStatusCode.TooManyRequests
        or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private sealed record OverpassResponse(IReadOnlyList<OverpassElement> Elements, string? Remark);
}

/// <summary>A node with its position, or a way or relation with its <see cref="Center"/>.</summary>
public sealed record OverpassElement(
    string Type,
    long Id,
    double? Lat,
    double? Lon,
    OverpassCenter? Center,
    IReadOnlyDictionary<string, string>? Tags)
{
    public double? Latitude => Lat ?? Center?.Lat;
    public double? Longitude => Lon ?? Center?.Lon;

    /// <summary>The value of a tag, or null when it is missing or blank.</summary>
    public string? Tag(string key) => Tags?.GetValueOrDefault(key)?.Trim() is { Length: > 0 } value ? value : null;
}

public sealed record OverpassCenter(double Lat, double Lon);
