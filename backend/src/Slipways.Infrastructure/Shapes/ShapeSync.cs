using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Shapes;

public sealed record ShapeSyncResult(int AssignmentsChanged, int ShapesUpdated, IReadOnlyList<string> MissingElements);

/// <summary>
/// Assigns OpenStreetMap elements to waters from Seed/waterShapes.json and fetches their shapes from Nominatim.
/// Waters are matched by short name, which PEGELONLINE also uses, so waters it added are covered too.
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

        await db.SaveChangesAsync(cancellationToken);
        var missing = due.SelectMany(w => w.OsmElements).Distinct().Where(e => !geometries.ContainsKey(e)).ToList();
        return new ShapeSyncResult(assignmentsChanged, shapesUpdated, missing);
    }
}
