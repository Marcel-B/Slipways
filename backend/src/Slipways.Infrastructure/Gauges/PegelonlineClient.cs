using System.Net.Http.Json;
using System.Text.Json;

namespace Slipways.Infrastructure.Gauges;

/// <summary>Reads gauge stations from the PEGELONLINE REST API of the German waterways administration (WSV), licensed DL-DE->Zero-2.0.</summary>
public sealed class PegelonlineClient(HttpClient http)
{
    // One request returns every station with its water body and the latest water level (timeseries "W").
    private const string StationsWithWaterLevel = "stations.json?timeseries=W&includeTimeseries=true&includeCurrentMeasurement=true";

    public async Task<IReadOnlyList<PegelonlineStation>> GetStationsAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<List<PegelonlineStation>>(StationsWithWaterLevel, JsonSerializerOptions.Web, cancellationToken) ?? [];
}

public sealed record PegelonlineStation(
    Guid Uuid,
    string Number,
    string Shortname,
    string Longname,
    double? Km,
    string? Agency,
    double? Longitude,
    double? Latitude,
    PegelonlineWater Water,
    IReadOnlyList<PegelonlineTimeseries>? Timeseries);

public sealed record PegelonlineWater(string Shortname, string Longname);

public sealed record PegelonlineTimeseries(string Shortname, string? Unit, PegelonlineMeasurement? CurrentMeasurement);

public sealed record PegelonlineMeasurement(DateTimeOffset Timestamp, double Value, string? StateMnwMhw);
