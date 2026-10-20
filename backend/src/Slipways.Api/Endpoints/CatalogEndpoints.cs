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

    private static Task<List<WaterDto>> ListWaters(SlipwaysDbContext db, CancellationToken ct) =>
        db.Waters.AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new WaterDto(w.Id, w.Name))
            .ToListAsync(ct);

    private static Task<List<MarinaDto>> ListMarinas(SlipwaysDbContext db, CancellationToken ct) =>
        db.Marinas.AsNoTracking()
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
