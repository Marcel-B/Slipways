using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Osm;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Shapes;

/// <param name="PlacesAssigned">Slipways and marinas without a water that lie on one of the new shapes.</param>
public sealed record ShapeSyncResult(int AssignmentsChanged, int ShapesUpdated, IReadOnlyList<string> MissingElements, int PlacesAssigned = 0);

/// <summary>
/// Assigns OpenStreetMap elements to waters from Seed/waterShapes.json and fetches their shapes from Nominatim.
/// Waters are matched by short name, which PEGELONLINE also uses, so waters it added are covered too. Waters the
/// OpenStreetMap import added bring their elements along. When shapes changed, places without a water are put on
/// the closest one, so they need not wait for the next import of their country.
/// </summary>
public sealed class ShapeSync(SlipwaysDbContext db, NominatimClient client, IOptions<NominatimOptions> options, TimeProvider time)
{
    public async Task<ShapeSyncResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var waters = await db.Waters.ToListAsync(cancellationToken);

        var assignments = SeedData.WaterShapes();
        var assignmentsChanged = 0;
        foreach (var water in waters)
        {
            if (water.Shortname is null || !assignments.TryGetValue(water.Shortname, out var elements)
                || water.OsmElements.SequenceEqual(elements))
                continue;

            water.OsmElements = [.. elements];
            water.Shape = null;
            water.ShapeUpdated = null;
            water.Updated = now;
            assignmentsChanged++;
        }

        var due = waters
            .Where(w => w.OsmElements.Count > 0 && (w.ShapeUpdated is null || w.ShapeUpdated < now - options.Value.RefreshAfter))
            .ToList();
        var geometries = due.Count == 0
            ? new Dictionary<string, JsonElement>()
            : await client.GetGeometriesAsync(due.SelectMany(w => w.OsmElements).Distinct().ToList(), cancellationToken);

        var shapesUpdated = 0;
        foreach (var water in due)
        {
            // Asking again tomorrow would not help when Nominatim does not know an element, so that waits for the refresh too.
            water.ShapeUpdated = now;
            var parts = water.OsmElements.Where(geometries.ContainsKey).Select(e => geometries[e]).ToList();
            if (parts.Count == 0)
                continue;

            water.Shape = parts.Count == 1
                ? parts[0].GetRawText()
                : JsonSerializer.Serialize(new { type = "GeometryCollection", geometries = parts });
            shapesUpdated++;
        }

        var placesAssigned = shapesUpdated > 0 ? await AssignPlacesAsync(waters, now, cancellationToken) : 0;

        await db.SaveChangesAsync(cancellationToken);
        var missing = due.SelectMany(w => w.OsmElements).Distinct().Where(e => !geometries.ContainsKey(e)).ToList();
        return new ShapeSyncResult(assignmentsChanged, shapesUpdated, missing, placesAssigned);
    }

    private async Task<int> AssignPlacesAsync(List<Water> waters, DateTime now, CancellationToken cancellationToken)
    {
        var locator = new WaterLocator(waters.Where(w => w.Shape is not null).Select(w => (w.Id, w.Shape!)));
        var places = (await db.Slipways.Where(s => s.WaterId == null).ToListAsync(cancellationToken)).Cast<IOsmPlace>()
            .Concat(await db.Marinas.Where(m => m.WaterId == null).ToListAsync(cancellationToken));

        var assigned = 0;
        foreach (var place in places)
        {
            if (locator.Find(place.Latitude, place.Longitude, OsmImport.WaterDistance) is not { } water)
                continue;
            place.WaterId = water;
            ((Entity)place).Updated = now;
            assigned++;
        }
        return assigned;
    }
}
