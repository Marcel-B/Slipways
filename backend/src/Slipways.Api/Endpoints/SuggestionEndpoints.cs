using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Suggestions;

namespace Slipways.Api.Endpoints;

/// <summary>Suggestions from users without an account; see <c>docs/nutzerbeitraege.md</c>.</summary>
public static class SuggestionEndpoints
{
    public static IEndpointRouteBuilder MapSuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        var suggestions = app.MapGroup("/api/suggestions").WithTags("Suggestions");

        suggestions.MapGet("/challenge", Challenge).RequireRateLimiting(RateLimiting.ApiPolicy);
        suggestions.MapPost("/", Submit).RequireRateLimiting(RateLimiting.SuggestionPolicy);
        suggestions.MapGet("/{id:guid}", Status).RequireRateLimiting(RateLimiting.ApiPolicy);
        suggestions.MapDelete("/{id:guid}", Withdraw).RequireRateLimiting(RateLimiting.SuggestionPolicy);

        app.MapGet("/api/contributions/odbl", OdblContributions).WithTags("Suggestions").RequireRateLimiting(RateLimiting.ApiPolicy);

        return app;
    }

    private static Results<Ok<ProofChallengeDto>, ProblemHttpResult> Challenge(ProofOfWork proof, IOptions<SuggestionOptions> options)
    {
        if (!options.Value.Enabled)
            return Disabled();
        var challenge = proof.CreateChallenge();
        return TypedResults.Ok(new ProofChallengeDto(
            challenge.Algorithm, challenge.Challenge, challenge.MaxNumber, challenge.Salt, challenge.Signature));
    }

    private static async Task<Results<Accepted<SuggestionCreatedDto>, Conflict<NearbySlipwaysDto>, NotFound, ProblemHttpResult>> Submit(
        SuggestionRequestDto request, SuggestionService service, ProofOfWork proof, IOptions<SuggestionOptions> options,
        CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return Disabled();

        // Bots that fill in every field get an answer that looks like success, so they have nothing to learn from.
        if (!string.IsNullOrEmpty(request.Homepage))
        {
            Count("honeypot");
            return TypedResults.Accepted((string?)null, new SuggestionCreatedDto(Guid.NewGuid(), Guid.NewGuid().ToString("N")));
        }

        if (request.Proof is not { } solution
            || !proof.Verify(new ProofSolution(solution.Algorithm, solution.Challenge, solution.Number, solution.Salt, solution.Signature)))
        {
            Count("proof_failed");
            return TypedResults.Problem(
                "Der Spam-Schutz ist abgelaufen oder ungültig. Bitte noch einmal senden.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await service.SubmitAsync(new SuggestionInput(
            request.Kind, request.PlaceType, request.PlaceId, request.Name, request.Latitude, request.Longitude,
            request.Fields, request.Text, request.ConfirmNew), ct);
        switch (result)
        {
            case SubmitResult.Created created:
                Count("queued");
                return TypedResults.Accepted($"/api/suggestions/{created.Suggestion.Id}", new SuggestionCreatedDto(created.Suggestion.Id, created.Token));
            case SubmitResult.Nearby nearby:
                Count("nearby");
                return TypedResults.Conflict(new NearbySlipwaysDto(nearby.Places.Select(ToDto).ToList()));
            case SubmitResult.NotFound:
                Count("invalid");
                return TypedResults.NotFound();
            case SubmitResult.Invalid invalid:
            default:
                Count("invalid");
                return TypedResults.Problem((result as SubmitResult.Invalid)?.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<Results<Ok<SuggestionStatusDto>, NotFound>> Status(
        Guid id, string? token, SuggestionService service, CancellationToken ct) =>
        await service.FindForSenderAsync(id, token, ct) is { } suggestion
            ? TypedResults.Ok(new SuggestionStatusDto(suggestion.Id, suggestion.Kind, suggestion.Status, suggestion.Created, suggestion.Decided))
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound, Conflict>> Withdraw(
        Guid id, string? token, SuggestionService service, CancellationToken ct)
    {
        if (await service.FindForSenderAsync(id, token, ct) is not { } suggestion)
            return TypedResults.NotFound();
        return await service.WithdrawAsync(suggestion, ct) ? TypedResults.NoContent() : TypedResults.Conflict();
    }

    private static async Task<OdblContributionsDto> OdblContributions(SlipwaysDbContext db, CancellationToken ct)
    {
        var osmSlipways = db.Slipways.Where(s => s.Source == Sources.Osm).Select(s => s.Id);
        var osmMarinas = db.Marinas.Where(m => m.Source == Sources.Osm).Select(m => m.Id);
        var values = await db.PlaceInfos.AsNoTracking()
            .Where(i => i.Source == Sources.User && i.OsmId != null
                        && ((i.PlaceType == PlaceTypes.Slipway && osmSlipways.Contains(i.PlaceId))
                            || (i.PlaceType == PlaceTypes.Marina && osmMarinas.Contains(i.PlaceId))))
            .OrderBy(i => i.PlaceType).ThenBy(i => i.OsmType).ThenBy(i => i.OsmId).ThenBy(i => i.Field)
            .Select(i => new OdblContributionDto(i.PlaceType, i.OsmType + "/" + i.OsmId, i.Field, i.Value, i.Retrieved))
            .ToListAsync(ct);
        return new OdblContributionsDto(
            "Open Database License (ODbL) 1.0, https://opendatacommons.org/licenses/odbl/1-0/",
            "Slipways (https://slipways.de) und © OpenStreetMap-Mitwirkende", values);
    }

    public static NearbySlipwayDto ToDto(NearbyPlace place) => new(place.Id, place.Name, place.Distance, place.Source);

    private static void Count(string result) => SlipwaysMetrics.SuggestionsReceived.WithLabels(result).Inc();

    private static ProblemHttpResult Disabled() =>
        TypedResults.Problem("Beiträge sind gerade abgeschaltet.", statusCode: StatusCodes.Status503ServiceUnavailable);
}
