using System.Text.Json.Nodes;

namespace Slipways.Infrastructure.Shapes;

/// <summary>Thins out GeoJSON geometries with Douglas–Peucker, e.g. to draw every water on one overview map.</summary>
public static class ShapeSimplifier
{
    /// <summary>
    /// Drops vertices closer than <paramref name="tolerance"/> degrees to the simplified line and rounds the rest to
    /// four decimals (about 10 m). Rings that would collapse are kept as they are, so small lakes stay areas.
    /// </summary>
    public static JsonNode Simplify(JsonNode geometry, double tolerance)
    {
        var type = (string?)geometry["type"];
        if (type == "GeometryCollection")
        {
            return new JsonObject
            {
                ["type"] = type,
                ["geometries"] = new JsonArray(geometry["geometries"]!.AsArray().Select(g => Simplify(g!, tolerance)).ToArray()),
            };
        }

        var coordinates = geometry["coordinates"]!.AsArray();
        JsonNode simplified = type switch
        {
            "LineString" => Line(coordinates, tolerance, minimum: 2),
            "MultiLineString" => Each(coordinates, line => Line(line, tolerance, minimum: 2)),
            "Polygon" => Each(coordinates, ring => Line(ring, tolerance, minimum: 4)),
            "MultiPolygon" => Each(coordinates, polygon => Each(polygon, ring => Line(ring, tolerance, minimum: 4))),
            _ => coordinates.DeepClone(),
        };
        return new JsonObject { ["type"] = type, ["coordinates"] = simplified };
    }

    private static JsonArray Each(JsonArray parts, Func<JsonArray, JsonArray> simplify) =>
        new(parts.Select(p => (JsonNode)simplify(p!.AsArray())).ToArray());

    private static JsonArray Line(JsonArray line, double tolerance, int minimum)
    {
        var points = line.Select(p => (X: (double)p![0]!, Y: (double)p[1]!)).ToArray();
        var keep = new bool[points.Length];
        keep[0] = keep[^1] = true;

        // Iterative, since rivers have thousands of vertices.
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, points.Length - 1));
        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();
            var (farthest, distance) = (-1, 0.0);
            for (var i = start + 1; i < end; i++)
            {
                var d = Distance(points[i], points[start], points[end]);
                if (d > distance)
                    (farthest, distance) = (i, d);
            }
            if (farthest < 0 || distance <= tolerance)
                continue;
            keep[farthest] = true;
            stack.Push((start, farthest));
            stack.Push((farthest, end));
        }

        var kept = points.Where((_, i) => keep[i]).ToList();
        if (kept.Count < minimum)
            kept = [.. points];
        return new JsonArray(kept.Select(p => (JsonNode)new JsonArray(Math.Round(p.X, 4), Math.Round(p.Y, 4))).ToArray());
    }

    /// <summary>Distance from p to the segment a–b, in degrees.</summary>
    private static double Distance((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        var t = dx == 0 && dy == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
        return Math.Sqrt(Math.Pow(p.X - a.X - t * dx, 2) + Math.Pow(p.Y - a.Y - t * dy, 2));
    }
}
