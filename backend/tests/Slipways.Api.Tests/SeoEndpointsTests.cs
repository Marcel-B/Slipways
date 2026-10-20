using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Slipways.Api.Endpoints;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public partial class SeoEndpointsTests(SlipwaysApiFactory factory) : IClassFixture<SlipwaysApiFactory>
{
    private static readonly Guid KelheimId = Guid.Parse("EAC96A93-FD47-4A9A-8771-0005805B9655");
    private static readonly Guid DonauId = Guid.Parse("D19708CA-AC42-4AD4-8D62-E85722F946F6");

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Describes_a_slipway_with_canonical_link_and_structured_data()
    {
        var head = await Head($"/slipways/{KelheimId}?from=map");

        Assert.Contains("<title>Am Jachthafen, Kelheim · Slipways</title>", head);
        Assert.Contains($"<link rel=\"canonical\" href=\"https://slipways.de/slipways/{KelheimId}\" />", head);
        Assert.Contains("93309 Kelheim", head);
        Assert.DoesNotContain("noindex", head);

        var data = StructuredData(head);
        Assert.Equal("Place", (string?)data["@type"]);
        Assert.Equal("93309", (string?)data["address"]?["postalCode"]);
        Assert.NotNull(data["geo"]?["latitude"]);
    }

    [Fact]
    public async Task Describes_a_water_with_what_lies_on_it()
    {
        var head = await Head($"/waters/{DonauId}");

        Assert.Contains("<title>Donau · Slipways</title>", head);
        Assert.Matches(@"Donau: \d+ Slipanlagen", head);
        Assert.Equal("BodyOfWater", (string?)StructuredData(head)["@type"]);
    }

    [Fact]
    public async Task Describes_the_start_page_as_web_site()
    {
        var head = await Head("/");

        Assert.Contains("<link rel=\"canonical\" href=\"https://slipways.de/\" />", head);
        Assert.Equal("WebSite", (string?)StructuredData(head)["@type"]);
    }

    [Theory]
    [InlineData("/slipways/00000000-0000-0000-0000-000000000001")]
    [InlineData("/gibt-es-nicht")]
    [InlineData("/slipways/kein-guid")]
    public async Task Keeps_unknown_pages_out_of_the_index(string path)
    {
        var head = await Head(path);

        Assert.Contains("<meta name=\"robots\" content=\"noindex\" />", head);
        Assert.DoesNotContain("canonical", head);
    }

    [Fact]
    public async Task Lists_every_place_in_the_sitemap()
    {
        var response = await _client.GetAsync("/api/seo/sitemap", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var sitemap = XDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var locations = sitemap.Descendants(ns + "loc").Select(l => l.Value).ToList();

        Assert.Contains("https://slipways.de/", locations);
        Assert.Contains($"https://slipways.de/slipways/{KelheimId}", locations);
        Assert.Contains($"https://slipways.de/waters/{DonauId}", locations);
        Assert.Equal(locations.Count, locations.Distinct().Count());
    }

    [Theory]
    [InlineData("Slipanlage", "Kiel", "Slipanlage in Kiel")]
    [InlineData("Am Jachthafen, Kelheim", "Kelheim", "Am Jachthafen, Kelheim")]
    [InlineData("Marina", null, "Marina")]
    public void Adds_the_town_to_titles_without_it(string name, string? city, string title) =>
        Assert.Equal(title, SeoEndpoints.PlaceTitle(name, city));

    [Fact]
    public async Task Escapes_names_so_they_cannot_break_out_of_the_head()
    {
        var id = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>();
            db.Slipways.Add(new Slipway { Id = id, Name = "Hafen \"Süd\" </script><b>", City = "Köln" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var head = await Head($"/slipways/{id}");

        Assert.Contains("<title>Hafen &quot;Süd&quot; &lt;/script&gt;&lt;b&gt; in Köln · Slipways</title>", head);
        Assert.DoesNotContain("<b>", head);
        Assert.Single(StructuredDataPattern().Matches(head));
        Assert.Equal("Hafen \"Süd\" </script><b> in Köln", (string?)StructuredData(head)["name"]);
    }

    private async Task<string> Head(string path)
    {
        var response = await _client.GetAsync($"/api/seo/head{path}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static JsonNode StructuredData(string head) =>
        JsonNode.Parse(StructuredDataPattern().Match(head).Groups[1].Value)!;

    [GeneratedRegex("<script type=\"application/ld\\+json\">(.*)</script>")]
    private static partial Regex StructuredDataPattern();
}
