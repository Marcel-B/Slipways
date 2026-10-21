using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Slipways.Infrastructure.Notifications;

/// <summary>
/// When collected messages are due: at each of <see cref="NotificationOptions.Times"/> on each of
/// <see cref="NotificationOptions.Days"/>. Without times, every check is due and new suggestions are sent right away.
/// </summary>
public sealed class NotificationSchedule
{
    public static readonly NotificationSchedule Immediately = new(new HashSet<DayOfWeek>(), [], TimeZoneInfo.Utc);

    // German and English names, short and long, as people write them in a config file.
    private static readonly Dictionary<string, DayOfWeek> DayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mo"] = DayOfWeek.Monday, ["montag"] = DayOfWeek.Monday, ["mon"] = DayOfWeek.Monday, ["monday"] = DayOfWeek.Monday,
        ["di"] = DayOfWeek.Tuesday, ["dienstag"] = DayOfWeek.Tuesday, ["tue"] = DayOfWeek.Tuesday, ["tuesday"] = DayOfWeek.Tuesday,
        ["mi"] = DayOfWeek.Wednesday, ["mittwoch"] = DayOfWeek.Wednesday, ["wed"] = DayOfWeek.Wednesday, ["wednesday"] = DayOfWeek.Wednesday,
        ["do"] = DayOfWeek.Thursday, ["donnerstag"] = DayOfWeek.Thursday, ["thu"] = DayOfWeek.Thursday, ["thursday"] = DayOfWeek.Thursday,
        ["fr"] = DayOfWeek.Friday, ["freitag"] = DayOfWeek.Friday, ["fri"] = DayOfWeek.Friday, ["friday"] = DayOfWeek.Friday,
        ["sa"] = DayOfWeek.Saturday, ["samstag"] = DayOfWeek.Saturday, ["sat"] = DayOfWeek.Saturday, ["saturday"] = DayOfWeek.Saturday,
        ["so"] = DayOfWeek.Sunday, ["sonntag"] = DayOfWeek.Sunday, ["sun"] = DayOfWeek.Sunday, ["sunday"] = DayOfWeek.Sunday,
    };

    private readonly IReadOnlySet<DayOfWeek> _days;
    private readonly IReadOnlyList<TimeOnly> _times;
    private readonly TimeZoneInfo _zone;

    private NotificationSchedule(IReadOnlySet<DayOfWeek> days, IReadOnlyList<TimeOnly> times, TimeZoneInfo zone)
    {
        (_days, _times, _zone) = (days, times, zone);
    }

    public bool IsImmediate => _times.Count == 0;

    public static bool TryParse(NotificationOptions options, [NotNullWhen(true)] out NotificationSchedule? schedule, [NotNullWhen(false)] out string? error)
    {
        schedule = null;
        var days = new HashSet<DayOfWeek>();
        foreach (var name in Split(options.Days))
        {
            if (!DayNames.TryGetValue(name, out var day))
            {
                error = $"Unknown weekday '{name}' in {NotificationOptions.SectionName}:Days.";
                return false;
            }
            days.Add(day);
        }

        var times = new List<TimeOnly>();
        foreach (var text in Split(options.Times))
        {
            if (!TimeOnly.TryParseExact(text, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                error = $"Invalid time '{text}' in {NotificationOptions.SectionName}:Times, expected e.g. 18:00.";
                return false;
            }
            times.Add(time);
        }

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            error = $"Unknown time zone '{options.TimeZone}' in {NotificationOptions.SectionName}:TimeZone.";
            return false;
        }

        if (days.Count == 0)
            days = [.. Enum.GetValues<DayOfWeek>()];
        schedule = new NotificationSchedule(days, times, zone);
        error = null;
        return true;
    }

    /// <summary>The latest point in time at or before <paramref name="now"/> a collected message was due; null when immediate.</summary>
    public DateTimeOffset? LastSlot(DateTimeOffset now)
    {
        if (IsImmediate)
            return null;

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, _zone).DateTime);
        // A week back always reaches the last slot, since every configured weekday comes up once.
        for (var date = today; date >= today.AddDays(-7); date = date.AddDays(-1))
        {
            if (!_days.Contains(date.DayOfWeek))
                continue;
            var slots = _times.Select(t => ToUtc(date.ToDateTime(t))).Where(s => s <= now).ToList();
            if (slots.Count > 0)
                return slots.Max();
        }
        return null;
    }

    private DateTimeOffset ToUtc(DateTime local)
    {
        // A time skipped when the clocks go forward happens an hour later that day.
        if (_zone.IsInvalidTime(local))
            local = local.AddHours(1);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, _zone), TimeSpan.Zero);
    }

    private static IEnumerable<string> Split(string? list) =>
        (list ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
