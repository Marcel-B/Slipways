using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Endpoints;

public sealed class SeoOptions
{
    public const string SectionName = "Seo";

    /// <summary>Public address of the web app, used for canonical links, Open Graph and the sitemap.</summary>
    public string SiteUrl { get; set; } = "https://slipways.de";
}

/// <summary>
/// What search engines and link previews need from the single page web app: nginx includes <see cref="Head"/> into
/// index.html per request (server side includes), so every page has its own title, description, canonical link and
/// structured data before any JavaScript runs. <see cref="Sitemap"/> lists all pages worth indexing.
/// </summary>
public static class SeoEndpoints
{
    private const string SiteName = "Slipways";

    private const string DefaultDescription =
        "Slipanlagen, Marinas und Pegelstände in Deutschland, Österreich, der Schweiz und den Niederlanden auf einer " +
        "Karte: Lage, Kosten, Zugang, Untergrund und aktueller Wasserstand.";

    private static readonly Dictionary<string, SeoPage> StaticPages = new()
    {
        [""] = new($"{SiteName} · Slipanlagen, Marinas und Pegel", DefaultDescription, ""),
        ["waters"] = new($"Gewässer · {SiteName}",
            "Flüsse, Seen und Kanäle mit ihren Slipanlagen, Marinas und aktuellen Pegelständen.", "waters"),
        ["marinas"] = new($"Marinas · {SiteName}",
            "Marinas und Sportboothäfen auf der Karte, mit Kontakt, Öffnungszeiten und Slipanlagen in der Nähe.",
            "marinas"),
        ["impressum"] = new($"Impressum · {SiteName}", "Impressum von slipways.de.", "impressum"),
        ["datenschutz"] = new($"Datenschutz · {SiteName}",
            "Welche Daten slipways.de und die Slipways-Apps verarbeiten.", "datenschutz"),
        ["support"] = new($"Support · {SiteName}", "Hilfe und Kontakt zu Slipways.", "support"),
        // A form, nothing to find in a search.
        ["slipways/neu"] = new($"Slipanlage vorschlagen · {SiteName}", "Eine fehlende Slipanlage ohne Konto vorschlagen.", null),
    };

    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        var seo = app.MapGroup("/api/seo").RequireRateLimiting(RateLimiting.ApiPolicy).ExcludeFromDescription();

        // HEAD as well: crawlers check the sitemap with it, and nginx makes the include with the method of the page request.
        seo.MapMethods("/head/{**path}", [HttpMethods.Get, HttpMethods.Head], Head);
        seo.MapMethods("/sitemap", [HttpMethods.Get, HttpMethods.Head], Sitemap);

        return app;
    }

    /// <summary>The page path without slashes and query, e.g. "slipways/0b6…" for "/slipways/0b6…?x=1".</summary>
    private static string NormalizePath(string? path)
    {
        var value = path ?? string.Empty;
        var query = value.IndexOfAny(['?', '#']);
        if (query >= 0)
            value = value[..query];
        return value.Trim('/');
    }

    private static async Task<IResult> Head(
        SlipwaysDbContext db, IOptions<SeoOptions> options, string? path, CancellationToken ct)
    {
        var page = await FindPage(db, NormalizePath(path), ct);
        return Results.Content(Render(page, options.Value.SiteUrl.TrimEnd('/')), "text/html; charset=utf-8");
    }

    private static async Task<SeoPage> FindPage(SlipwaysDbContext db, string path, CancellationToken ct)
    {
        if (StaticPages.TryGetValue(path, out var known))
            return known;

        var segments = path.Split('/');
        if (segments.Length == 2 && Guid.TryParse(segments[1], out var id))
        {
            var page = segments[0] switch
            {
                "slipways" => await SlipwayPage(db, id, ct),
                "marinas" => await MarinaPage(db, id, ct),
                "waters" => await WaterPage(db, id, ct),
                _ => null,
            };
            if (page is not null)
                return page;
        }

        // Unknown pages are answered with 200 by the single page app, so at least keep them out of the index.
        return new SeoPage($"Seite nicht gefunden · {SiteName}", DefaultDescription, null);
    }

    private static async Task<SeoPage?> SlipwayPage(SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var slipway = await db.Slipways.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Name, s.Latitude, s.Longitude, s.Street, s.PostalCode, s.City, s.Country,
                Water = s.Water == null ? null : s.Water.Name,
            })
            .SingleOrDefaultAsync(ct);
        if (slipway is null)
            return null;

        var title = PlaceTitle(slipway.Name, slipway.City);
        var description = new StringBuilder("Slipanlage");
        if (Join(" ", slipway.PostalCode, slipway.City) is { } place)
            description.Append(CultureInfo.InvariantCulture, $" in {place}");
        if (slipway.Water is not null)
            description.Append(CultureInfo.InvariantCulture, $", Gewässer {slipway.Water}");
        description.Append(": Lage, Kosten, Zugang, Untergrund und der nächste Pegel.");

        var data = Place("Place", title, $"slipways/{id}", slipway.Latitude, slipway.Longitude);
        if (Address(slipway.Street, slipway.PostalCode, slipway.City, slipway.Country) is { } address)
            data["address"] = address;
        return new SeoPage($"{title} · {SiteName}", description.ToString(), $"slipways/{id}", data);
    }

    private static async Task<SeoPage?> MarinaPage(SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var marina = await db.Marinas.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new
            {
                m.Name, m.Latitude, m.Longitude, m.Street, m.PostalCode, m.City, m.Phone, m.Url,
                Water = m.Water == null ? null : m.Water.Name,
            })
            .SingleOrDefaultAsync(ct);
        if (marina is null)
            return null;

        var title = PlaceTitle(marina.Name, marina.City);
        var description = new StringBuilder("Marina");
        if (Join(" ", marina.PostalCode, marina.City) is { } place)
            description.Append(CultureInfo.InvariantCulture, $" in {place}");
        if (marina.Water is not null)
            description.Append(CultureInfo.InvariantCulture, $", Gewässer {marina.Water}");
        description.Append(": Lage, Kontakt, Öffnungszeiten und Slipanlagen in der Nähe.");

        var data = Place("Marina", title, $"marinas/{id}", marina.Latitude, marina.Longitude);
        if (Address(marina.Street, marina.PostalCode, marina.City, null) is { } address)
            data["address"] = address;
        if (marina.Phone is not null)
            data["telephone"] = marina.Phone;
        if (Uri.TryCreate(marina.Url, UriKind.Absolute, out var url) && url.Scheme is "http" or "https")
            data["sameAs"] = url.ToString();
        return new SeoPage($"{title} · {SiteName}", description.ToString(), $"marinas/{id}", data);
    }

    private static async Task<SeoPage?> WaterPage(SlipwaysDbContext db, Guid id, CancellationToken ct)
    {
        var water = await db.Waters.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new
            {
                w.Name,
                Slipways = db.Slipways.Count(s => s.WaterId == w.Id),
                Marinas = db.Marinas.Count(m => m.WaterId == w.Id),
            })
            .SingleOrDefaultAsync(ct);
        if (water is null)
            return null;

        var description =
            $"{water.Name}: {Count(water.Slipways, "Slipanlage", "Slipanlagen")} und " +
            $"{Count(water.Marinas, "Marina", "Marinas")} auf der Karte, dazu die aktuellen Pegelstände.";
        var data = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BodyOfWater",
            ["name"] = water.Name,
        };
        return new SeoPage($"{water.Name} · {SiteName}", description, $"waters/{id}", data);
    }

    private static async Task<IResult> Sitemap(SlipwaysDbContext db, IOptions<SeoOptions> options, CancellationToken ct)
    {
        var site = options.Value.SiteUrl.TrimEnd('/');
        var slipways = await db.Slipways.AsNoTracking().OrderBy(s => s.Id)
            .Select(s => new { s.Id, Modified = s.Updated ?? s.Created }).ToListAsync(ct);
        var marinas = await db.Marinas.AsNoTracking().OrderBy(m => m.Id)
            .Select(m => new { m.Id, Modified = m.Updated ?? m.Created }).ToListAsync(ct);
        var waters = await db.Waters.AsNoTracking().OrderBy(w => w.Id)
            .Select(w => new { w.Id, Modified = w.Updated ?? w.Created }).ToListAsync(ct);

        using var stream = new MemoryStream();
        await using (var xml = XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Encoding = new UTF8Encoding(false) }))
        {
            const string ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            await xml.WriteStartDocumentAsync();
            xml.WriteStartElement("urlset", ns);

            foreach (var path in new[] { "", "marinas", "waters" })
                WriteUrl(xml, ns, $"{site}/{path}", null);
            foreach (var slipway in slipways)
                WriteUrl(xml, ns, $"{site}/slipways/{slipway.Id}", slipway.Modified);
            foreach (var marina in marinas)
                WriteUrl(xml, ns, $"{site}/marinas/{marina.Id}", marina.Modified);
            foreach (var water in waters)
                WriteUrl(xml, ns, $"{site}/waters/{water.Id}", water.Modified);

            await xml.WriteEndElementAsync();
            await xml.WriteEndDocumentAsync();
        }

        return Results.Bytes(stream.ToArray(), "application/xml; charset=utf-8");
    }

    private static void WriteUrl(XmlWriter xml, string ns, string location, DateTime? modified)
    {
        xml.WriteStartElement("url", ns);
        xml.WriteElementString("loc", ns, location);
        if (modified is { } value && value > DateTime.MinValue)
            xml.WriteElementString("lastmod", ns, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        xml.WriteEndElement();
    }

    /// <summary>
    /// Many imported places are only called "Slipanlage" or "Marina", so the town makes the title tell them apart.
    /// The web app builds the same title in <c>web/src/pages/pageTitle.ts</c>.
    /// </summary>
    public static string PlaceTitle(string name, string? city) =>
        city is { Length: > 0 } && !name.Contains(city, StringComparison.OrdinalIgnoreCase) ? $"{name} in {city}" : name;

    private static string Count(int count, string one, string many) =>
        count == 1 ? $"1 {one}" : $"{count.ToString(CultureInfo.InvariantCulture)} {many}";

    private static string? Join(string separator, params string?[] parts)
    {
        var present = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return present.Count == 0 ? null : string.Join(separator, present);
    }

    private static JsonObject Place(string type, string name, string path, double latitude, double longitude) => new()
    {
        ["@context"] = "https://schema.org",
        ["@type"] = type,
        ["name"] = name,
        ["geo"] = new JsonObject
        {
            ["@type"] = "GeoCoordinates",
            ["latitude"] = latitude,
            ["longitude"] = longitude,
        },
        ["hasMap"] = $"https://www.openstreetmap.org/?mlat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                     $"&mlon={longitude.ToString(CultureInfo.InvariantCulture)}#map=16/" +
                     $"{latitude.ToString(CultureInfo.InvariantCulture)}/{longitude.ToString(CultureInfo.InvariantCulture)}",
    };

    private static JsonObject? Address(string? street, string? postalCode, string? city, string? country)
    {
        if (street is null && postalCode is null && city is null)
            return null;
        var address = new JsonObject { ["@type"] = "PostalAddress" };
        if (street is not null)
            address["streetAddress"] = street;
        if (postalCode is not null)
            address["postalCode"] = postalCode;
        if (city is not null)
            address["addressLocality"] = city;
        if (country is not null)
            address["addressCountry"] = country;
        return address;
    }

    private static string Render(SeoPage page, string site)
    {
        var html = new StringBuilder();
        html.Append(CultureInfo.InvariantCulture, $"<title>{Encode(page.Title)}</title>\n");
        html.Append(CultureInfo.InvariantCulture, $"    <meta name=\"description\" content=\"{Encode(page.Description)}\" />\n");
        if (page.Path is null)
        {
            html.Append("    <meta name=\"robots\" content=\"noindex\" />\n");
            return html.ToString();
        }

        var url = $"{site}/{page.Path}";
        html.Append(CultureInfo.InvariantCulture, $"    <link rel=\"canonical\" href=\"{Encode(url)}\" />\n");
        html.Append(CultureInfo.InvariantCulture, $"    <meta property=\"og:type\" content=\"website\" />\n");
        html.Append(CultureInfo.InvariantCulture, $"    <meta property=\"og:site_name\" content=\"{SiteName}\" />\n");
        html.Append("    <meta property=\"og:locale\" content=\"de_DE\" />\n");
        html.Append(CultureInfo.InvariantCulture, $"    <meta property=\"og:title\" content=\"{Encode(page.Title)}\" />\n");
        html.Append(CultureInfo.InvariantCulture, $"    <meta property=\"og:description\" content=\"{Encode(page.Description)}\" />\n");
        html.Append(CultureInfo.InvariantCulture, $"    <meta property=\"og:url\" content=\"{Encode(url)}\" />\n");

        var data = page.Data ?? new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = SiteName,
            ["url"] = $"{site}/",
            ["inLanguage"] = "de",
        };
        if (page.Data is not null)
            data["url"] = url;
        // The default encoder escapes <, > and &, so the data can never close the script element.
        html.Append(CultureInfo.InvariantCulture,
            $"    <script type=\"application/ld+json\">{data.ToJsonString(JsonOptions)}</script>\n");
        return html.ToString();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    // WebUtility.HtmlEncode would also turn every umlaut into an entity.
    private static string Encode(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("'", "&#39;", StringComparison.Ordinal);

    /// <param name="Path">The canonical path; null for pages that should not be indexed.</param>
    private sealed record SeoPage(string Title, string Description, string? Path, JsonObject? Data = null);
}
