using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Slipways.Domain;
using Slipways.Infrastructure.Identity;

namespace Slipways.Infrastructure.Persistence;

public class SlipwaysDbContext(DbContextOptions<SlipwaysDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Water> Waters => Set<Water>();
    public DbSet<Slipway> Slipways => Set<Slipway>();
    public DbSet<Marina> Marinas => Set<Marina>();
    public DbSet<Extra> Extras => Set<Extra>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<JobState> JobStates => Set<JobState>();
    public DbSet<PlaceInfo> PlaceInfos => Set<PlaceInfo>();
    public DbSet<WebsiteCheck> WebsiteChecks => Set<WebsiteCheck>();
    public DbSet<Suggestion> Suggestions => Set<Suggestion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite stores DateTime as text without a kind; every timestamp in this database is UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Water>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Shortname).HasMaxLength(100);
            e.Property(x => x.Source).HasMaxLength(10).HasDefaultValue(Sources.Manual);
            e.PrimitiveCollection(x => x.OsmElements);
        });

        builder.Entity<Slipway>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Costs).HasPrecision(8, 2);
            ConfigureSource(e);
            e.HasOne(x => x.Water).WithMany().HasForeignKey(x => x.WaterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Marina).WithMany(x => x.Slipways).HasForeignKey(x => x.MarinaId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Extras).WithMany(x => x.Slipways).UsingEntity("SlipwayExtras");
        });

        builder.Entity<Marina>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            ConfigureSource(e);
            e.HasOne(x => x.Water).WithMany().HasForeignKey(x => x.WaterId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Extra>(e => e.Property(x => x.Name).HasMaxLength(100));

        builder.Entity<Manufacturer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasMany(x => x.Services).WithMany(x => x.Manufacturers).UsingEntity("ManufacturerServices");
        });

        builder.Entity<Service>(e => e.Property(x => x.Name).HasMaxLength(200));

        builder.Entity<JobState>(e =>
        {
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(50);
        });

        // Not tied to the places with foreign keys, since a row can belong to a slipway or a marina.
        builder.Entity<PlaceInfo>(e =>
        {
            e.Property(x => x.PlaceType).HasMaxLength(10);
            e.Property(x => x.OsmType).HasMaxLength(10);
            e.Property(x => x.Field).HasMaxLength(50);
            e.Property(x => x.Value).HasMaxLength(500);
            e.Property(x => x.BaseValue).HasMaxLength(500);
            e.Property(x => x.Source).HasMaxLength(20);
            e.Property(x => x.SourceUrl).HasMaxLength(2000);
            e.HasIndex(x => new { x.PlaceType, x.PlaceId });
        });

        builder.Entity<WebsiteCheck>(e =>
        {
            e.HasKey(x => new { x.PlaceType, x.PlaceId });
            e.Property(x => x.PlaceType).HasMaxLength(10);
            e.Property(x => x.Url).HasMaxLength(2000);
            e.Property(x => x.Result).HasMaxLength(20);
        });

        builder.Entity<Suggestion>(e =>
        {
            e.Property(x => x.Kind).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(10);
            e.Property(x => x.PlaceType).HasMaxLength(10);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Fields).HasMaxLength(4000);
            e.Property(x => x.Text).HasMaxLength(1000);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.Status);
        });

        builder.Entity<Station>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.WaterLevelState).HasMaxLength(20);
            e.HasIndex(x => x.ExternalId).IsUnique();
            e.Property(x => x.ExternalCode).HasMaxLength(50);
            e.HasIndex(x => x.ExternalCode).IsUnique();
            e.HasOne(x => x.Water).WithMany().HasForeignKey(x => x.WaterId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSource<T>(EntityTypeBuilder<T> e) where T : class, IOsmPlace
    {
        // Rows from before the OpenStreetMap import were all entered by hand.
        e.Property(x => x.Source).HasMaxLength(10).HasDefaultValue(Sources.Manual);
        e.Property(x => x.OsmType).HasMaxLength(10);
        e.Property(x => x.OsmCountry).HasMaxLength(2);
        e.HasIndex(x => new { x.OsmType, x.OsmId }).IsUnique();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
