using System.Text.Json.Nodes;
using Slipways.Infrastructure.Shapes;

namespace Slipways.Api.Tests;

public class ShapeSimplifierTests
{
    [Fact]
    public void Drops_vertices_close_to_the_line_and_keeps_the_ends()
    {
        var line = JsonNode.Parse("""{"type":"LineString","coordinates":[[0,0],[1,0.001],[2,-0.001],[3,1],[4,0]]}""")!;

        var simplified = ShapeSimplifier.Simplify(line, 0.01);

        Assert.Equal("""{"type":"LineString","coordinates":[[0,0],[2,-0.001],[3,1],[4,0]]}""", simplified.ToJsonString());
    }

    [Fact]
    public void Rounds_to_about_ten_metres()
    {
        var line = JsonNode.Parse("""{"type":"LineString","coordinates":[[7.123456789,50.987654321],[8.5,51.25]]}""")!;

        var simplified = ShapeSimplifier.Simplify(line, 0.01);

        Assert.Equal("""{"type":"LineString","coordinates":[[7.1235,50.9877],[8.5,51.25]]}""", simplified.ToJsonString());
    }

    [Fact]
    public void Keeps_small_lakes_as_they_are()
    {
        const string lake = """{"type":"Polygon","coordinates":[[[10,49],[10.001,49],[10.001,49.001],[10,49.001],[10,49]]]}""";

        var simplified = ShapeSimplifier.Simplify(JsonNode.Parse(lake)!, 0.01);

        Assert.Equal(lake, simplified.ToJsonString());
    }

    [Fact]
    public void Simplifies_every_part_of_a_collection()
    {
        var collection = JsonNode.Parse("""
            {"type":"GeometryCollection","geometries":[
              {"type":"MultiLineString","coordinates":[[[0,0],[1,0],[2,0]],[[5,5],[6,5.0001],[7,5]]]},
              {"type":"Point","coordinates":[3,3]}
            ]}
            """)!;

        var simplified = ShapeSimplifier.Simplify(collection, 0.01);

        Assert.Equal(
            """{"type":"GeometryCollection","geometries":[{"type":"MultiLineString","coordinates":[[[0,0],[2,0]],[[5,5],[7,5]]]},{"type":"Point","coordinates":[3,3]}]}""",
            simplified.ToJsonString());
    }
}
