using Microsoft.EntityFrameworkCore;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Gauges;

public sealed record GaugeSyncResult(int StationsAdded, int WatersAdded, int WaterLevelsUpdated);

/// <summary>
/// Brings stations, their waters and the latest water levels in line with PEGELONLINE.
/// Stations are matched by PEGELONLINE id, falling back to the gauge number for seeded stations.
/// Stations that disappear upstream are kept; their water level simply stops changing.
/// </summary>
public sealed class GaugeSync(SlipwaysDbContext db, PegelonlineClient client, TimeProvider time)
{
    public async Task<GaugeSyncResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var remoteStations = await client.GetStationsAsync(cancellationToken);
        var now = time.GetUtcNow().UtcDateTime;

        var waters = (await db.Waters.ToListAsync(cancellationToken))
            .Where(w => w.Shortname is not null)
            .GroupBy(w => w.Shortname!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var stations = await db.Stations.ToListAsync(cancellationToken);
        var stationsByExternalId = stations.Where(s => s.ExternalId is not null).ToDictionary(s => s.ExternalId!.Value);
        var stationsByNumber = stations.Where(s => s.Number is not null).GroupBy(s => s.Number!).ToDictionary(g => g.Key, g => g.First());

        int stationsAdded = 0, watersAdded = 0, levelsUpdated = 0;

        foreach (var remote in remoteStations)
        {
            // A few stations have no coordinates; without them they cannot be shown on the map.
            if (remote.Latitude is not { } latitude || remote.Longitude is not { } longitude)
                continue;

            if (!waters.TryGetValue(remote.Water.Shortname, out var water))
            {
                water = new Water
                {
                    Id = Guid.NewGuid(),
                    Created = now,
                    Name = SeedData.ToTitleCase(remote.Water.Longname),
                    Shortname = remote.Water.Shortname,
                };
                db.Waters.Add(water);
                waters.Add(remote.Water.Shortname, water);
                watersAdded++;
            }

            var isNew = false;
            if (!stationsByExternalId.TryGetValue(remote.Uuid, out var station) &&
                !stationsByNumber.TryGetValue(remote.Number, out station))
            {
                station = new Station { Id = Guid.NewGuid(), Created = now, Name = remote.Longname };
                isNew = true;
                db.Stations.Add(station);
                stationsAdded++;
            }

            var changed = station.ExternalId != remote.Uuid
                || station.Name != remote.Longname
                || station.Number != remote.Number
                || station.Shortname != remote.Shortname
                || station.Latitude != latitude
                || station.Longitude != longitude
                || station.Km != (remote.Km ?? 0)
                || station.Agency != remote.Agency
                || station.WaterId != water.Id;

            if (changed)
            {
                station.ExternalId = remote.Uuid;
                station.Name = remote.Longname;
                station.Number = remote.Number;
                station.Shortname = remote.Shortname;
                station.Latitude = latitude;
                station.Longitude = longitude;
                station.Km = remote.Km ?? 0;
                station.Agency = remote.Agency;
                station.Water = water;
                if (!isNew)
                    station.Updated = now;
            }

            var measurement = remote.Timeseries?.FirstOrDefault(t => t.Shortname == "W")?.CurrentMeasurement;
            if (measurement is not null && station.WaterLevelMeasuredAt != measurement.Timestamp.UtcDateTime)
            {
                station.WaterLevel = measurement.Value;
                station.WaterLevelMeasuredAt = measurement.Timestamp.UtcDateTime;
                station.WaterLevelState = measurement.StateMnwMhw;
                levelsUpdated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new GaugeSyncResult(stationsAdded, watersAdded, levelsUpdated);
    }
}
