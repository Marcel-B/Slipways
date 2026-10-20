using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/slipways", ListSlipways).WithTags("Slipways");
        api.MapGet("/slipways/{id:guid}", GetSlipway).WithTags("Slipways");
        api.MapGet("/waters", ListWaters).WithTags("Waters");
        api.MapGet("/waters/{id:guid}", GetWater).WithTags("Waters");
        api.MapGet("/marinas", ListMarinas).WithTags("Marinas");
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
                s.Water.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SlipwaySummaryDto(
                s.Id, s.Name, s.Latitude, s.Longitude, s.PostalCode, s.City, s.Country,
                new WaterDto(s.Water.Id, s.Water.Name)))
            .ToListAsync(ct);
    }

    private static async Task<Results<Ok<SlipwayDetailsDto>, NotFound>> GetSlipway(
        SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var slipway = await db.Slipways.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SlipwayDetailsDto(
                s.Id, s.Name, s.Latitude, s.Longitude, s.Street, s.PostalCode, s.City, s.Country,
                s.Comment, s.Rating, s.Costs, s.Pro, s.Contra,
                new WaterDto(s.Water.Id, s.Water.Name),
                s.Marina == null ? null : new MarinaSummaryDto(s.Marina.Id, s.Marina.Name, s.Marina.Url),
                s.Extras.OrderBy(e => e.Name).Select(e => e.Name).ToList(),
                s.Created, s.Updated))
            .SingleOrDefaultAsync(ct);

        return slipway is null ? TypedResults.NotFound() : TypedResults.Ok(slipway);
    }

    private static async Task<List<WaterSummaryDto>> ListWaters(
        SlipwaysDbContext db, string? search, CancellationToken ct)
    {
        var query = db.Waters.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(w =>
                w.Name.ToLower().Contains(term) ||
                (w.Shortname != null && w.Shortname.ToLower().Contains(term)));
        }

        var waters = await query
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, w.Shortname })
            .ToListAsync(ct);

        // Waters have no geometry of their own, so their position comes from what lies on them.
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

        return waters.Select(w =>
        {
            var waterStations = stations[w.Id].ToList();
            var positions = slipways[w.Id].Concat(marinas[w.Id]).Concat(waterStations.Select(s => s.Position)).ToList();
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
        var marinas = await ListMarinas(db, id, ct);
        var positions = slipways.Select(s => new PositionDto(s.Latitude, s.Longitude))
            .Concat(stations.Select(s => new PositionDto(s.Latitude, s.Longitude)))
            .Concat(marinas.Select(m => new PositionDto(m.Latitude, m.Longitude)))
            .ToList();

        return TypedResults.Ok(new WaterDetailsDto(
            water.Id, water.Name, water.Shortname, BoundsOf(positions), slipways, stations, marinas));
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

    private static Task<List<MarinaDto>> ListMarinas(SlipwaysDbContext db, Guid? waterId, CancellationToken ct) =>
        db.Marinas.AsNoTracking()
            .Where(m => waterId == null || m.WaterId == waterId)
            .OrderBy(m => m.Name)
            .Select(m => new MarinaDto(
                m.Id, m.Name, m.Latitude, m.Longitude, m.Url, m.Street, m.PostalCode, m.City, m.Phone, m.Email,
                new WaterDto(m.Water.Id, m.Water.Name)))
            .ToListAsync(ct);

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
