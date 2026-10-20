using System.Net;
using System.Net.Http.Json;
using Slipways.Api.Endpoints;

namespace Slipways.Api.Tests;

public class CatalogEndpointsTests(SlipwaysApiFactory factory) : IClassFixture<SlipwaysApiFactory>
{
    private static readonly Guid KelheimId = Guid.Parse("EAC96A93-FD47-4A9A-8771-0005805B9655");

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Lists_all_seeded_slipways()
    {
        var slipways = await _client.GetFromJsonAsync<List<SlipwaySummaryDto>>("/api/slipways", TestContext.Current.CancellationToken);

        Assert.NotNull(slipways);
        Assert.Equal(106, slipways.Count);
        Assert.Equal(slipways.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => s.Id), slipways.Select(s => s.Id));
    }

    [Fact]
    public async Task Filters_slipways_by_search_term()
    {
        var slipways = await _client.GetFromJsonAsync<List<SlipwaySummaryDto>>("/api/slipways?search=kelheim", TestContext.Current.CancellationToken);

        Assert.NotNull(slipways);
        Assert.Contains(slipways, s => s.Id == KelheimId);
        Assert.All(slipways, s => Assert.Contains("kelheim", $"{s.Name} {s.City} {s.Water.Name}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Returns_slipway_details()
    {
        var slipway = await _client.GetFromJsonAsync<SlipwayDetailsDto>($"/api/slipways/{KelheimId}", TestContext.Current.CancellationToken);

        Assert.NotNull(slipway);
        Assert.Equal("Am Jachthafen, Kelheim", slipway.Name);
        Assert.Equal("93309", slipway.PostalCode);
        Assert.Null(slipway.Costs);
        Assert.NotNull(slipway.Marina);
    }

    [Fact]
    public async Task Returns_404_for_unknown_slipway()
    {
        var response = await _client.GetAsync($"/api/slipways/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Lists_waters_with_readable_names()
    {
        var waters = await _client.GetFromJsonAsync<List<WaterDto>>("/api/waters", TestContext.Current.CancellationToken);

        Assert.NotNull(waters);
        Assert.Equal(109, waters.Count);
        Assert.Contains(waters, w => w.Name == "Oste");
    }

    [Theory]
    [InlineData("/api/marinas")]
    [InlineData("/api/extras")]
    [InlineData("/api/services")]
    [InlineData("/api/stations")]
    [InlineData("/health")]
    public async Task Other_endpoints_respond(string url)
    {
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
