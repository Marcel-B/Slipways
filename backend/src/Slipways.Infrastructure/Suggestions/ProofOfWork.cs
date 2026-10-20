using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Suggestions;

/// <summary>A challenge in the format of ALTCHA (altcha.org): find <c>number</c> with SHA-256(salt + number) = challenge.</summary>
public sealed record ProofChallenge(string Algorithm, string Challenge, int MaxNumber, string Salt, string Signature);

public sealed record ProofSolution(string Algorithm, string Challenge, int Number, string Salt, string Signature);

/// <summary>
/// Proof of work against automated suggestions, without a third party, cookies or anything about the client: each
/// suggestion costs the sender a second or so of computing, which adds up for anyone sending thousands. Challenges are
/// signed instead of stored, carry their expiry in the salt, and each one is accepted only once.
/// </summary>
public sealed class ProofOfWork(IOptions<SuggestionOptions> options, TimeProvider time)
{
    public const string Algorithm = "SHA-256";

    private readonly byte[] _key = options.Value.ChallengeKey is { Length: > 0 } key
        ? Encoding.UTF8.GetBytes(key)
        : RandomNumberGenerator.GetBytes(32);

    // Challenges already used, until they expire anyway.
    private readonly ConcurrentDictionary<string, DateTimeOffset> _used = new();

    public ProofChallenge CreateChallenge()
    {
        var expires = time.GetUtcNow() + options.Value.ChallengeLifetime;
        var salt = $"{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12))}?expires={expires.ToUnixTimeSeconds()}";
        var number = RandomNumberGenerator.GetInt32(options.Value.ChallengeMaxNumber + 1);
        var challenge = Hash(salt, number);
        return new ProofChallenge(Algorithm, challenge, options.Value.ChallengeMaxNumber, salt, Sign(challenge));
    }

    public bool Verify(ProofSolution? solution)
    {
        if (solution is null || solution.Algorithm != Algorithm || solution.Number < 0)
            return false;

        var now = time.GetUtcNow();
        if (Expiry(solution.Salt) is not { } expires || expires < now)
            return false;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(solution.Challenge)), Encoding.ASCII.GetBytes(solution.Signature))
            || Hash(solution.Salt, solution.Number) != solution.Challenge)
            return false;

        foreach (var (challenge, until) in _used)
        {
            if (until < now)
                _used.TryRemove(challenge, out _);
        }
        return _used.TryAdd(solution.Challenge, expires);
    }

    private static DateTimeOffset? Expiry(string salt)
    {
        var at = salt.IndexOf("?expires=", StringComparison.Ordinal);
        return at >= 0 && long.TryParse(salt[(at + 9)..], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    public static string Hash(string salt, int number) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(salt + number.ToString(CultureInfo.InvariantCulture))));

    private string Sign(string challenge) => Convert.ToHexStringLower(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(challenge)));
}
