#:property PublishAot=false

// Suggests OpenStreetMap elements for waters that have no entry in Seed/waterShapes.json yet.
//
//   dotnet run tools/suggest-water-shapes.cs -- http://localhost:5080 [suggestions.json]
//
// Reads the waters and what lies on them from a running API, searches Nominatim for each name and keeps the
// candidates whose shape runs past our slipways, gauges and marinas. Prints a report and writes the
// suggestions in the format of waterShapes.json; review them before copying them over.
// Big international rivers have multilingual names Nominatim does not find ("Donau" is "Danube, Србија"); their
// relation id is on Wikidata (property P402, "OpenStreetMap relation ID").
// Nominatim allows one request per second, so this takes a few minutes.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

const double NearKm = 1.5;
const string MappingPath = "src/Slipways.Infrastructure/Seed/waterShapes.json";
string[] waterTypes = ["water", "bay", "strait", "reservoir", "basin", "lagoon"];

var api = new Uri(args.Length > 0 ? args[0] : "http://localhost:5080");
var outputPath = args.Length > 1 ? args[1] : "suggestions.json";

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("Slipways (+https://github.com/Marcel-B/Slipways)");

var known = File.Exists(MappingPath)
    ? JsonNode.Parse(File.ReadAllText(MappingPath))!.AsArray()
        .Select(e => (string)e!["shortname"]!).ToHashSet(StringComparer.OrdinalIgnoreCase)
    : [];

var waters = JsonNode.Parse(await http.GetStringAsync(new Uri(api, "/api/waters")))!.AsArray();
var suggestions = new JsonArray();
var lastRequest = DateTime.MinValue;

foreach (var water in waters)
{
    var shortname = (string)water!["shortname"]!;
    var name = (string)water["name"]!;
    if (known.Contains(shortname))
        continue;

    var details = JsonNode.Parse(await http.GetStringAsync(new Uri(api, $"/api/waters/{water["id"]}")))!;
    var places = new[] { "slipways", "stations", "marinas" }
        .SelectMany(key => details[key]!.AsArray())
        .Select(p => new Point((double)p!["latitude"]!, (double)p["longitude"]!))
        .ToList();

    var candidates = new Dictionary<string, Candidate>();
    foreach (var query in Queries(name, shortname))
    {
        foreach (var feature in await Search(query, details["bounds"]))
        {
            var props = feature!["properties"]!;
            var category = (string)props["category"]!;
            var type = (string)props["type"]!;
            if (category is not ("waterway" or "water") && !waterTypes.Contains(type))
                continue;

            var osm = $"{props["osm_type"]}/{props["osm_id"]}";
            var shape = Shape.From(feature["geometry"]!);
            var near = places.Select((p, i) => (p, i)).Where(x => shape.DistanceKm(x.p) <= NearKm).Select(x => x.i).ToHashSet();
            candidates.TryAdd(osm, new Candidate(osm, (string?)props["display_name"] ?? osm, $"{category}={type}", near));
        }
    }

    // Rivers without a waterway relation come in pieces, so take as many pieces as it needs to reach our places.
    var chosen = new List<Candidate>();
    var covered = new HashSet<int>();
    while (chosen.Count < 5)
    {
        var next = candidates.Values.Except(chosen).MaxBy(c => c.Near.Count(i => !covered.Contains(i)));
        if (next is null || (places.Count > 0 && next.Near.All(covered.Contains)))
            break;
        chosen.Add(next);
        covered.UnionWith(next.Near);
        if (places.Count == 0)
            break;
    }

    var coverage = places.Count == 0 ? (double?)null : (double)covered.Count / places.Count;
    var verdict = chosen.Count == 0 ? "keine" : coverage is >= 0.8 ? "gut" : "prüfen";
    Console.WriteLine($"{verdict,-7} {name,-36} {Percent(coverage),5} von {places.Count,3}  {string.Join(", ", chosen.Select(c => $"{c.Osm} ({c.Kind}, {Shorten(c.Label)})"))}");

    suggestions.Add(new JsonObject
    {
        ["shortname"] = shortname,
        ["name"] = name,
        ["osm"] = new JsonArray(chosen.Select(c => (JsonNode)c.Osm).ToArray()),
        ["verdict"] = verdict,
        ["coverage"] = coverage,
        ["places"] = places.Count,
        ["candidates"] = new JsonArray(candidates.Values.OrderByDescending(c => c.Near.Count)
            .Select(c => (JsonNode)$"{c.Osm} {c.Kind} {c.Near.Count}/{places.Count} {c.Label}").ToArray()),
    });
}

File.WriteAllText(outputPath, suggestions.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{suggestions.Count} Vorschläge in {outputPath}");

static string Percent(double? value) => value is { } v ? v.ToString("P0", CultureInfo.GetCultureInfo("de-DE")) : "–";

static string Shorten(string label) => label.Split(',')[0];

static IEnumerable<string> Queries(string name, string shortname)
{
    var queries = new List<string> { name };
    var spelled = name.Replace("strasse", "straße").Replace("Gewaesser", "Gewässer");
    if (spelled != name)
        queries.Add(spelled);
    // Abbreviations like "RHK" find nothing, but some short names are the German name ("MOLDAU" for the Vltava).
    if (shortname.Length > 4 && !shortname.Equals(name, StringComparison.OrdinalIgnoreCase))
        queries.Add(SeedTitle(shortname));
    return queries.Distinct();
}

static string SeedTitle(string value) =>
    CultureInfo.GetCultureInfo("de-DE").TextInfo.ToTitleCase(value.Replace('_', ' ').ToLowerInvariant());

async Task<JsonArray> Search(string query, JsonNode? bounds)
{
    var url = "https://nominatim.openstreetmap.org/search?format=geojson&polygon_geojson=1&polygon_threshold=0.001"
        + $"&limit=20&layer=natural&q={Uri.EscapeDataString(query)}";
    url += bounds is null
        ? "&countrycodes=de,nl"
        // Prefers results around our places without excluding the rest of a long river.
        : FormattableString.Invariant(
            $"&viewbox={(double)bounds["west"]! - 0.5},{(double)bounds["north"]! + 0.5},{(double)bounds["east"]! + 0.5},{(double)bounds["south"]! - 0.5}");

    var wait = lastRequest.AddSeconds(1.1) - DateTime.UtcNow;
    if (wait > TimeSpan.Zero)
        await Task.Delay(wait);
    lastRequest = DateTime.UtcNow;

    var result = JsonNode.Parse(await http.GetStringAsync(url))!;
    return result["features"]!.AsArray();
}

record Point(double Latitude, double Longitude);

record Candidate(string Osm, string Label, string Kind, HashSet<int> Near);

/// <summary>Lines and polygon rings of a GeoJSON geometry, flattened to measure distances on a local plane.</summary>
sealed class Shape
{
    private readonly List<Point[]> _lines = [];
    private readonly List<Point[]> _areas = [];

    public static Shape From(JsonNode geometry)
    {
        var shape = new Shape();
        shape.Add(geometry);
        return shape;
    }

    private void Add(JsonNode geometry)
    {
        var coordinates = geometry["coordinates"];
        switch ((string)geometry["type"]!)
        {
            case "Point":
                _lines.Add([ToPoint(coordinates!)]);
                break;
            case "LineString":
                _lines.Add(ToLine(coordinates!));
                break;
            case "MultiLineString":
                _lines.AddRange(coordinates!.AsArray().Select(l => ToLine(l!)));
                break;
            case "Polygon":
                AddPolygon(coordinates!);
                break;
            case "MultiPolygon":
                foreach (var polygon in coordinates!.AsArray())
                    AddPolygon(polygon!);
                break;
            case "GeometryCollection":
                foreach (var part in geometry["geometries"]!.AsArray())
                    Add(part!);
                break;
        }
    }

    private void AddPolygon(JsonNode rings)
    {
        var all = rings.AsArray().Select(r => ToLine(r!)).ToList();
        _lines.AddRange(all);
        _areas.Add(all[0]);
    }

    private static Point ToPoint(JsonNode c) => new((double)c[1]!, (double)c[0]!);

    private static Point[] ToLine(JsonNode line) => line.AsArray().Select(c => ToPoint(c!)).ToArray();

    public double DistanceKm(Point p)
    {
        if (_areas.Any(ring => Contains(ring, p)))
            return 0;

        var best = double.MaxValue;
        foreach (var line in _lines)
        {
            if (line.Length == 1)
                best = Math.Min(best, Km(p, line[0], line[0]));
            for (var i = 1; i < line.Length; i++)
                best = Math.Min(best, Km(p, line[i - 1], line[i]));
        }
        return best;
    }

    private static bool Contains(Point[] ring, Point p)
    {
        var inside = false;
        for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
        {
            if ((ring[i].Latitude > p.Latitude) != (ring[j].Latitude > p.Latitude) &&
                p.Longitude < (ring[j].Longitude - ring[i].Longitude) * (p.Latitude - ring[i].Latitude)
                    / (ring[j].Latitude - ring[i].Latitude) + ring[i].Longitude)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>Distance from p to the segment a–b, good enough at the scale of a few kilometres.</summary>
    private static double Km(Point p, Point a, Point b)
    {
        var scale = Math.Cos(p.Latitude * Math.PI / 180) * 111.32;
        double X(Point q) => q.Longitude * scale;
        double Y(Point q) => q.Latitude * 110.57;

        double dx = X(b) - X(a), dy = Y(b) - Y(a);
        var t = dx == 0 && dy == 0 ? 0 : Math.Clamp(((X(p) - X(a)) * dx + (Y(p) - Y(a)) * dy) / (dx * dx + dy * dy), 0, 1);
        return Math.Sqrt(Math.Pow(X(p) - X(a) - t * dx, 2) + Math.Pow(Y(p) - Y(a) - t * dy, 2));
    }
}
