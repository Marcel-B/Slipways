using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Websites;

public sealed record WebsiteSyncResult(int Checked, int Found);

/// <summary>
/// Reads the home pages of marinas that are due (never read, read longer than <see cref="WebsiteOptions.RefreshAfter"/>
/// ago, or with a changed address) and keeps what <see cref="WebsiteParser"/> finds as <see cref="PlaceInfo"/> with
/// <see cref="Sources.Website"/>. Nothing is written into the marinas themselves, so data from OpenStreetMap and from
/// websites stay apart.
/// </summary>
public sealed class WebsiteSync(SlipwaysDbContext db, WebsiteClient client, IOptions<WebsiteOptions> options, TimeProvider time)
{
    public async Task<WebsiteSyncResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var marinas = await db.Marinas.AsNoTracking()
            .Where(m => m.Url != null)
            .Select(m => new Target(m.Id, m.Url!, m.OsmType, m.OsmId))
            .ToListAsync(cancellationToken);
        var checks = await db.WebsiteChecks.Where(c => c.PlaceType == PlaceTypes.Marina).ToDictionaryAsync(c => c.PlaceId, cancellationToken);

        await ForgetRemovedWebsitesAsync(marinas.Select(m => m.Id).ToHashSet(), checks, cancellationToken);

        var due = marinas
            .Select(m => (Marina: m, Check: checks.GetValueOrDefault(m.Id)))
            .Where(m => m.Check is null
                || (m.Check.Result != WebsiteCheckResults.Excluded
                    && (m.Check.Url != m.Marina.Url || m.Check.Checked < now - options.Value.RefreshAfter)))
            .OrderBy(m => m.Check?.Checked ?? DateTime.MinValue)
            .Take(options.Value.BatchSize)
            .ToList();

        var found = 0;
        foreach (var (marina, check) in due)
        {
            var (id, url) = (marina.Id, marina.Url);
            var result = await CheckAsync(marina, cancellationToken);
            if (result == WebsiteCheckResults.Found)
                found++;
            SlipwaysMetrics.WebsiteChecks.WithLabels(result).Inc();

            if (check is null)
            {
                db.WebsiteChecks.Add(new WebsiteCheck
                {
                    PlaceType = PlaceTypes.Marina, PlaceId = id, Url = url, Checked = now, Result = result,
                });
            }
            else
            {
                check.Url = url;
                check.Checked = now;
                check.Result = result;
            }

            // Saved one by one, so a run cut short by a restart keeps what it did.
            await db.SaveChangesAsync(cancellationToken);
        }

        return new WebsiteSyncResult(due.Count, found);
    }

    private async Task<string> CheckAsync(Target marina, CancellationToken cancellationToken)
    {
        var page = Address(marina.Url) is { } address
            ? await client.GetHomePageAsync(address, cancellationToken)
            : new WebsitePage(WebsiteCheckResults.Refused);

        // A site that is down keeps what it said last time; one that no longer says it, or forbids reading it, does not.
        if (page.Result == WebsiteCheckResults.Failed)
            return page.Result;

        var details = page.Html is null ? null : WebsiteParser.Parse(page.Html);
        await ReplaceAsync(marina, details, page.Url, cancellationToken);
        return page.Html is null ? page.Result
            : details!.IsEmpty ? WebsiteCheckResults.Empty
            : WebsiteCheckResults.Found;
    }

    private async Task ReplaceAsync(Target marina, WebsiteDetails? details, Uri? source, CancellationToken cancellationToken)
    {
        var marinaId = marina.Id;
        var now = time.GetUtcNow().UtcDateTime;
        var existing = await db.PlaceInfos
            .Where(i => i.PlaceType == PlaceTypes.Marina && i.PlaceId == marinaId && i.Source == Sources.Website)
            .ToListAsync(cancellationToken);

        (string Field, string? Value)[] values =
        [
            (PlaceInfoFields.Phone, details?.Phone),
            (PlaceInfoFields.Email, details?.Email),
            (PlaceInfoFields.OpeningHours, details?.OpeningHours),
        ];
        foreach (var (field, value) in values)
        {
            var info = existing.FirstOrDefault(i => i.Field == field);
            if (value is null)
            {
                if (info is not null)
                    db.PlaceInfos.Remove(info);
                continue;
            }

            if (info is null)
            {
                db.PlaceInfos.Add(new PlaceInfo
                {
                    Created = now, PlaceType = PlaceTypes.Marina, PlaceId = marinaId, OsmType = marina.OsmType, OsmId = marina.OsmId,
                    Field = field, Value = value,
                    Source = Sources.Website, SourceUrl = source?.AbsoluteUri, Retrieved = now,
                });
                continue;
            }

            if (info.Value != value)
            {
                info.Value = value;
                info.Updated = now;
            }
            info.SourceUrl = source?.AbsoluteUri;
            info.OsmType = marina.OsmType;
            info.OsmId = marina.OsmId;
            info.Retrieved = now;
        }
    }

    /// <summary>Drops checks and details of marinas that no longer link a website; exclusions are kept.</summary>
    private async Task ForgetRemovedWebsitesAsync(
        HashSet<Guid> withWebsite, Dictionary<Guid, WebsiteCheck> checks, CancellationToken cancellationToken)
    {
        var removed = checks.Values
            .Where(c => !withWebsite.Contains(c.PlaceId) && c.Result != WebsiteCheckResults.Excluded)
            .Select(c => c.PlaceId)
            .ToList();
        if (removed.Count == 0)
            return;

        db.WebsiteChecks.RemoveRange(removed.Select(id => checks[id]));
        foreach (var id in removed)
            checks.Remove(id);
        await db.PlaceInfos
            .Where(i => i.PlaceType == PlaceTypes.Marina && i.Source == Sources.Website && removed.Contains(i.PlaceId))
            .ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record Target(Guid Id, string Url, string? OsmType, long? OsmId);

    /// <summary>The address to read; OpenStreetMap often has "www.example.de" without a scheme.</summary>
    private static Uri? Address(string url)
    {
        url = url.Trim();
        if (!url.Contains("://", StringComparison.Ordinal))
            url = "http://" + url;
        return Uri.TryCreate(url, UriKind.Absolute, out var address) ? address : null;
    }
}
