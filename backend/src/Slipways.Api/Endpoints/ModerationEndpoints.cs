using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Slipways.Domain;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Suggestions;

namespace Slipways.Api.Endpoints;

/// <summary>
/// The moderation of suggestions. It has no login: Program.cs answers it only on the moderation port, which is never
/// forwarded to the public and reaches the tailnet through "tailscale serve" (see deploy/README.md).
/// </summary>
public static class ModerationEndpoints
{
    public const string PagePath = "/moderation";
    public const string ApiPath = "/api/moderation";

    // Changes need this header. A page on another site cannot send it without a CORS preflight, which this API never
    // allows, so visiting a hostile site while in the tailnet cannot accept or reject anything.
    public const string RequestHeader = "X-Moderation";

    public static IEndpointRouteBuilder MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PagePath, () => Asset("index.html", "text/html; charset=utf-8")).ExcludeFromDescription();
        app.MapGet($"{PagePath}/app.js", () => Asset("app.js", "text/javascript; charset=utf-8")).ExcludeFromDescription();
        app.MapGet($"{PagePath}/app.css", () => Asset("app.css", "text/css; charset=utf-8")).ExcludeFromDescription();

        var api = app.MapGroup(ApiPath).WithTags("Moderation");
        api.MapGet("/suggestions", List);
        api.MapPost("/suggestions/{id:guid}/accept", Accept).AddEndpointFilter(RequireHeader);
        api.MapPost("/suggestions/{id:guid}/reject", Reject).AddEndpointFilter(RequireHeader);

        return app;
    }

    private static async ValueTask<object?> RequireHeader(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.Request.Headers.ContainsKey(RequestHeader)
            ? await next(context)
            : TypedResults.StatusCode(StatusCodes.Status403Forbidden);

    private static IResult Asset(string name, string contentType)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Slipways.Api.Moderation.{name}")
            ?? throw new InvalidOperationException($"Moderation asset {name} is missing.");
        return Results.Stream(stream, contentType);
    }

    private static async Task<List<ModerationSuggestionDto>> List(
        string? status, SuggestionService service, SlipwaysDbContext db, CancellationToken ct)
    {
        var suggestions = await service.ListAsync(status ?? SuggestionStatuses.Pending, ct);
        SlipwaysMetrics.SuggestionsPending.Set(await db.Suggestions.CountAsync(s => s.Status == SuggestionStatuses.Pending, ct));

        var result = new List<ModerationSuggestionDto>();
        foreach (var suggestion in suggestions)
        {
            var place = await service.FindPlaceAsync(suggestion.PlaceType, suggestion.PlaceId, ct);
            var infos = place is null
                ? []
                : await db.PlaceInfos.AsNoTracking()
                    .Where(i => i.Source == Sources.User && i.PlaceType == suggestion.PlaceType && i.PlaceId == place.Id)
                    .ToListAsync(ct);
            var fields = SuggestionService.FieldsOf(suggestion)
                .Select(f => new ModerationFieldDto(f.Key, f.Value,
                    infos.FirstOrDefault(i => i.Field == f.Key)?.Value
                    ?? (place is null ? null : SuggestionFields.All.GetValueOrDefault(f.Key)?.Read?.Invoke(place))))
                .ToList();
            var nearby = suggestion is { Kind: SuggestionKinds.NewSlipway, Status: SuggestionStatuses.Pending }
                ? (await service.NearbySlipwaysAsync(suggestion.Latitude!.Value, suggestion.Longitude!.Value, ct))
                    .Select(SuggestionEndpoints.ToDto).ToList()
                : [];
            result.Add(new ModerationSuggestionDto(
                suggestion.Id, suggestion.Kind, suggestion.Status, suggestion.Created, suggestion.Decided,
                suggestion.PlaceType, suggestion.PlaceId, place?.Name, place?.Source,
                suggestion.Name, suggestion.Latitude, suggestion.Longitude, fields, suggestion.Text,
                suggestion.TokenHash is null, nearby));
        }
        return result;
    }

    private static Task<Results<NoContent, NotFound, Conflict<string>>> Accept(
        Guid id, SuggestionService service, SlipwaysDbContext db, CancellationToken ct) =>
        Decide(id, db, ct, "accepted", suggestion => service.AcceptAsync(suggestion, ct));

    private static Task<Results<NoContent, NotFound, Conflict<string>>> Reject(
        Guid id, SuggestionService service, SlipwaysDbContext db, CancellationToken ct) =>
        Decide(id, db, ct, "rejected", suggestion => service.RejectAsync(suggestion, ct));

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> Decide(
        Guid id, SlipwaysDbContext db, CancellationToken ct, string decision, Func<Suggestion, Task<string?>> decide)
    {
        if (await db.Suggestions.SingleOrDefaultAsync(s => s.Id == id, ct) is not { } suggestion)
            return TypedResults.NotFound();
        if (await decide(suggestion) is { } error)
            return TypedResults.Conflict(error);
        SlipwaysMetrics.SuggestionsDecided.WithLabels(decision).Inc();
        return TypedResults.NoContent();
    }
}
