using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Shapes;

namespace Slipways.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/slipways", ListSlipways).WithTags("Slipways");
        api.MapGet("/slipways/{id:guid}", GetSlipway).WithTags("Slipways");
        api.MapGet("/waters", ListWaters).WithTags("Waters");
        api.MapGet("/waters/shapes", ListWaterShapes).WithTags("Waters");
        api.MapGet("/waters/{id:guid}", GetWater).WithTags("Waters");
        api.MapGet("/marinas", ListMarinas).WithTags("Marinas");
        api.MapGet("/marinas/{id:guid}", GetMarina).WithTags("Marinas");
        api.MapGet("/extras", ListExtras).WithTags("Extras");
        api.MapGet("/services", ListServices).WithTags("Services");
        api.MapGet("/stations", ListStations).WithTags("Stations");

        return app;
    }

    private static async Task<List<SlipwaySummaryDto>> ListSlipways(
        SlipwaysDbContext db, string? search, Guid? waterId, CancellationToken ct)
    {
        var query = db.Slipways.AsNoTracking();

        if (waterId is { } water)
            query = query.Where(s => s.WaterId == water);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.City != null && s.City.ToLower().Contains(term)) ||
                (s.Water != null && s.Water.Name.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SlipwaySummaryDto(
                s.Id, s.Name, s.Latitude, s.Longitude, s.PostalCode, s.City, s.Country,
                s.Water == null ? null : new WaterDto(s.Water.Id, s.Water.Name),
                s.Source))
            .ToListAsync(ct);
    }

    private static async Task<Results<Ok<SlipwayDetailsDto>, NotFound>> GetSlipway(
        SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var slipway = await db.Slipways.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SlipwayDetailsDto(
                s.Id, s.Name, s.Latitude, s.Longitude, s.Street, s.PostalCode, s.City, s.Country, s.Url,
                s.Comment, s.Rating, s.Costs, s.Pro, s.Contra,
                s.Operator, s.Fee, s.Access, s.OpeningHours, s.Surface,
                s.Water == null ? null : new WaterDto(s.Water.Id, s.Water.Name),
                s.Marina == null ? null : new MarinaSummaryDto(s.Marina.Id, s.Marina.Name, s.Marina.Url),
                s.Extras.OrderBy(e => e.Name).Select(e => e.Name).ToList(),
                s.Source, s.OsmId == null ? null : s.OsmType + "/" + s.OsmId,
                s.Created, s.Updated))
            .SingleOrDefaultAsync(ct);

        return slipway is null ? TypedResults.NotFound() : TypedResults.Ok(slipway);
    }

    private static IQueryable<Water> SearchWaters(SlipwaysDbContext db, string? search)
    {
        var query = db.Waters.AsNoTracking();
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim().ToLower();
        return query.Where(w =>
            w.Name.ToLower().Contains(term) ||
            (w.Shortname != null && w.Shortname.ToLower().Contains(term)));
    }

    private static async Task<List<WaterSummaryDto>> ListWaters(
        SlipwaysDbContext db, string? search, CancellationToken ct)
    {
        var waters = await SearchWaters(db, search)
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, w.Shortname, HasShape = w.Shape != null })
            .ToListAsync(ct);

        // The position comes from what lies on a water, which is far less data than its shape.
        var slipways = (await db.Slipways.AsNoTracking()
            .Select(s => new { s.WaterId, Position = new PositionDto(s.Latitude, s.Longitude) })
            .ToListAsync(ct)).ToLookup(s => s.WaterId, s => s.Position);
        var marinas = (await db.Marinas.AsNoTracking()
            .Select(m => new { m.WaterId, Position = new PositionDto(m.Latitude, m.Longitude) })
            .ToListAsync(ct)).ToLookup(m => m.WaterId, m => m.Position);
        var stations = (await db.Stations.AsNoTracking()
            .Select(s => new
            {
                s.WaterId,
                Position = new PositionDto(s.Latitude, s.Longitude),
                Gauge = s.WaterLevel == null || s.WaterLevelMeasuredAt == null
                    ? null
                    : new GaugeReadingDto(s.Id, s.Name,
                        new WaterLevelDto(s.WaterLevel.Value, s.WaterLevelMeasuredAt.Value, s.WaterLevelState)),
            })
            .ToListAsync(ct)).ToLookup(s => s.WaterId);

        // Only waters with nothing on them fall back to their shape.
        var bare = waters
            .Where(w => w.HasShape && !slipways.Contains(w.Id) && !marinas.Contains(w.Id) && !stations.Contains(w.Id))
            .Select(w => w.Id)
            .ToList();
        var shapes = bare.Count == 0
            ? []
            : await db.Waters.AsNoTracking()
                .Where(w => bare.Contains(w.Id))
                .Select(w => new { w.Id, w.Shape })
                .ToDictionaryAsync(w => w.Id, w => ShapePositions(w.Shape!).ToList(), ct);

        return waters.Select(w =>
        {
            var waterStations = stations[w.Id].ToList();
            var positions = slipways[w.Id].Concat(marinas[w.Id]).Concat(waterStations.Select(s => s.Position))
                .Concat(shapes.GetValueOrDefault(w.Id) ?? [])
                .ToList();
            return new WaterSummaryDto(
                w.Id, w.Name, w.Shortname, slipways[w.Id].Count(), waterStations.Count,
                MarkerPosition(positions),
                waterStations.Count == 1 ? waterStations[0].Gauge : null);
        }).ToList();
    }

    private static async Task<Results<Ok<WaterDetailsDto>, NotFound>> GetWater(
        SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var water = await db.Waters.AsNoTracking().SingleOrDefaultAsync(w => w.Id == id, ct);
        if (water is null)
            return TypedResults.NotFound();

        var slipways = await ListSlipways(db, null, id, ct);
        var stations = await ListStations(db, id, ct);
        var marinas = await ListMarinas(db, null, id, ct);
        var positions = slipways.Select(s => new PositionDto(s.Latitude, s.Longitude))
            .Concat(stations.Select(s => new PositionDto(s.Latitude, s.Longitude)))
            .Concat(marinas.Select(m => new PositionDto(m.Latitude, m.Longitude)))
            .Concat(water.Shape is null ? [] : ShapePositions(water.Shape))
            .ToList();

        return TypedResults.Ok(new WaterDetailsDto(
            water.Id, water.Name, water.Shortname, BoundsOf(positions),
            water.Shape is null ? null : JsonDocument.Parse(water.Shape).RootElement.Clone(),
            water.OsmElements, slipways, stations, marinas));
    }

    // About 500 m: invisible on a map of Germany, and it brings all shapes from 565 KB down to under 200 KB.
    private const double OverviewTolerance = 0.005;

    /// <summary>Shapes of the waters a search finds, thinned out for drawing them all on one map.</summary>
    private static async Task<WaterShapeCollectionDto> ListWaterShapes(
        SlipwaysDbContext db, string? search, CancellationToken ct)
    {
        var shapes = await SearchWaters(db, search)
            .Where(w => w.Shape != null)
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, w.Shape })
            .ToListAsync(ct);

        return new WaterShapeCollectionDto(shapes
            .Select(w => new WaterShapeDto(
                w.Id, new WaterShapePropertiesDto(w.Name),
                ShapeSimplifier.Simplify(JsonNode.Parse(w.Shape!)!, OverviewTolerance)))
            .ToList());
    }

    /// <summary>Every vertex of a GeoJSON geometry.</summary>
    private static IEnumerable<PositionDto> ShapePositions(string shape)
    {
        using var document = JsonDocument.Parse(shape);
        return Vertices(document.RootElement).ToList();

        static IEnumerable<PositionDto> Vertices(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var parts = element.TryGetProperty("geometries", out var geometries) ? geometries : element.GetProperty("coordinates");
                foreach (var vertex in Vertices(parts))
                    yield return vertex;
            }
            else if (element[0].ValueKind == JsonValueKind.Number)
            {
                // GeoJSON puts the longitude first.
                yield return new PositionDto(element[1].GetDouble(), element[0].GetDouble());
            }
            else
            {
                foreach (var vertex in element.EnumerateArray().SelectMany(Vertices))
                    yield return vertex;
            }
        }
    }

    private static BoundsDto? BoundsOf(IReadOnlyCollection<PositionDto> positions) =>
        positions.Count == 0
            ? null
            : new BoundsDto(
                positions.Min(p => p.Latitude), positions.Min(p => p.Longitude),
                positions.Max(p => p.Latitude), positions.Max(p => p.Longitude));

    /// <summary>
    /// The position closest to the middle of the bounds. Unlike the middle itself, it lies on the water even
    /// when the water is a winding river.
    /// </summary>
    private static PositionDto? MarkerPosition(IReadOnlyCollection<PositionDto> positions)
    {
        if (BoundsOf(positions) is not { } bounds)
            return null;

        var latitude = (bounds.South + bounds.North) / 2;
        var longitude = (bounds.West + bounds.East) / 2;
        return positions.MinBy(p => Math.Pow(p.Latitude - latitude, 2) + Math.Pow(p.Longitude - longitude, 2));
    }

    private static Task<List<MarinaDto>> ListMarinas(
        SlipwaysDbContext db, string? search, Guid? waterId, CancellationToken ct)
    {
        var query = db.Marinas.AsNoTracking();

        if (waterId is { } water)
            query = query.Where(m => m.WaterId == water);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                (m.City != null && m.City.ToLower().Contains(term)) ||
                (m.Water != null && m.Water.Name.ToLower().Contains(term)));
        }

        return query
            .OrderBy(m => m.Name)
            .Select(m => new MarinaDto(
                m.Id, m.Name, m.Latitude, m.Longitude, m.Url, m.Street, m.PostalCode, m.City, m.Phone, m.Email,
                m.Operator, m.Fee, m.Access, m.OpeningHours,
                m.Water == null ? null : new WaterDto(m.Water.Id, m.Water.Name),
                m.Source, m.OsmId == null ? null : m.OsmType + "/" + m.OsmId))
            .ToListAsync(ct);
    }

    private static async Task<Results<Ok<MarinaDetailsDto>, NotFound>> GetMarina(
        SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var marina = await db.Marinas.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MarinaDetailsDto(
                m.Id, m.Name, m.Latitude, m.Longitude, m.Url, m.Street, m.PostalCode, m.City, m.Phone, m.Email,
                m.Operator, m.Fee, m.Access, m.OpeningHours,
                m.Water == null ? null : new WaterDto(m.Water.Id, m.Water.Name),
                m.Source, m.OsmId == null ? null : m.OsmType + "/" + m.OsmId,
                m.Slipways.OrderBy(s => s.Name)
                    .Select(s => new SlipwaySummaryDto(
                        s.Id, s.Name, s.Latitude, s.Longitude, s.PostalCode, s.City, s.Country,
                        s.Water == null ? null : new WaterDto(s.Water.Id, s.Water.Name),
                        s.Source))
                    .ToList()))
            .SingleOrDefaultAsync(ct);

        return marina is null ? TypedResults.NotFound() : TypedResults.Ok(marina);
    }

    private static Task<List<ExtraDto>> ListExtras(SlipwaysDbContext db, CancellationToken ct) =>
        db.Extras.AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new ExtraDto(e.Id, e.Name))
            .ToListAsync(ct);

    private static Task<List<ServiceDto>> ListServices(SlipwaysDbContext db, CancellationToken ct) =>
        db.Services.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto(
                s.Id, s.Name, s.Latitude, s.Longitude, s.Url, s.Street, s.PostalCode, s.City, s.Phone, s.Email,
                s.Manufacturers.OrderBy(m => m.Name).Select(m => m.Name).ToList()))
            .ToListAsync(ct);

    private static Task<List<StationDto>> ListStations(SlipwaysDbContext db, Guid? waterId, CancellationToken ct) =>
        db.Stations.AsNoTracking()
            .Where(s => waterId == null || s.WaterId == waterId)
            .OrderBy(s => s.Name)
            .Select(s => new StationDto(
                s.Id, s.Name, s.Number, s.Latitude, s.Longitude, s.Km, s.Agency,
                new WaterDto(s.Water.Id, s.Water.Name),
                s.WaterLevel == null || s.WaterLevelMeasuredAt == null
                    ? null
                    : new WaterLevelDto(s.WaterLevel.Value, s.WaterLevelMeasuredAt.Value, s.WaterLevelState)))
            .ToListAsync(ct);
}
