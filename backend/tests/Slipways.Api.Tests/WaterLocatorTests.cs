using Slipways.Infrastructure.Osm;

namespace Slipways.Api.Tests;

public class WaterLocatorTests
{
    private static readonly Guid River = Guid.NewGuid();
    private static readonly Guid Lake = Guid.NewGuid();

    // A river along 48.0° N and, 0.01° (about 1.1 km) north of it, a lake with an island in the middle.
    private static readonly WaterLocator Locator = new([
        (River, """{"type":"MultiLineString","coordinates":[[[12.0,48.0],[12.1,48.0]]]}"""),
        (Lake, """
            {"type":"GeometryCollection","geometries":[{"type":"Polygon","coordinates":[
              [[12.0,48.01],[12.1,48.01],[12.1,48.05],[12.0,48.05],[12.0,48.01]],
              [[12.04,48.02],[12.06,48.02],[12.06,48.04],[12.04,48.04],[12.04,48.02]]]}]}
            """),
    ]);

    [Theory]
    [InlineData(48.002, 12.05, true)] // about 220 m from the river
    [InlineData(48.004, 12.05, false)] // about 440 m
    [InlineData(48.0, 12.103, true)] // beyond the end of the river, about 220 m away
    public void Finds_a_river_within_the_distance(double latitude, double longitude, bool found) =>
        Assert.Equal(found ? River : null, Locator.Find(latitude, longitude, 300));

    [Fact]
    public void Finds_a_lake_the_place_lies_in()
    {
        Assert.Equal(Lake, Locator.Find(48.03, 12.02, 300));
        // On the island, far from every shore.
        Assert.Null(Locator.Find(48.03, 12.05, 300));
    }

    [Fact]
    public void Takes_the_closest_water()
    {
        // 0.006° north of the river, 0.004° south of the lake.
        Assert.Equal(Lake, Locator.Find(48.006, 12.05, 1000));
    }
}
