using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Notifications;

/// <param name="Due">Whether this check was due at all; collected messages are only due after one of the times.</param>
/// <param name="New">Suggestions that came in since the last message and are still waiting.</param>
public sealed record NotificationResult(bool Due, int New, int Pending, bool Sent);

/// <summary>
/// Tells the moderator about suggestions that came in since the last message, either on every check or at the times of
/// the <see cref="NotificationSchedule"/>. The message only holds counts per kind, nothing a user wrote. Reported
/// suggestions get <see cref="Suggestion.Notified"/>, and the last due check is kept in the <see cref="JobState"/> row
/// of the job, so a restart neither repeats nor loses a message.
/// </summary>
public sealed class SuggestionNotifier(SlipwaysDbContext db, NtfyClient client, IOptions<NotificationOptions> options, TimeProvider time)
{
    /// <summary>Name of the job in <see cref="JobState"/> and the <c>task</c> label of its metrics.</summary>
    public const string JobName = "notifications";

    public async Task<NotificationResult> RunAsync(NotificationSchedule schedule, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var state = await db.JobStates.SingleOrDefaultAsync(s => s.Name == JobName, cancellationToken)
            ?? db.JobStates.Add(new JobState { Name = JobName }).Entity;
        // LastSuccess is the last due check; a collected message is due once per slot after it.
        if (!schedule.IsImmediate && (schedule.LastSlot(now) is not { } slot || state.LastSuccess >= slot.UtcDateTime))
            return new NotificationResult(Due: false, New: 0, Pending: 0, Sent: false);

        var pending = await db.Suggestions.CountAsync(s => s.Status == SuggestionStatuses.Pending, cancellationToken);
        var fresh = await db.Suggestions
            .Where(s => s.Status == SuggestionStatuses.Pending && s.Notified == null)
            .ToListAsync(cancellationToken);

        if (fresh.Count > 0)
        {
            var kinds = fresh.GroupBy(s => s.Kind).ToDictionary(g => g.Key, g => g.Count());
            var url = Uri.TryCreate(options.Value.ModerationUrl, UriKind.Absolute, out var moderation) ? moderation : null;
            await client.SendAsync(Compose(kinds, pending, url), cancellationToken);
            foreach (var suggestion in fresh)
                suggestion.Notified = now.UtcDateTime;
        }

        state.LastSuccess = now.UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return new NotificationResult(Due: true, New: fresh.Count, Pending: pending, Sent: fresh.Count > 0);
    }

    public static NtfyMessage Compose(IReadOnlyDictionary<string, int> newByKind, int pending, Uri? moderationUrl)
    {
        var total = newByKind.Values.Sum();
        var title = total == 1 ? "Slipways: 1 neuer Vorschlag" : $"Slipways: {total} neue Vorschläge";
        var parts = SuggestionKinds.All
            .Where(newByKind.ContainsKey)
            .Select(kind => Describe(kind, newByKind[kind]))
            .Concat(newByKind.Keys.Except(SuggestionKinds.All).Select(kind => $"{newByKind[kind]} × {kind}"));
        var waiting = pending == 1 ? "Insgesamt wartet 1 Vorschlag." : $"Insgesamt warten {pending} Vorschläge.";
        return new NtfyMessage(title, $"{string.Join(", ", parts)}. {waiting}", moderationUrl);
    }

    private static string Describe(string kind, int count) => (kind, count) switch
    {
        (SuggestionKinds.NewSlipway, 1) => "1 neue Slipanlage",
        (SuggestionKinds.NewSlipway, _) => $"{count} neue Slipanlagen",
        (SuggestionKinds.Edit, 1) => "1 Änderung",
        (SuggestionKinds.Edit, _) => $"{count} Änderungen",
        (SuggestionKinds.Tip, 1) => "1 Tipp",
        (SuggestionKinds.Tip, _) => $"{count} Tipps",
        (SuggestionKinds.Report, 1) => "1 Meldung",
        _ => $"{count} Meldungen",
    };
}
