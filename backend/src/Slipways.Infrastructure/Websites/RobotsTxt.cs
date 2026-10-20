using System.Text.RegularExpressions;

namespace Slipways.Infrastructure.Websites;

/// <summary>
/// The rules of a robots.txt (RFC 9309) that apply to us: the groups for our product token, or else those for "*".
/// The longest matching rule wins, and of two equally long ones, allow.
/// </summary>
public sealed class RobotsTxt
{
    /// <summary>The name we look for in <c>user-agent</c> lines; the start of our user agent.</summary>
    public const string ProductToken = "slipways";

    /// <summary>For a missing robots.txt (4xx), which means everything is allowed.</summary>
    public static readonly RobotsTxt AllowAll = new([]);

    private readonly IReadOnlyList<Rule> _rules;

    private RobotsTxt(IReadOnlyList<Rule> rules) => _rules = rules;

    public static RobotsTxt Parse(string content)
    {
        var ours = new List<Rule>();
        var anyone = new List<Rule>();
        var agents = new List<string>();
        var inRules = false;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Split('#', 2)[0].Trim();
            if (line.Split(':', 2) is not [var rawKey, var rawValue])
                continue;
            var (key, value) = (rawKey.Trim().ToLowerInvariant(), rawValue.Trim());

            if (key == "user-agent")
            {
                // A user-agent line after rules starts a new group.
                if (inRules)
                {
                    agents.Clear();
                    inRules = false;
                }
                agents.Add(value.ToLowerInvariant());
                continue;
            }

            if (key is not ("allow" or "disallow"))
                continue;
            inRules = true;
            // An empty disallow allows everything, so it is no rule at all.
            if (value.Length == 0)
                continue;

            var rule = new Rule(value, key == "allow");
            if (agents.Contains(ProductToken))
                ours.Add(rule);
            if (agents.Contains("*"))
                anyone.Add(rule);
        }

        return new RobotsTxt(ours.Count > 0 || HasOwnGroup(content) ? ours : anyone);
    }

    /// <summary>Whether we may read <paramref name="url"/>, judged by its path and query.</summary>
    public bool Allows(Uri url)
    {
        var path = url.PathAndQuery;
        var best = _rules
            .Where(r => r.Matches(path))
            .OrderByDescending(r => r.Pattern.Length)
            .ThenByDescending(r => r.Allow)
            .FirstOrDefault();
        return best is null || best.Allow;
    }

    // A group for us without any rules (e.g. "User-agent: slipways" and "Disallow:") still replaces the one for "*".
    private static bool HasOwnGroup(string content) =>
        Regex.IsMatch(content, $@"^\s*user-agent\s*:\s*{ProductToken}\s*(#.*)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private sealed record Rule(string Pattern, bool Allow)
    {
        private readonly Regex _regex = new(
            "^" + Regex.Escape(Pattern).Replace(@"\*", ".*").Replace(@"\$", "$"),
            RegexOptions.CultureInvariant);

        public bool Matches(string path) => _regex.IsMatch(path);
    }
}
