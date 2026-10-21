namespace Slipways.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Turns notifications about new suggestions off; without a <see cref="Topic"/> they are off as well.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>ntfy server; ntfy.sh is free and needs no account, a self-hosted one works the same.</summary>
    public Uri BaseUrl { get; set; } = new("https://ntfy.sh/");

    /// <summary>
    /// Topic the messages go to, subscribed to in the ntfy app. On ntfy.sh anyone who knows the name can read it, so it
    /// should be long and random; the messages only hold counts anyway.
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>Access token for a server or topic that needs one; sent as bearer token.</summary>
    public string? Token { get; set; }

    /// <summary>Address of the moderation page, opened when the message is tapped, e.g. in the tailnet.</summary>
    public string? ModerationUrl { get; set; }

    /// <summary>How often the queue is looked at; with no <see cref="Times"/>, that is how soon a message comes.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Weekdays for collected messages, comma-separated, e.g. "Mo,Do" or "Monday,Thursday"; empty means every day.
    /// Only used together with <see cref="Times"/>.
    /// </summary>
    public string? Days { get; set; }

    /// <summary>
    /// Times of day for collected messages, comma-separated, e.g. "08:00,18:00". Empty sends a message as soon as
    /// something new is waiting.
    /// </summary>
    public string? Times { get; set; }

    /// <summary>Time zone of <see cref="Days"/> and <see cref="Times"/>.</summary>
    public string TimeZone { get; set; } = "Europe/Berlin";
}
