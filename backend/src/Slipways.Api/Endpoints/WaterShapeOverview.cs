using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Shapes;

namespace Slipways.Api.Endpoints;

/// <summary>A water's shape thinned out for the overview map, with its bounds to pick the shapes in a map view.</summary>
public sealed record WaterShapeEntry(Guid Id, string Name, BoundsDto Bounds, string Geometry);

/// <summary>
/// The thinned-out shapes of all waters, kept in memory. Thinning out thousands of shapes takes seconds, and they only
/// change when the shape job fetches new ones; the number of shapes and the newest fetch tell when to build them again.
/// </summary>
public sealed class WaterShapeOverview
{
    // About 500 m: invisible on a map of a whole region, and it brings the shapes down to a fraction.
    private const double Tolerance = 0.005;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private (int Count, DateTime? Newest, IReadOnlyList<WaterShapeEntry> Entries)? _built;

    public async Task<IReadOnlyList<WaterShapeEntry>> GetAsync(SlipwaysDbContext db, CancellationToken ct)
    {
        var withShape = db.Waters.AsNoTracking().Where(w => w.Shape != null);
        var count = await withShape.CountAsync(ct);
        var newest = await withShape.MaxAsync(w => w.ShapeUpdated, ct);
        if (_built is { } built && built.Count == count && built.Newest == newest)
            return built.Entries;

        await _lock.WaitAsync(ct);
        try
        {
            if (_built is { } current && current.Count == count && current.Newest == newest)
                return current.Entries;

            var shapes = await withShape
                .OrderBy(w => w.Name)
                .Select(w => new { w.Id, w.Name, w.Shape })
                .ToListAsync(ct);
            var entries = shapes
                .Select(w =>
                {
                    var geometry = ShapeSimplifier.Simplify(JsonNode.Parse(w.Shape!)!, Tolerance).ToJsonString();
                    return CatalogEndpoints.ShapeBounds(geometry) is { } bounds ? new WaterShapeEntry(w.Id, w.Name, bounds, geometry) : null;
                })
                .OfType<WaterShapeEntry>()
                .ToList();
            _built = (count, newest, entries);
            return entries;
        }
        finally
        {
            _lock.Release();
        }
    }
}
