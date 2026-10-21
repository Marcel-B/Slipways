using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Osm;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Gauges;

/// <param name="LocationsWithoutWater">Locations from the catalogue that lie on none of our waters, e.g. at sea.</param>
public sealed record RijkswaterstaatSyncResult(int StationsAdded, int LocationsWithoutWater, int WaterLevelsUpdated);

/// <summary>
/// Brings Dutch gauges and their latest water levels in line with Rijkswaterstaat. Rijkswaterstaat names no water
/// for a location, so a location becomes a station on the water whose shape is closest, within
/// <see cref="RijkswaterstaatOptions.WaterDistance"/>; the shapes come from PEGELONLINE's waters and the waters the
/// OpenStreetMap import found. Stations are matched by location code and never deleted.
/// </summary>
public sealed class RijkswaterstaatSync(
    SlipwaysDbContext db, RijkswaterstaatClient client, IOptions<RijkswaterstaatOptions> options, TimeProvider time)
{
    public const string Agency = "Rijkswaterstaat";

    /// <param name="refreshStations">Reads the catalogue of locations first, which is large, so not on every run.</param>
    public async Task<RijkswaterstaatSyncResult> RunAsync(bool refreshStations, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var stations = await db.Stations.Where(s => s.ExternalCode != null).ToDictionaryAsync(s => s.ExternalCode!, cancellationToken);

        int added = 0, withoutWater = 0;
        if (refreshStations)
        {
            var locations = await client.GetWaterLevelLocationsAsync(cancellationToken);
            var shapes = await db.Waters.AsNoTracking()
                .Where(w => w.Shape != null)
                .Select(w => new { w.Id, w.Shape })
                .ToListAsync(cancellationToken);
            var waters = new WaterLocator(shapes.Select(w => (w.Id, w.Shape!)));

            foreach (var location in locations)
            {
                var water = waters.Find(location.Latitude, location.Longitude, options.Value.WaterDistance);
                if (!stations.TryGetValue(location.Code, out var station))
                {
                    if (water is null)
                    {
                        withoutWater++;
                        continue;
                    }
                    station = new Station { Id = Guid.NewGuid(), Created = now, Name = location.Name, ExternalCode = location.Code, Agency = Agency };
                    db.Stations.Add(station);
                    stations.Add(location.Code, station);
                    added++;
                }

                var changed = station.Name != location.Name
                    || station.Latitude != location.Latitude
                    || station.Longitude != location.Longitude
                    || (water is { } id && station.WaterId != id);
                if (!changed)
                    continue;

                station.Name = location.Name;
                station.Latitude = location.Latitude;
                station.Longitude = location.Longitude;
                // A station keeps its water when the shapes no longer reach it; it needs one anyway.
                if (water is { } waterId)
                    station.WaterId = waterId;
                if (station.Created != now)
                    station.Updated = now;
            }
        }

        var levels = stations.Count == 0
            ? new Dictionary<string, RijkswaterstaatMeasurement>()
            : await client.GetLatestWaterLevelsAsync(stations.Keys, cancellationToken);
        var updated = 0;
        foreach (var (code, level) in levels)
        {
            var measuredAt = level.MeasuredAt.UtcDateTime;
            if (now - measuredAt > options.Value.MaxAge || !stations.TryGetValue(code, out var station)
                || station.WaterLevelMeasuredAt == measuredAt)
                continue;

            station.WaterLevel = level.Value;
            station.WaterLevelMeasuredAt = measuredAt;
            // Rijkswaterstaat does not compare the level with mean low and high water as PEGELONLINE does.
            station.WaterLevelState = null;
            updated++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new RijkswaterstaatSyncResult(added, withoutWater, updated);
    }
}
