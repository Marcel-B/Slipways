using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Slipways.Infrastructure.Websites;

/// <summary>What a website states about the place itself; null where it states nothing or something ambiguous.</summary>
public sealed record WebsiteDetails(string? Phone, string? Email, string? OpeningHours)
{
    public bool IsEmpty => Phone is null && Email is null && OpeningHours is null;
}

/// <summary>
/// Reads contact details and opening hours from a home page: first from schema.org data (JSON-LD), then phone and email
/// from <c>tel:</c> and <c>mailto:</c> links. A value is only taken when the page names exactly one, so a page that
/// lists several contacts, such as a club's board, yields none. Data about persons is never read.
/// </summary>
public static partial class WebsiteParser
{
    private const int MaxValueLength = 500;

    // Properties that describe other people or things than the place itself.
    private static readonly HashSet<string> SkippedProperties =
        ["author", "creator", "founder", "founders", "employee", "employees", "member", "members", "review", "reviews",
         "contributor", "editor", "funder", "sponsor", "alumni", "parentOrganization", "subOrganization"];

    private static readonly string[] DayCodes = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];

    public static WebsiteDetails Parse(string html)
    {
        var data = new Found();
        foreach (Match script in JsonLdScript().Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(script.Groups[1].Value,
                    new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                Visit(document.RootElement, data);
            }
            catch (JsonException)
            {
                // Broken structured data is common; the links below may still help.
            }
        }

        var phones = data.Phones.Count > 0 ? data.Phones : Links(html, "tel").ToList();
        var emails = data.Emails.Count > 0 ? data.Emails : Links(html, "mailto").ToList();

        return new WebsiteDetails(
            Single(phones.Select(NormalizePhone), SamePhone),
            Single(emails.Select(NormalizeEmail), (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)),
            Single(data.OpeningHours.Where(h => h.Length <= MaxValueLength), (a, b) => a == b));
    }

    private static void Visit(JsonElement element, Found data)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                Visit(item, data);
            return;
        }
        if (element.ValueKind != JsonValueKind.Object)
            return;

        var types = Strings(element, "@type").ToList();
        if (types.Any(t => t.EndsWith("Person", StringComparison.OrdinalIgnoreCase)))
            return;

        // Pages and sites describe the document, not the place; their publisher is visited below.
        if (!types.Any(t => t is "WebSite" or "WebPage" or "BreadcrumbList" or "ImageObject"))
        {
            data.Phones.AddRange(Strings(element, "telephone"));
            data.Emails.AddRange(Strings(element, "email"));
            if (OpeningHours(element) is { } hours)
                data.OpeningHours.Add(hours);
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!SkippedProperties.Contains(property.Name) && property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                Visit(property.Value, data);
        }
    }

    private static string? OpeningHours(JsonElement element)
    {
        var text = Strings(element, "openingHours").Select(h => h.Trim()).Where(h => h.Length > 0).ToList();
        if (text.Count > 0)
            return string.Join("; ", text);

        if (!element.TryGetProperty("openingHoursSpecification", out var specification))
            return null;

        // Groups days with the same hours, in the syntax OpenStreetMap uses too: "Mo-Fr 08:00-18:00; Sa 09:00-12:00".
        var specifications = specification.ValueKind == JsonValueKind.Array ? specification.EnumerateArray().ToList() : [specification];
        var groups = new List<(string Hours, SortedSet<int> Days)>();
        foreach (var spec in specifications)
        {
            // Seasonal hours would need dates to make sense, which free text cannot carry well.
            if (spec.ValueKind != JsonValueKind.Object || spec.TryGetProperty("validFrom", out _) || spec.TryGetProperty("validThrough", out _))
                return null;
            if (Strings(spec, "opens").FirstOrDefault() is not { } opens || Strings(spec, "closes").FirstOrDefault() is not { } closes)
                continue;

            var hours = $"{Time(opens)}-{Time(closes)}";
            var days = Strings(spec, "dayOfWeek").Select(Day).Where(d => d >= 0).ToList();
            if (days.Count == 0 || hours.Length > 11)
                continue;

            var group = groups.FindIndex(g => g.Hours == hours);
            if (group < 0)
                groups.Add((hours, [.. days]));
            else
                groups[group].Days.UnionWith(days);
        }

        return groups.Count == 0
            ? null
            : string.Join("; ", groups.OrderBy(g => g.Days.Min).Select(g => $"{DayRanges(g.Days)} {g.Hours}"));
    }

    private static string Time(string value) => value.Length >= 5 ? value[..5] : value;

    /// <summary>"https://schema.org/Monday", "Monday" or "Mo" as 0 to 6; -1 for anything else.</summary>
    private static int Day(string value)
    {
        var name = value[(value.LastIndexOf('/') + 1)..];
        return name.Length >= 2 ? Array.FindIndex(DayCodes, c => name.StartsWith(c, StringComparison.OrdinalIgnoreCase)) : -1;
    }

    private static string DayRanges(SortedSet<int> days)
    {
        var ranges = new List<string>();
        var list = days.ToList();
        for (var i = 0; i < list.Count;)
        {
            var end = i;
            while (end + 1 < list.Count && list[end + 1] == list[end] + 1)
                end++;
            ranges.Add(end == i ? DayCodes[list[i]] : $"{DayCodes[list[i]]}{(end == i + 1 ? "," : "-")}{DayCodes[list[end]]}");
            i = end + 1;
        }
        return string.Join(",", ranges);
    }

    /// <summary>A string property, or each string in an array property.</summary>
    private static IEnumerable<string> Strings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            yield break;
        if (value.ValueKind == JsonValueKind.String)
            yield return value.GetString()!;
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.String))
                yield return item.GetString()!;
    }

    private static IEnumerable<string> Links(string html, string scheme) =>
        LinkHref().Matches(html)
            .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim())
            .Where(href => href.StartsWith(scheme + ":", StringComparison.OrdinalIgnoreCase))
            .Select(href => href[(scheme.Length + 1)..]);

    private static string? NormalizePhone(string value)
    {
        var phone = Unescape(value).Trim();
        var digits = phone.Count(char.IsAsciiDigit);
        return PhoneCharacters().IsMatch(phone) && digits is >= 6 and <= 20 ? WhiteSpace().Replace(phone, " ") : null;
    }

    private static string? NormalizeEmail(string value)
    {
        var email = Unescape(value.Split('?', 2)[0]).Trim();
        if (email.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            email = email[7..];
        return email.Length <= 254 && EmailAddress().IsMatch(email) ? email : null;
    }

    private static string Unescape(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            return value;
        }
    }

    /// <summary>
    /// Whether two numbers are the same, written nationally or internationally: "+49 30 1234567" and "030 1234567" both
    /// end in "301234567" once the country code or trunk prefix is dropped.
    /// </summary>
    private static bool SamePhone(string a, string b)
    {
        var (x, y) = (Digits(a), Digits(b));
        var (shorter, longer) = x.Length <= y.Length ? (x, y) : (y, x);
        return shorter.Length >= 6 && longer.EndsWith(shorter, StringComparison.Ordinal);

        static string Digits(string phone) => new string(phone.Where(char.IsAsciiDigit).ToArray()).TrimStart('0');
    }

    /// <summary>The value when all valid ones mean the same thing; the first one with a country code is preferred.</summary>
    private static string? Single(IEnumerable<string?> values, Func<string, string, bool> same)
    {
        var valid = values.OfType<string>().ToList();
        if (valid.Count == 0 || valid.Any(v => !same(v, valid[0])))
            return null;
        return valid.FirstOrDefault(v => v.StartsWith('+')) ?? valid[0];
    }

    private sealed class Found
    {
        public List<string> Phones { get; } = [];
        public List<string> Emails { get; } = [];
        public List<string> OpeningHours { get; } = [];
    }

    [GeneratedRegex("""<script[^>]*type\s*=\s*["']?application/ld\+json["']?[^>]*>(.*?)</script>""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScript();

    [GeneratedRegex("""href\s*=\s*["']([^"']*)["']""", RegexOptions.IgnoreCase)]
    private static partial Regex LinkHref();

    [GeneratedRegex(@"^\+?[\d\s()./-]+$")]
    private static partial Regex PhoneCharacters();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();

    [GeneratedRegex("""^[^@\s<>"',;:]+@[^@\s<>"',;:]+\.[a-zA-Z]{2,}$""")]
    private static partial Regex EmailAddress();
}
