using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Slipways.Domain;

namespace Slipways.Infrastructure.Persistence;

/// <summary>
/// Loads the initial data set (taken over from the original Slipways database) into an empty database.
/// </summary>
public static class SeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task SeedAsync(SlipwaysDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Waters.AnyAsync(cancellationToken))
            return;

        var waters = Load<WaterSeed>("waters").Select(w => new Water
        {
            Id = w.Id,
            Created = w.Created,
            Updated = w.Updated,
            Name = ToTitleCase(w.Longname),
            Shortname = w.Shortname,
        }).ToList();
        db.Waters.AddRange(waters);

        var extras = Load<ExtraSeed>("extras").Select(e => new Extra
        {
            Id = e.Id,
            Created = e.Created,
            Updated = e.Updated,
            Name = e.Name,
        }).ToDictionary(e => e.Id);
        db.Extras.AddRange(extras.Values);

        db.Marinas.AddRange(Load<MarinaSeed>("marinas").Select(m => new Marina
        {
            Id = m.Id,
            Created = m.Created,
            Updated = m.Updated,
            Name = m.Name,
            Latitude = m.Latitude,
            Longitude = m.Longitude,
            Url = m.Url,
            Street = m.Street?.Trim(),
            PostalCode = m.Postalcode?.Trim(),
            City = m.City?.Trim(),
            Phone = m.Phone,
            Email = m.Email,
            WaterId = m.WaterFk,
        }));

        var extrasBySlipway = Load<SlipwayExtraSeed>("slipwayExtra").ToLookup(x => x.SlipwayFk, x => x.ExtraFk);
        db.Slipways.AddRange(Load<SlipwaySeed>("slipways").Select(s => new Slipway
        {
            Id = s.Id,
            Created = s.Created,
            Updated = s.Updated,
            Name = s.Name,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Street = s.Street?.Trim(),
            PostalCode = s.Postalcode?.Trim(),
            City = s.City?.Trim(),
            Country = s.Country,
            Comment = s.Comment,
            Rating = s.Rating,
            // The original data uses -1 for "unknown".
            Costs = s.Costs < 0 ? null : s.Costs,
            Pro = s.Pro,
            Contra = s.Contra,
            WaterId = s.WaterFk,
            MarinaId = s.PortFk,
            Extras = extrasBySlipway[s.Id].Distinct().Select(id => extras[id]).ToList(),
        }));

        var manufacturers = Manufacturers.ToDictionary(m => m.Id);
        db.Manufacturers.AddRange(manufacturers.Values);

        var manufacturersByService = Load<ManufacturerServiceSeed>("manufacturerService")
            .ToLookup(x => x.ServiceFk, x => x.ManufacturerFk);
        db.Services.AddRange(Load<ServiceSeed>("services").Select(s => new Service
        {
            Id = s.Id,
            Created = s.Created,
            Updated = s.Updated,
            Name = s.Name,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Url = s.Url,
            Street = s.Street?.Trim(),
            PostalCode = s.Postalcode?.Trim(),
            City = s.City?.Trim(),
            Phone = s.Phone,
            Email = s.Email,
            Manufacturers = manufacturersByService[s.Id].Distinct().Select(id => manufacturers[id]).ToList(),
        }));

        db.Stations.AddRange(Load<StationSeed>("stations").Select(s => new Station
        {
            Id = s.Id,
            Created = s.Created,
            Updated = s.Updated,
            Name = s.Longname ?? s.Shortname ?? s.Number ?? string.Empty,
            Number = s.Number,
            Shortname = s.Shortname,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Km = s.Km,
            Agency = s.Agency,
            WaterId = s.WaterFk,
        }));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static readonly DateTime ManufacturersCreated = new(2019, 12, 16, 0, 0, 0, DateTimeKind.Utc);

    private static IEnumerable<Manufacturer> Manufacturers =>
    [
        Manufacturer("0FD675D0-867D-41EA-B14F-D1B2C264DAC7", "Quicksilver"),
        Manufacturer("102A6D4B-BD31-4428-BC16-E6991A1A9A1D", "Johnson"),
        Manufacturer("8BAA7BF4-CB1D-4D5E-B3C8-EF190DAC1C3C", "Suzuki"),
        Manufacturer("45A1882C-4822-4CD1-9621-CEBDA51BFA05", "Mercury"),
        Manufacturer("E2931187-E433-4A36-8A0A-93900564A741", "Volvo Penta"),
        Manufacturer("9F36C0C2-F1EE-46FE-BEF0-77F6E913BF41", "Tohatsu"),
        Manufacturer("75EE4DD3-5933-4904-9020-5836EC757F02", "Honda"),
        Manufacturer("7A73006A-6825-43A3-A894-57BD588529B3", "Verado"),
        Manufacturer("D07683A9-2FFA-4763-A047-31214E8EB9C9", "Yamaha"),
    ];

    private static Manufacturer Manufacturer(string id, string name) =>
        new() { Id = Guid.Parse(id), Created = ManufacturersCreated, Name = name };

    internal static string ToTitleCase(string value) =>
        CultureInfo.GetCultureInfo("de-DE").TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());

    private static List<T> Load<T>(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().Single(r => r.EndsWith($".Seed.{name}.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions)!;
    }

    private abstract record SeedBase(Guid Id, DateTime Created, DateTime? Updated);

    private sealed record WaterSeed(Guid Id, DateTime Created, DateTime? Updated, string Longname, string? Shortname)
        : SeedBase(Id, Created, Updated);

    private sealed record ExtraSeed(Guid Id, DateTime Created, DateTime? Updated, string Name)
        : SeedBase(Id, Created, Updated);

    private sealed record MarinaSeed(
        Guid Id, DateTime Created, DateTime? Updated, string Name, double Latitude, double Longitude,
        string? Url, string? Street, string? Postalcode, string? City, string? Phone, string? Email, Guid WaterFk)
        : SeedBase(Id, Created, Updated);

    private sealed record SlipwaySeed(
        Guid Id, DateTime Created, DateTime? Updated, string Name, double Latitude, double Longitude,
        string? Street, string? Postalcode, string? City, string? Country, string? Comment, int Rating,
        decimal Costs, string? Pro, string? Contra, Guid WaterFk, Guid? PortFk)
        : SeedBase(Id, Created, Updated);

    private sealed record SlipwayExtraSeed(Guid SlipwayFk, Guid ExtraFk);

    private sealed record ServiceSeed(
        Guid Id, DateTime Created, DateTime? Updated, string Name, double Latitude, double Longitude,
        string? Url, string? Street, string? Postalcode, string? City, string? Phone, string? Email)
        : SeedBase(Id, Created, Updated);

    private sealed record ManufacturerServiceSeed(Guid ServiceFk, Guid ManufacturerFk);

    private sealed record StationSeed(
        Guid Id, DateTime Created, DateTime? Updated, string? Longname, string? Shortname, string? Number,
        double Latitude, double Longitude, double Km, string? Agency, Guid WaterFk)
        : SeedBase(Id, Created, Updated);
}
