namespace Slipways.Infrastructure.Suggestions;

public sealed class SuggestionOptions
{
    public const string SectionName = "Suggestions";

    /// <summary>Turns sending suggestions off; what was accepted stays visible.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Key that signs the proof-of-work challenges. Without one, a random key per process is used, so a restart only
    /// invalidates challenges that are a few minutes old at most.
    /// </summary>
    public string? ChallengeKey { get; set; }

    /// <summary>Upper bound of the number a client has to find; on average it tries half of them (about a second in a browser).</summary>
    public int ChallengeMaxNumber { get; set; } = 100_000;

    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Rejected suggestions are deleted this long after the decision.</summary>
    public TimeSpan KeepRejected { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Accepted ones too; their content lives on in the place by then.</summary>
    public TimeSpan KeepAccepted { get; set; } = TimeSpan.FromDays(90);
}
