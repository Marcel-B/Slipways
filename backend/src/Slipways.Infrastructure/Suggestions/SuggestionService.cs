using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Osm;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Suggestions;

/// <param name="Kind">One of <see cref="SuggestionKinds"/>.</param>
/// <param name="ConfirmNew">The sender saw the places nearby and still means a new one.</param>
public sealed record SuggestionInput(
    string Kind,
    string? PlaceType,
    Guid? PlaceId,
    string? Name,
    double? Latitude,
    double? Longitude,
    IReadOnlyDictionary<string, string>? Fields,
    string? Text,
    bool ConfirmNew);

public sealed record NearbyPlace(Guid Id, string Name, double Distance, string Source);

public abstract record SubmitResult
{
    public sealed record Created(Suggestion Suggestion, string Token) : SubmitResult;

    public sealed record Invalid(string Message) : SubmitResult;

    public sealed record NotFound : SubmitResult;

    /// <summary>There are slipways close to a new one; the sender has to confirm it is not one of them.</summary>
    public sealed record Nearby(IReadOnlyList<NearbyPlace> Places) : SubmitResult;
}

/// <summary>Takes suggestions from users and applies them once the moderator accepts them.</summary>
public sealed class SuggestionService(SlipwaysDbContext db, IOptions<SuggestionOptions> options, TimeProvider time)
{
    /// <summary>Metres around a new slipway within which places we have are shown to the sender first.</summary>
    public const double NearbyDistance = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<SubmitResult> SubmitAsync(SuggestionInput input, CancellationToken cancellationToken = default)
    {
        var suggestion = new Suggestion { Kind = input.Kind };
        switch (input.Kind)
        {
            case SuggestionKinds.NewSlipway:
            {
                if (SuggestionFields.CleanText(input.Name, SuggestionFields.MaxNameLength) is not { } name)
                    return new SubmitResult.Invalid("Bitte einen Namen ohne Links angeben.");
                // The OpenStreetMap import covers countries all over Europe.
                if (input is not { Latitude: { } lat, Longitude: { } lon } || !OsmImport.InEurope(lat, lon))
                    return new SubmitResult.Invalid("Die Position liegt außerhalb von Europa.");
                var (latitude, longitude) = (input.Latitude.Value, input.Longitude.Value);

                if (await PendingNewSlipwayNearAsync(latitude, longitude, cancellationToken))
                    return new SubmitResult.Invalid("Für diese Stelle liegt schon ein Vorschlag vor.");
                if (!input.ConfirmNew && await NearbySlipwaysAsync(latitude, longitude, cancellationToken) is { Count: > 0 } nearby)
                    return new SubmitResult.Nearby(nearby);

                (suggestion.Name, suggestion.Latitude, suggestion.Longitude) = (name, latitude, longitude);
                if (!SetFields(suggestion, PlaceTypes.Slipway, input, out var error))
                    return new SubmitResult.Invalid(error);
                break;
            }
            case SuggestionKinds.Edit or SuggestionKinds.Tip or SuggestionKinds.Report:
            {
                if (input.PlaceType is not (PlaceTypes.Slipway or PlaceTypes.Marina) || input.PlaceId is not { } placeId)
                    return new SubmitResult.Invalid("Es fehlt, um welche Slipanlage oder Marina es geht.");
                if (await FindPlaceAsync(input.PlaceType, placeId, cancellationToken) is null)
                    return new SubmitResult.NotFound();
                (suggestion.PlaceType, suggestion.PlaceId) = (input.PlaceType, placeId);

                if (input.Kind == SuggestionKinds.Edit)
                {
                    if (input.Fields is not { Count: > 0 })
                        return new SubmitResult.Invalid("Es fehlt, was geändert werden soll.");
                    if (!SetFields(suggestion, input.PlaceType, input, out var error))
                        return new SubmitResult.Invalid(error);
                }
                else
                {
                    var maxLength = input.Kind == SuggestionKinds.Tip ? SuggestionFields.MaxTipLength : SuggestionFields.MaxTextLength;
                    if (SuggestionFields.CleanText(input.Text, maxLength, multiline: true) is not { } text)
                        return new SubmitResult.Invalid($"Bitte einen Text mit höchstens {maxLength} Zeichen und ohne Links schreiben.");
                    suggestion.Text = text;
                    return await SaveAsync(suggestion, cancellationToken);
                }
                break;
            }
            default:
                return new SubmitResult.Invalid("Unbekannte Art von Vorschlag.");
        }

        if (input.Text is { Length: > 0 })
        {
            if (SuggestionFields.CleanText(input.Text, SuggestionFields.MaxTextLength, multiline: true) is not { } text)
                return new SubmitResult.Invalid($"Der Kommentar darf höchstens {SuggestionFields.MaxTextLength} Zeichen haben und keine Links enthalten.");
            suggestion.Text = text;
        }
        return await SaveAsync(suggestion, cancellationToken);
    }

    private static bool SetFields(Suggestion suggestion, string placeType, SuggestionInput input, out string error)
    {
        var (values, invalid) = SuggestionFields.Validate(placeType, input.Fields);
        error = invalid ?? "";
        if (invalid is null && values.Count > 0)
            suggestion.Fields = JsonSerializer.Serialize(values, Json);
        return invalid is null;
    }

    private async Task<SubmitResult> SaveAsync(Suggestion suggestion, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_');
        suggestion.Id = Guid.NewGuid();
        suggestion.Created = time.GetUtcNow().UtcDateTime;
        suggestion.TokenHash = HashToken(token);
        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        return new SubmitResult.Created(suggestion, token);
    }

    /// <summary>The suggestion, when the token is the one its sender got.</summary>
    public async Task<Suggestion?> FindForSenderAsync(Guid id, string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return null;
        var suggestion = await db.Suggestions.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return suggestion?.TokenHash is { } hash
               && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash), Encoding.ASCII.GetBytes(HashToken(token)))
            ? suggestion
            : null;
    }

    /// <summary>Deletes a suggestion its sender takes back; only while it waits for moderation.</summary>
    public async Task<bool> WithdrawAsync(Suggestion suggestion, CancellationToken cancellationToken = default)
    {
        if (suggestion.Status != SuggestionStatuses.Pending)
            return false;
        db.Suggestions.Remove(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static IReadOnlyDictionary<string, string> FieldsOf(Suggestion suggestion) =>
        suggestion.Fields is null
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(suggestion.Fields, Json) ?? [];

    public async Task<IOsmPlace?> FindPlaceAsync(string? placeType, Guid? id, CancellationToken cancellationToken = default) =>
        placeType switch
        {
            PlaceTypes.Slipway => await db.Slipways.SingleOrDefaultAsync(s => s.Id == id, cancellationToken),
            PlaceTypes.Marina => await db.Marinas.SingleOrDefaultAsync(m => m.Id == id, cancellationToken),
            _ => null,
        };

    public async Task<List<NearbyPlace>> NearbySlipwaysAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        // A box a little larger than the distance first, so only a handful of rows need the exact distance.
        var degrees = NearbyDistance / GeoMath.MetresPerDegree * 2;
        var candidates = await db.Slipways.AsNoTracking()
            .Where(s => s.Latitude > latitude - degrees && s.Latitude < latitude + degrees
                        && s.Longitude > longitude - degrees * 2 && s.Longitude < longitude + degrees * 2)
            .Select(s => new { s.Id, s.Name, s.Latitude, s.Longitude, s.Source })
            .ToListAsync(cancellationToken);
        return candidates
            .Select(s => new NearbyPlace(s.Id, s.Name, Math.Round(GeoMath.Haversine(latitude, longitude, s.Latitude, s.Longitude)), s.Source))
            .Where(s => s.Distance <= NearbyDistance)
            .OrderBy(s => s.Distance)
            .ToList();
    }

    private async Task<bool> PendingNewSlipwayNearAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var pending = await db.Suggestions.AsNoTracking()
            .Where(s => s.Kind == SuggestionKinds.NewSlipway && s.Status == SuggestionStatuses.Pending)
            .Select(s => new { s.Latitude, s.Longitude })
            .ToListAsync(cancellationToken);
        return pending.Any(s => GeoMath.Haversine(latitude, longitude, s.Latitude!.Value, s.Longitude!.Value) <= OsmImport.SamePlaceDistance);
    }

    public async Task<List<Suggestion>> ListAsync(string status, CancellationToken cancellationToken = default)
    {
        await CleanUpAsync(cancellationToken);
        var query = db.Suggestions.AsNoTracking().Where(s => s.Status == status);
        // Oldest first while waiting, newest first once decided.
        return status == SuggestionStatuses.Pending
            ? await query.OrderBy(s => s.Created).ToListAsync(cancellationToken)
            : await query.OrderByDescending(s => s.Decided).Take(100).ToListAsync(cancellationToken);
    }

    /// <summary>Applies a suggestion: a new slipway of our own, values for fields, or a tip; reports only get marked.</summary>
    /// <returns>Null when done, else why it could not be applied.</returns>
    public async Task<string?> AcceptAsync(Suggestion suggestion, CancellationToken cancellationToken = default)
    {
        if (suggestion.Status != SuggestionStatuses.Pending)
            return "Der Vorschlag ist schon entschieden.";

        var now = time.GetUtcNow().UtcDateTime;
        var fields = FieldsOf(suggestion);
        switch (suggestion.Kind)
        {
            case SuggestionKinds.NewSlipway:
            {
                var shapes = await db.Waters.AsNoTracking()
                    .Where(w => w.Shape != null)
                    .Select(w => new { w.Id, w.Shape })
                    .ToListAsync(cancellationToken);
                var slipway = new Slipway
                {
                    Id = Guid.NewGuid(),
                    Created = now,
                    Name = suggestion.Name!,
                    Latitude = suggestion.Latitude!.Value,
                    Longitude = suggestion.Longitude!.Value,
                    Source = Sources.Manual,
                    WaterId = new WaterLocator(shapes.Select(w => (w.Id, w.Shape!)))
                        .Find(suggestion.Latitude.Value, suggestion.Longitude.Value, OsmImport.WaterDistance),
                };
                db.Slipways.Add(slipway);
                Apply(slipway, PlaceTypes.Slipway, fields, now);
                break;
            }
            case SuggestionKinds.Edit:
            {
                if (await FindPlaceAsync(suggestion.PlaceType, suggestion.PlaceId, cancellationToken) is not { } place)
                    return "Den Eintrag gibt es nicht mehr.";
                await RemoveUserInfosAsync(suggestion.PlaceType!, place.Id, fields.Keys, cancellationToken);
                Apply(place, suggestion.PlaceType!, fields, now);
                if (place.Source != Sources.Osm)
                    ((Entity)place).Updated = now;
                break;
            }
            case SuggestionKinds.Tip:
            {
                if (await FindPlaceAsync(suggestion.PlaceType, suggestion.PlaceId, cancellationToken) is not { } place)
                    return "Den Eintrag gibt es nicht mehr.";
                db.PlaceInfos.Add(UserInfo(place, suggestion.PlaceType!, PlaceInfoFields.Tip, suggestion.Text!, null, now));
                break;
            }
        }

        suggestion.Status = SuggestionStatuses.Accepted;
        suggestion.Decided = now;
        db.Update(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> RejectAsync(Suggestion suggestion, CancellationToken cancellationToken = default)
    {
        if (suggestion.Status != SuggestionStatuses.Pending)
            return "Der Vorschlag ist schon entschieden.";
        suggestion.Status = SuggestionStatuses.Rejected;
        suggestion.Decided = time.GetUtcNow().UtcDateTime;
        db.Update(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    /// <summary>
    /// Our own places take values into their columns. Imported ones never do, since the import would overwrite them
    /// and they would mix with the ODbL data: there the values go into <see cref="PlaceInfo"/> rows, together with what
    /// OpenStreetMap had, so <see cref="ReconcileAfterImport"/> can tell later whether it caught up.
    /// </summary>
    private void Apply(IOsmPlace place, string placeType, IReadOnlyDictionary<string, string> fields, DateTime now)
    {
        foreach (var (name, value) in fields)
        {
            var field = SuggestionFields.All[name];
            if (place.Source != Sources.Osm && field.Write is not null)
                field.Write(place, value);
            else
                db.PlaceInfos.Add(UserInfo(place, placeType, name, value, place.Source == Sources.Osm ? field.Read?.Invoke(place) : null, now));
        }
    }

    private static PlaceInfo UserInfo(IOsmPlace place, string placeType, string field, string value, string? baseValue, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Created = now,
            PlaceType = placeType,
            PlaceId = place.Id,
            OsmType = place.OsmType,
            OsmId = place.OsmId,
            Field = field,
            Value = value,
            BaseValue = baseValue,
            Source = Sources.User,
            Retrieved = now,
        };

    /// <summary>A newer value from a user replaces the older one; tips are the only field with several rows.</summary>
    private async Task RemoveUserInfosAsync(string placeType, Guid placeId, IEnumerable<string> fields, CancellationToken cancellationToken)
    {
        var names = fields.ToList();
        db.PlaceInfos.RemoveRange(await db.PlaceInfos
            .Where(i => i.Source == Sources.User && i.PlaceType == placeType && i.PlaceId == placeId && names.Contains(i.Field))
            .ToListAsync(cancellationToken));
    }

    /// <summary>Deletes decided suggestions after <see cref="SuggestionOptions.KeepRejected"/> and <see cref="SuggestionOptions.KeepAccepted"/>.</summary>
    public async Task CleanUpAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var rejected = now - options.Value.KeepRejected;
        var accepted = now - options.Value.KeepAccepted;
        await db.Suggestions
            .Where(s => (s.Status == SuggestionStatuses.Rejected && s.Decided < rejected)
                        || (s.Status == SuggestionStatuses.Accepted && s.Decided < accepted))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Compares values from users on imported places with what OpenStreetMap has after an import, before it is saved.
    /// Where OpenStreetMap now has the user's value, the row is no longer needed. Where OpenStreetMap changed to
    /// something else, the row goes back to moderation as a new suggestion, and OpenStreetMap's value shows until then.
    /// </summary>
    /// <returns>Rows OpenStreetMap caught up with, and rows sent back to moderation.</returns>
    public static (int Resolved, int Conflicts) ReconcileAfterImport(
        SlipwaysDbContext db, IEnumerable<IOsmPlace> slipways, IEnumerable<IOsmPlace> marinas, List<PlaceInfo> userInfos, DateTime now)
    {
        var places = new Dictionary<(string, Guid), IOsmPlace>();
        foreach (var slipway in slipways.Where(p => p.Source == Sources.Osm))
            places[(PlaceTypes.Slipway, slipway.Id)] = slipway;
        foreach (var marina in marinas.Where(p => p.Source == Sources.Osm))
            places[(PlaceTypes.Marina, marina.Id)] = marina;

        int resolved = 0, conflicts = 0;
        foreach (var info in userInfos)
        {
            if (!SuggestionFields.All.TryGetValue(info.Field, out var field) || !field.FromOsm
                || !places.TryGetValue((info.PlaceType, info.PlaceId), out var place))
                continue;

            var current = field.Read!(place);
            if (current == info.Value)
            {
                db.PlaceInfos.Remove(info);
                resolved++;
            }
            else if (current != info.BaseValue)
            {
                db.PlaceInfos.Remove(info);
                db.Suggestions.Add(new Suggestion
                {
                    Id = Guid.NewGuid(),
                    Created = now,
                    Kind = SuggestionKinds.Edit,
                    PlaceType = info.PlaceType,
                    PlaceId = info.PlaceId,
                    Fields = JsonSerializer.Serialize(new Dictionary<string, string> { [info.Field] = info.Value }, Json),
                    Text = $"Nach dem OpenStreetMap-Import: OpenStreetMap hat jetzt „{current ?? "nichts"}“, " +
                           $"als der Wert angenommen wurde, war es „{info.BaseValue ?? "nichts"}“.",
                });
                conflicts++;
            }
        }
        return (resolved, conflicts);
    }

    public static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
