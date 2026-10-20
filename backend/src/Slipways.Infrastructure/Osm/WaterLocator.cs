using System.Text.Json;

namespace Slipways.Infrastructure.Osm;

/// <summary>
/// Finds the water a place lies on, by its distance to the shapes of the known waters (GeoJSON in WGS 84).
/// Works in memory, so it needs no spatial features of the database.
/// </summary>
public sealed class WaterLocator
{
    private readonly List<WaterGeometry> _waters;

    public WaterLocator(IEnumerable<(Guid WaterId, string Shape)> shapes) =>
        _waters = shapes.Select(s => WaterGeometry.Parse(s.WaterId, s.Shape)).Where(w => w.Lines.Count > 0).ToList();

    /// <summary>The water whose shape is closest to the position and at most <paramref name="maxDistance"/> metres away.</summary>
    public Guid? Find(double latitude, double longitude, double maxDistance)
    {
        var latMargin = maxDistance / GeoMath.MetresPerDegree;
        var lonMargin = latMargin / Math.Max(Math.Cos(GeoMath.ToRadians(latitude)), 0.01);

        Guid? closest = null;
        var closestDistance = maxDistance;
        foreach (var water in _waters)
        {
            if (latitude < water.South - latMargin || latitude > water.North + latMargin
                || longitude < water.West - lonMargin || longitude > water.East + lonMargin)
                continue;

            var distance = water.DistanceTo(latitude, longitude);
            if (distance <= closestDistance)
            {
                closest = water.WaterId;
                closestDistance = distance;
            }
        }
        return closest;
    }

    /// <summary>Longitude first, as in GeoJSON.</summary>
    private readonly record struct Coordinate(double Lon, double Lat);

    /// <param name="Lines">Line strings and polygon rings; a point is a line of one coordinate.</param>
    /// <param name="Polygons">Rings of each polygon, the first being the outer one.</param>
    private sealed record WaterGeometry(Guid WaterId, List<Coordinate[]> Lines, List<List<Coordinate[]>> Polygons)
    {
        public double South { get; } = Lines.SelectMany(l => l).DefaultIfEmpty().Min(c => c.Lat);
        public double North { get; } = Lines.SelectMany(l => l).DefaultIfEmpty().Max(c => c.Lat);
        public double West { get; } = Lines.SelectMany(l => l).DefaultIfEmpty().Min(c => c.Lon);
        public double East { get; } = Lines.SelectMany(l => l).DefaultIfEmpty().Max(c => c.Lon);

        public static WaterGeometry Parse(Guid waterId, string shape)
        {
            List<Coordinate[]> lines = [];
            List<List<Coordinate[]>> polygons = [];
            using var document = JsonDocument.Parse(shape);
            Add(document.RootElement, lines, polygons);
            return new WaterGeometry(waterId, lines, polygons);
        }

        /// <summary>Metres to the nearest line, or 0 inside a polygon such as a lake.</summary>
        public double DistanceTo(double latitude, double longitude)
        {
            var point = new Coordinate(longitude, latitude);
            if (Polygons.Any(rings => Contains(rings, point)))
                return 0;

            // Metres in a plane around the point; accurate enough for the few hundred metres that matter here.
            var lonScale = Math.Cos(GeoMath.ToRadians(latitude)) * GeoMath.MetresPerDegree;
            var distance = double.MaxValue;
            foreach (var line in Lines)
            {
                if (line.Length == 1)
                    distance = Math.Min(distance, SegmentDistance(line[0], line[0]));
                for (var i = 1; i < line.Length; i++)
                    distance = Math.Min(distance, SegmentDistance(line[i - 1], line[i]));
            }
            return distance;

            double SegmentDistance(Coordinate a, Coordinate b)
            {
                double ax = (a.Lon - longitude) * lonScale, ay = (a.Lat - latitude) * GeoMath.MetresPerDegree;
                double bx = (b.Lon - longitude) * lonScale, by = (b.Lat - latitude) * GeoMath.MetresPerDegree;
                double dx = bx - ax, dy = by - ay;
                var lengthSquared = dx * dx + dy * dy;
                var t = lengthSquared == 0 ? 0 : Math.Clamp(-(ax * dx + ay * dy) / lengthSquared, 0, 1);
                return Math.Sqrt(Math.Pow(ax + t * dx, 2) + Math.Pow(ay + t * dy, 2));
            }
        }

        /// <summary>Even-odd rule over all rings, so holes such as islands are left out.</summary>
        private static bool Contains(List<Coordinate[]> rings, Coordinate point)
        {
            var inside = false;
            foreach (var ring in rings)
            {
                for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                {
                    if ((ring[i].Lat > point.Lat) != (ring[j].Lat > point.Lat)
                        && point.Lon < (ring[j].Lon - ring[i].Lon) * (point.Lat - ring[i].Lat) / (ring[j].Lat - ring[i].Lat) + ring[i].Lon)
                        inside = !inside;
                }
            }
            return inside;
        }

        private static void Add(JsonElement geometry, List<Coordinate[]> lines, List<List<Coordinate[]>> polygons)
        {
            var type = geometry.GetProperty("type").GetString();
            if (type == "GeometryCollection")
            {
                foreach (var part in geometry.GetProperty("geometries").EnumerateArray())
                    Add(part, lines, polygons);
                return;
            }

            var coordinates = geometry.GetProperty("coordinates");
            switch (type)
            {
                case "Point":
                    lines.Add([ToCoordinate(coordinates)]);
                    break;
                case "MultiPoint":
                    lines.AddRange(coordinates.EnumerateArray().Select(c => new[] { ToCoordinate(c) }));
                    break;
                case "LineString":
                    lines.Add(ToLine(coordinates));
                    break;
                case "MultiLineString":
                    lines.AddRange(coordinates.EnumerateArray().Select(ToLine));
                    break;
                case "Polygon":
                    AddPolygon(coordinates, lines, polygons);
                    break;
                case "MultiPolygon":
                    foreach (var polygon in coordinates.EnumerateArray())
                        AddPolygon(polygon, lines, polygons);
                    break;
            }
        }

        private static void AddPolygon(JsonElement polygon, List<Coordinate[]> lines, List<List<Coordinate[]>> polygons)
        {
            var rings = polygon.EnumerateArray().Select(ToLine).ToList();
            polygons.Add(rings);
            lines.AddRange(rings);
        }

        private static Coordinate[] ToLine(JsonElement line) => line.EnumerateArray().Select(ToCoordinate).ToArray();

        private static Coordinate ToCoordinate(JsonElement position) => new(position[0].GetDouble(), position[1].GetDouble());
    }
}
