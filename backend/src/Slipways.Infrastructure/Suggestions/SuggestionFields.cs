using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Slipways.Domain;

namespace Slipways.Infrastructure.Suggestions;

/// <param name="Name">The field name in suggestions and in <see cref="PlaceInfo.Field"/>; OpenStreetMap's tag name where there is one.</param>
/// <param name="PlaceTypes">The <see cref="Domain.PlaceTypes"/> the field exists for.</param>
/// <param name="Normalize">The value as it is stored, or null when it is not valid.</param>
/// <param name="Read">The place's own column for the field, if it has one.</param>
/// <param name="Write">Sets that column; used for our own places only.</param>
public sealed record SuggestionField(
    string Name,
    string[] PlaceTypes,
    Func<string, string?> Normalize,
    Func<IOsmPlace, string?>? Read = null,
    Action<IOsmPlace, string>? Write = null)
{
    /// <summary>The OpenStreetMap import writes this column of imported places, so a later import may catch up with a user's value.</summary>
    public bool FromOsm => Read is not null && Name != SuggestionFields.Costs;
}

/// <summary>The fields users may suggest values for, and how their values are checked.</summary>
public static partial class SuggestionFields
{
    public const string Costs = "costs";

    public const int MaxNameLength = 120;
    public const int MaxTextLength = 500;
    public const int MaxTipLength = 200;

    private static readonly string[] Both = [PlaceTypes.Slipway, PlaceTypes.Marina];
    private static readonly string[] SlipwayOnly = [PlaceTypes.Slipway];
    private static readonly string[] MarinaOnly = [PlaceTypes.Marina];

    // The values the web and the apps offer; the same as the OpenStreetMap tags.
    public static readonly string[] AccessValues = ["yes", "permissive", "customers", "members", "private", "no"];
    public static readonly string[] SurfaceValues = ["concrete", "asphalt", "paving_stones", "gravel", "wood", "sand", "grass", "unpaved"];

    /// <summary>Facts about a slipway to tick, each "yes" or "no".</summary>
    public static readonly string[] Facts =
        ["trailer_parking", "jetty", "toilet", "crane", "steep", "slippery_at_low_water", "key_required"];

    public static readonly IReadOnlyDictionary<string, SuggestionField> All = new SuggestionField[]
        {
            new("fee", Both, YesNo, p => Column(p, s => s.Fee, m => m.Fee), (p, v) => Set(p, v, (s, x) => s.Fee = x, (m, x) => m.Fee = x)),
            new(Costs, SlipwayOnly, Amount,
                p => ((Slipway)p).Costs?.ToString("0.##", CultureInfo.InvariantCulture),
                (p, v) => ((Slipway)p).Costs = decimal.Parse(v, CultureInfo.InvariantCulture)),
            new("access", Both, OneOf(AccessValues), p => Column(p, s => s.Access, m => m.Access),
                (p, v) => Set(p, v, (s, x) => s.Access = x, (m, x) => m.Access = x)),
            new(PlaceInfoFields.OpeningHours, Both, PlainText(100), p => Column(p, s => s.OpeningHours, m => m.OpeningHours),
                (p, v) => Set(p, v, (s, x) => s.OpeningHours = x, (m, x) => m.OpeningHours = x)),
            new("surface", SlipwayOnly, OneOf(SurfaceValues), p => ((Slipway)p).Surface, (p, v) => ((Slipway)p).Surface = v),
            new("operator", Both, PlainText(100), p => Column(p, s => s.Operator, m => m.Operator),
                (p, v) => Set(p, v, (s, x) => s.Operator = x, (m, x) => m.Operator = x)),
            new(PlaceInfoFields.Phone, MarinaOnly, Phone, p => ((Marina)p).Phone, (p, v) => ((Marina)p).Phone = v),
            new(PlaceInfoFields.Email, MarinaOnly, Email, p => ((Marina)p).Email, (p, v) => ((Marina)p).Email = v),
        }
        .Concat(Facts.Select(f => new SuggestionField(f, SlipwayOnly, YesNo)))
        .ToDictionary(f => f.Name);

    /// <summary>Checks and normalizes suggested values; the error names the first field that is not valid.</summary>
    public static (Dictionary<string, string> Values, string? Error) Validate(string placeType, IReadOnlyDictionary<string, string>? fields)
    {
        var values = new Dictionary<string, string>();
        foreach (var (name, raw) in fields ?? new Dictionary<string, string>())
        {
            if (!All.TryGetValue(name, out var field) || !field.PlaceTypes.Contains(placeType))
                return (values, $"Das Feld „{name}“ gibt es hier nicht.");
            if (field.Normalize(raw ?? "") is not { } value)
                return (values, $"Der Wert für „{name}“ ist nicht gültig.");
            values[name] = value;
        }
        return (values, null);
    }

    /// <summary>Trimmed text with single spaces, or null when it is empty, too long or contains links or control characters.</summary>
    public static string? CleanText(string? text, int maxLength, bool multiline = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var lines = text.Replace("\r\n", "\n").Split('\n')
            .Select(l => Spaces().Replace(l, " ").Trim())
            .Where(l => l.Length > 0)
            .ToList();
        if (!multiline && lines.Count > 1)
            return null;
        var clean = string.Join('\n', lines);
        if (clean.Length > maxLength || clean.Any(c => char.IsControl(c) && c != '\n') || Link().IsMatch(clean))
            return null;
        return clean;
    }

    private static string? YesNo(string value) => value is "yes" or "no" ? value : null;

    private static string? Amount(string value) =>
        decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
        && amount is >= 0 and <= 1000
            ? amount.ToString("0.##", CultureInfo.InvariantCulture)
            : null;

    private static Func<string, string?> OneOf(string[] allowed) => value => allowed.Contains(value) ? value : null;

    private static Func<string, string?> PlainText(int maxLength) => value => CleanText(value, maxLength);

    private static string? Phone(string value)
    {
        var phone = Spaces().Replace(value.Trim(), " ");
        return PhonePattern().IsMatch(phone) && phone.Count(char.IsAsciiDigit) >= 5 ? phone : null;
    }

    private static string? Email(string value)
    {
        var email = value.Trim();
        return email.Length <= 100 && MailAddress.TryCreate(email, out var address) && address.Address == email
               && address.Host.Contains('.')
            ? email
            : null;
    }

    private static string? Column(IOsmPlace place, Func<Slipway, string?> slipway, Func<Marina, string?> marina) =>
        place switch
        {
            Slipway s => slipway(s),
            Marina m => marina(m),
            _ => null,
        };

    private static void Set(IOsmPlace place, string value, Action<Slipway, string> slipway, Action<Marina, string> marina)
    {
        switch (place)
        {
            case Slipway s: slipway(s, value); break;
            case Marina m: marina(m, value); break;
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"https?://|www\.|\b[a-z0-9-]+\.(de|com|net|org|at|ch|nl|eu|info|io)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Link();

    [GeneratedRegex(@"^\+?[0-9 ()/\-]{5,30}$")]
    private static partial Regex PhonePattern();
}
