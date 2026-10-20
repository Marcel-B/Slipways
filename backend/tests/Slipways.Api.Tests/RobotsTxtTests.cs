using Slipways.Infrastructure.Websites;

namespace Slipways.Api.Tests;

public class RobotsTxtTests
{
    [Theory]
    [InlineData("/", true)]
    [InlineData("/intern/", false)]
    [InlineData("/intern/oeffentlich.html", true)]
    [InlineData("/suche?q=boot", false)]
    [InlineData("/bild.pdf", false)]
    [InlineData("/bild.pdf?x=1", true)]
    public void Applies_the_longest_matching_rule_for_everyone(string path, bool allowed)
    {
        var robots = RobotsTxt.Parse("""
            # comment
            User-agent: Googlebot
            Disallow: /

            User-agent: *
            Disallow: /intern/
            Allow: /intern/oeffentlich
            Disallow: /suche
            Disallow: /*.pdf$
            """);

        Assert.Equal(allowed, robots.Allows(new Uri("https://hafen.example" + path)));
    }

    [Fact]
    public void Prefers_a_group_for_our_product_token_over_the_one_for_everyone()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow: /

            User-agent: Slipways
            Disallow:
            """);

        Assert.True(robots.Allows(new Uri("https://hafen.example/")));
    }

    [Fact]
    public void Disallows_everything_when_asked_to()
    {
        var robots = RobotsTxt.Parse("User-agent: *\r\nDisallow: /\r\n");

        Assert.False(robots.Allows(new Uri("https://hafen.example/")));
    }

    [Fact]
    public void Shares_rules_among_agents_of_one_group()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: otherbot
            User-agent: slipways
            Disallow: /privat
            """);

        Assert.False(robots.Allows(new Uri("https://hafen.example/privat/a")));
        Assert.True(robots.Allows(new Uri("https://hafen.example/")));
    }
}
