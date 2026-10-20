using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Slipways.Domain;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Infrastructure.Osm;

public sealed record OsmImportCounts(int Added, int Updated, int Linked);

/// <param name="Missing">Elements we know, such as "node/123", that the import no longer found in OpenStreetMap.</param>
public sealed record OsmImportResult(OsmImportCounts Slipways, OsmImportCounts Marinas, IReadOnlyList<string> Missing);

/// <summary>
/// Imports slipways (<c>leisure=slipway</c>) and marinas (<c>leisure=marina</c>) from OpenStreetMap.
/// Places are matched by OpenStreetMap element. An element closer than <see cref="SamePlaceDistance"/> to a place
/// entered by hand only links that place to it; anything else becomes a new place with <see cref="Sources.Osm"/>.
/// Imported places take their fields from OpenStreetMap on every run; places entered by hand keep theirs, so their
/// data stays apart from the ODbL data. Elements that disappear from OpenStreetMap are only reported, never deleted.
/// </summary>
public sealed class OsmImport(SlipwaysDbContext db, OverpassClient client, IOptions<OsmOptions> options, TimeProvider time)
{
    /// <summary>Metres within which an element is taken to be a place we already have.</summary>
    public const double SamePlaceDistance = 50;

    /// <summary>Metres from a water's shape within which a place is put on that water.</summary>
    public const double WaterDistance = 300;

    private int _queries;

    public async Task<OsmImportResult> RunAsync(CancellationToken cancellationToken = default)
    {
        // Everything is fetched before anything changes: a failed query must not make places look deleted.
        var slipwayElements = await FetchAsync("slipway", cancellationToken);
        var marinaElements = await FetchAsync("marina", cancellationToken);

        var now = time.GetUtcNow().UtcDateTime;
        var shapes = await db.Waters.AsNoTracking()
            .Where(w => w.Shape != null)
            .Select(w => new { w.Id, w.Shape })
            .ToListAsync(cancellationToken);
        var waters = new WaterLocator(shapes.Select(w => (w.Id, w.Shape!)));
        var missing = new List<string>();

        var slipways = Import(await db.Slipways.ToListAsync(cancellationToken), slipwayElements,
            () => new Slipway { Name = "" }, ApplySlipway, waters, now, missing);
        var marinas = Import(await db.Marinas.ToListAsync(cancellationToken), marinaElements,
            () => new Marina { Name = "" }, ApplyMarina, waters, now, missing);

        // In the same save as the places, so the recorded success and the data never disagree.
        await OsmImportSchedule.MarkSuccessAsync(db, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new OsmImportResult(slipways, marinas, missing);
    }

    /// <summary>Elements of all countries, one query per country; an element on a border comes only once.</summary>
    private async Task<List<OsmPlace>> FetchAsync(string leisure, CancellationToken cancellationToken)
    {
        var places = new Dictionary<(string, long), OsmPlace>();
        foreach (var country in options.Value.CountriesToImport)
        {
            if (_queries++ > 0)
                await Task.Delay(options.Value.RequestSpacing, time, cancellationToken);

            foreach (var element in await client.GetElementsAsync(country, leisure, cancellationToken))
            {
                if (element is { Latitude: { } latitude, Longitude: { } longitude })
                    places.TryAdd((element.Type, element.Id), new OsmPlace(element, country, latitude, longitude));
            }
        }
        return places.Values.ToList();
    }

    private OsmImportCounts Import<T>(
        List<T> existing,
        List<OsmPlace> places,
        Func<T> create,
        Action<T, OsmPlace> apply,
        WaterLocator waters,
        DateTime now,
        List<string> missing) where T : Entity, IOsmPlace
    {
        var byElement = existing.Where(e => e.OsmId is not null).ToDictionary(e => (e.OsmType!, e.OsmId!.Value));
        var manual = existing.Where(e => e.Source == Sources.Manual).ToList();
        int added = 0, updated = 0, linked = 0;

        foreach (var place in places)
        {
            var key = (place.Element.Type, place.Element.Id);
            if (byElement.TryGetValue(key, out var match))
            {
                if (match.Source == Sources.Osm && Update(match, place))
                {
                    match.Updated = now;
                    updated++;
                }
                continue;
            }

            var nearby = manual
                .Select(m => (Place: m, Distance: GeoMath.Haversine(m.Latitude, m.Longitude, place.Latitude, place.Longitude)))
                .Where(m => m.Distance < SamePlaceDistance)
                .OrderBy(m => m.Distance)
                .Select(m => m.Place)
                .ToList();
            if (nearby.Count > 0)
            {
                // When every place nearby is linked already, the element is most likely the same slipway mapped
                // twice, e.g. as a node and as a way, so it is not added either.
                if (nearby.FirstOrDefault(m => m.OsmId is null) is { } free)
                {
                    free.OsmType = place.Element.Type;
                    free.OsmId = place.Element.Id;
                    free.Updated = now;
                    linked++;
                }
                continue;
            }

            var created = create();
            created.Id = Guid.NewGuid();
            created.Created = now;
            created.Source = Sources.Osm;
            created.OsmType = place.Element.Type;
            created.OsmId = place.Element.Id;
            Apply(created, place);
            db.Add(created);
            added++;
        }

        var found = places.Select(p => (p.Element.Type, p.Element.Id)).ToHashSet();
        missing.AddRange(byElement.Keys.Where(k => !found.Contains(k)).Select(k => $"{k.Item1}/{k.Item2}"));
        return new OsmImportCounts(added, updated, linked);

        void Apply(T entity, OsmPlace place)
        {
            apply(entity, place);
            entity.Latitude = place.Latitude;
            entity.Longitude = place.Longitude;
            // Shapes may arrive after the first import, so places without a water get another chance on every run.
            entity.WaterId ??= waters.Find(place.Latitude, place.Longitude, WaterDistance);
        }

        // Assigns everything and lets EF tell whether a value actually changed.
        bool Update(T entity, OsmPlace place)
        {
            Apply(entity, place);
            var entry = db.Entry(entity);
            entry.DetectChanges();
            return entry.State == EntityState.Modified;
        }
    }

    private static void ApplySlipway(Slipway slipway, OsmPlace place)
    {
        slipway.Name = place.Name("Slipanlage");
        slipway.Street = place.Street;
        slipway.PostalCode = place.Element.Tag("addr:postcode");
        slipway.City = place.Element.Tag("addr:city");
        slipway.Country = place.Element.Tag("addr:country") ?? place.Country;
        slipway.Url = place.Website;
        slipway.Operator = place.Element.Tag("operator");
        slipway.Fee = place.Element.Tag("fee");
        slipway.Access = place.Element.Tag("access");
        slipway.OpeningHours = place.Element.Tag("opening_hours");
        slipway.Surface = place.Element.Tag("surface");
    }

    private static void ApplyMarina(Marina marina, OsmPlace place)
    {
        marina.Name = place.Name("Marina");
        marina.Street = place.Street;
        marina.PostalCode = place.Element.Tag("addr:postcode");
        marina.City = place.Element.Tag("addr:city");
        marina.Url = place.Website;
        marina.Operator = place.Element.Tag("operator");
        marina.Fee = place.Element.Tag("fee");
        marina.Access = place.Element.Tag("access");
        marina.OpeningHours = place.Element.Tag("opening_hours");
    }

    /// <param name="Country">The country it was found in, as an ISO 3166-1 code.</param>
    private sealed record OsmPlace(OverpassElement Element, string Country, double Latitude, double Longitude)
    {
        /// <summary>Many slipways have no name in OpenStreetMap; they are called after their kind and town.</summary>
        public string Name(string kind) =>
            Element.Tag("name") ?? (Element.Tag("addr:city") is { } city ? $"{kind} {city}" : kind);

        public string? Street => Element.Tag("addr:street") is { } street
            ? Element.Tag("addr:housenumber") is { } number ? $"{street} {number}" : street
            : null;

        public string? Website => Element.Tag("website") ?? Element.Tag("contact:website");
    }
}
