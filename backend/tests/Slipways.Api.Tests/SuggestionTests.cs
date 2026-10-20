using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Slipways.Api.Endpoints;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Suggestions;

namespace Slipways.Api.Tests;

public sealed class SuggestionTests : IAsyncLifetime
{
    private static readonly Guid KelheimId = Guid.Parse("EAC96A93-FD47-4A9A-8771-0005805B9655");

    private readonly SlipwaysApiFactory _factory = new();
    private HttpClient _client = null!;
    private HttpClient _moderation = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        // A small challenge keeps the tests fast; the server checks it all the same.
        _client = _factory.WithWebHostBuilder(b => b.UseSetting("Suggestions:ChallengeMaxNumber", "500")).CreateClient();
        // The test server reports local port 0, so that is the moderation port here.
        _moderation = _factory.WithWebHostBuilder(b => b.UseSetting("Moderation:Port", "0")).CreateClient();
        _moderation.DefaultRequestHeaders.Add(ModerationEndpoints.RequestHeader, "1");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Rejects_suggestions_without_a_valid_proof_and_each_proof_counts_once()
    {
        var withoutProof = await _client.PostAsJsonAsync("/api/suggestions", Tip(KelheimId) with { Proof = null }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, withoutProof.StatusCode);

        var proof = await SolveAsync();
        var forged = await _client.PostAsJsonAsync("/api/suggestions", Tip(KelheimId) with { Proof = proof with { Number = proof.Number + 1 } }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);

        Assert.Equal(HttpStatusCode.Accepted, (await _client.PostAsJsonAsync("/api/suggestions", Tip(KelheimId) with { Proof = proof }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/suggestions", Tip(KelheimId) with { Proof = proof }, Ct)).StatusCode);
    }

    [Fact]
    public async Task Answers_bots_that_fill_in_the_hidden_field_as_if_it_worked_but_keeps_nothing()
    {
        var response = await SubmitAsync(Tip(KelheimId) with { Homepage = "https://spam.example" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, await CountAsync());
    }

    [Theory]
    [InlineData("fee", "vielleicht")]
    [InlineData("phone", "+49 4401 12345")]
    [InlineData("surface", "lava")]
    [InlineData("operator", "Besuche https://spam.example")]
    public async Task Rejects_values_that_do_not_fit_the_field(string field, string value)
    {
        var response = await SubmitAsync(Edit(PlaceTypes.Slipway, KelheimId, field, value));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync());
    }

    [Fact]
    public async Task Rejects_links_in_tips()
    {
        var response = await SubmitAsync(Tip(KelheimId) with { Text = "Mehr unter www.example.de" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Shows_slipways_nearby_before_taking_a_new_one()
    {
        var kelheim = await WithDbAsync(db => db.Slipways.SingleAsync(s => s.Id == KelheimId));
        var request = new SuggestionRequestDto(SuggestionKinds.NewSlipway, null, null, "Slip am Hafen",
            kelheim.Latitude + 0.0002, kelheim.Longitude, new() { ["fee"] = "no" }, null, false, null, null);

        var nearby = await SubmitAsync(request);
        Assert.Equal(HttpStatusCode.Conflict, nearby.StatusCode);
        var places = await nearby.Content.ReadFromJsonAsync<NearbySlipwaysDto>(Ct);
        Assert.Contains(places!.Slipways, s => s.Id == KelheimId && s.Distance < 30);

        Assert.Equal(HttpStatusCode.Accepted, (await SubmitAsync(request with { ConfirmNew = true })).StatusCode);
        // The same spot again is already waiting.
        Assert.Equal(HttpStatusCode.BadRequest, (await SubmitAsync(request with { ConfirmNew = true })).StatusCode);
    }

    [Fact]
    public async Task Rejects_new_slipways_outside_europe()
    {
        var request = new SuggestionRequestDto(SuggestionKinds.NewSlipway, null, null, "Slip auf Réunion",
            -20.94, 55.29, null, null, true, null, null);

        Assert.Equal(HttpStatusCode.BadRequest, (await SubmitAsync(request)).StatusCode);
    }

    [Fact]
    public async Task The_sender_can_look_at_and_withdraw_a_suggestion_with_the_token_only()
    {
        var created = await (await SubmitAsync(Tip(KelheimId))).Content.ReadFromJsonAsync<SuggestionCreatedDto>(Ct);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/suggestions/{created!.Id}?token=wrong", Ct)).StatusCode);
        var status = await _client.GetFromJsonAsync<SuggestionStatusDto>($"/api/suggestions/{created.Id}?token={created.Token}", Ct);
        Assert.Equal(SuggestionStatuses.Pending, status!.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/suggestions/{created.Id}?token={created.Token}", Ct)).StatusCode);
        Assert.Equal(0, await CountAsync());
    }

    [Fact]
    public async Task Moderation_is_only_answered_on_its_own_port_and_changes_need_the_header()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/moderation", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/moderation/suggestions", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _moderation.GetAsync("/api/slipways", Ct)).StatusCode);

        var page = await _moderation.GetAsync("/moderation", Ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("script-src 'self'", page.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal(HttpStatusCode.OK, (await _moderation.GetAsync("/moderation/app.js", Ct)).StatusCode);

        var created = await (await SubmitAsync(Tip(KelheimId))).Content.ReadFromJsonAsync<SuggestionCreatedDto>(Ct);
        var withoutHeader = _factory.WithWebHostBuilder(b => b.UseSetting("Moderation:Port", "0")).CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await withoutHeader.PostAsync($"/api/moderation/suggestions/{created!.Id}/accept", null, Ct)).StatusCode);
    }

    [Fact]
    public async Task Accepted_values_go_into_our_own_slipways_directly()
    {
        var created = await CreateAsync(Edit(PlaceTypes.Slipway, KelheimId, "surface", "concrete") with
        {
            Fields = new() { ["surface"] = "concrete", ["costs"] = "5,50", ["jetty"] = "yes" },
        });

        var pending = await _moderation.GetFromJsonAsync<List<ModerationSuggestionDto>>("/api/moderation/suggestions", Ct);
        var shown = Assert.Single(pending!, s => s.Id == created.Id);
        Assert.Equal("Am Jachthafen, Kelheim", shown.PlaceName);
        Assert.Contains(shown.Fields, f => f is { Field: "costs", Value: "5.5" });

        Assert.Equal(HttpStatusCode.NoContent, (await AcceptAsync(created.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await AcceptAsync(created.Id)).StatusCode);

        var slipway = await _client.GetFromJsonAsync<SlipwayDetailsDto>($"/api/slipways/{KelheimId}", Ct);
        Assert.Equal("concrete", slipway!.Surface);
        Assert.Equal(5.5m, slipway.Costs);
        // Facts have no column, so even on our own slipways they are kept as infos.
        Assert.Contains(slipway.Infos, i => i is { Field: "jetty", Value: "yes", Source: Sources.User });

        var status = await _client.GetFromJsonAsync<SuggestionStatusDto>($"/api/suggestions/{created.Id}?token={created.Token}", Ct);
        Assert.Equal(SuggestionStatuses.Accepted, status!.Status);
    }

    [Fact]
    public async Task Accepted_values_for_imported_places_are_kept_apart_and_published_under_the_odbl()
    {
        var marinaId = await AddOsmMarinaAsync(fee: "yes");
        var created = await CreateAsync(Edit(PlaceTypes.Marina, marinaId, "fee", "no"));

        Assert.Equal(HttpStatusCode.NoContent, (await AcceptAsync(created.Id)).StatusCode);

        var marina = await _client.GetFromJsonAsync<MarinaDetailsDto>($"/api/marinas/{marinaId}", Ct);
        Assert.Equal("yes", marina!.Fee);
        Assert.Contains(marina.Infos, i => i is { Field: "fee", Value: "no", Source: Sources.User });
        var info = await WithDbAsync(db => db.PlaceInfos.SingleAsync(i => i.PlaceId == marinaId && i.Field == "fee"));
        Assert.Equal("yes", info.BaseValue);
        Assert.Equal(("way", 77001L), (info.OsmType, info.OsmId!.Value));

        var odbl = await _client.GetFromJsonAsync<OdblContributionsDto>("/api/contributions/odbl", Ct);
        Assert.Contains(odbl!.Values, v => v is { OsmElement: "way/77001", Field: "fee", Value: "no" });
        Assert.DoesNotContain(odbl.Values, v => v.Field == "jetty");
    }

    [Fact]
    public async Task Accepting_a_new_slipway_makes_it_one_of_our_own()
    {
        var created = await CreateAsync(new SuggestionRequestDto(SuggestionKinds.NewSlipway, null, null, "Slip Teststrand",
            53.5, 9.9, new() { ["fee"] = "no", ["toilet"] = "yes" }, "Selbst dort gewesen", true, null, null));

        Assert.Equal(HttpStatusCode.NoContent, (await AcceptAsync(created.Id)).StatusCode);

        var slipway = await WithDbAsync(db => db.Slipways.SingleAsync(s => s.Name == "Slip Teststrand"));
        Assert.Equal(Sources.Manual, slipway.Source);
        Assert.Equal("no", slipway.Fee);
        Assert.True(await WithDbAsync(db => db.PlaceInfos.AnyAsync(i => i.PlaceId == slipway.Id && i.Field == "toilet")));
    }

    [Fact]
    public async Task Rejected_tips_are_never_shown()
    {
        var created = await CreateAsync(Tip(KelheimId));

        Assert.Equal(HttpStatusCode.NoContent, (await _moderation.PostAsync($"/api/moderation/suggestions/{created.Id}/reject", null, Ct)).StatusCode);

        var slipway = await _client.GetFromJsonAsync<SlipwayDetailsDto>($"/api/slipways/{KelheimId}", Ct);
        Assert.DoesNotContain(slipway!.Infos, i => i.Field == PlaceInfoFields.Tip);
        var rejected = await _moderation.GetFromJsonAsync<List<ModerationSuggestionDto>>("/api/moderation/suggestions?status=rejected", Ct);
        Assert.Contains(rejected!, s => s.Id == created.Id);
    }

    [Fact]
    public async Task After_an_import_values_osm_caught_up_with_are_dropped_and_changed_ones_go_back_to_moderation()
    {
        var caughtUp = await AddOsmMarinaAsync(fee: "yes", osmId: 77002);
        var changed = await AddOsmMarinaAsync(fee: "yes", osmId: 77003);
        var unchanged = await AddOsmMarinaAsync(fee: "yes", osmId: 77004);
        foreach (var id in new[] { caughtUp, changed, unchanged })
            await AcceptAsync((await CreateAsync(Edit(PlaceTypes.Marina, id, "fee", "no"))).Id);

        var (resolved, conflicts) = await WithDbAsync(async db =>
        {
            var marinas = await db.Marinas.ToListAsync(Ct);
            marinas.Single(m => m.Id == caughtUp).Fee = "no";
            marinas.Single(m => m.Id == changed).Fee = "customers";
            var result = SuggestionService.ReconcileAfterImport(db, [], marinas,
                await db.PlaceInfos.Where(i => i.Source == Sources.User).ToListAsync(Ct), DateTime.UtcNow);
            await db.SaveChangesAsync(Ct);
            return result;
        });

        Assert.Equal((1, 1), (resolved, conflicts));
        var left = await WithDbAsync(db => db.PlaceInfos.Where(i => i.Source == Sources.User).Select(i => i.PlaceId).ToListAsync(Ct));
        Assert.Equal([unchanged], left);
        var back = await _moderation.GetFromJsonAsync<List<ModerationSuggestionDto>>("/api/moderation/suggestions", Ct);
        var conflict = Assert.Single(back!, s => s.PlaceId == changed);
        Assert.True(conflict.FromServer);
        Assert.Contains("customers", conflict.Text);
    }

    private static SuggestionRequestDto Tip(Guid slipwayId) =>
        new(SuggestionKinds.Tip, PlaceTypes.Slipway, slipwayId, null, null, null, null, "Parken nur am Wochenende schwierig.", false, null, null);

    private static SuggestionRequestDto Edit(string placeType, Guid id, string field, string value) =>
        new(SuggestionKinds.Edit, placeType, id, null, null, null, new() { [field] = value }, null, false, null, null);

    private async Task<ProofSolutionDto> SolveAsync()
    {
        var challenge = await _client.GetFromJsonAsync<ProofChallengeDto>("/api/suggestions/challenge", Ct);
        for (var number = 0; number <= challenge!.MaxNumber; number++)
        {
            if (ProofOfWork.Hash(challenge.Salt, number) == challenge.Challenge)
                return new ProofSolutionDto(challenge.Algorithm, challenge.Challenge, number, challenge.Salt, challenge.Signature);
        }
        throw new InvalidOperationException("Challenge has no solution.");
    }

    private async Task<HttpResponseMessage> SubmitAsync(SuggestionRequestDto request) =>
        await _client.PostAsJsonAsync("/api/suggestions", request with { Proof = request.Proof ?? await SolveAsync() }, Ct);

    private async Task<SuggestionCreatedDto> CreateAsync(SuggestionRequestDto request)
    {
        var response = await SubmitAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SuggestionCreatedDto>(Ct))!;
    }

    private Task<HttpResponseMessage> AcceptAsync(Guid id) =>
        _moderation.PostAsync($"/api/moderation/suggestions/{id}/accept", null, Ct);

    private Task<int> CountAsync() => WithDbAsync(db => db.Suggestions.CountAsync(Ct));

    private async Task<Guid> AddOsmMarinaAsync(string fee, long osmId = 77001) =>
        await WithDbAsync(async db =>
        {
            var marina = new Marina
            {
                Id = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Name = $"Testhafen {osmId}",
                Latitude = 53.0,
                Longitude = 8.0,
                Fee = fee,
                Source = Sources.Osm,
                OsmType = "way",
                OsmId = osmId,
            };
            db.Marinas.Add(marina);
            await db.SaveChangesAsync(Ct);
            return marina.Id;
        });

    private async Task<T> WithDbAsync<T>(Func<SlipwaysDbContext, Task<T>> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>());
    }
}
