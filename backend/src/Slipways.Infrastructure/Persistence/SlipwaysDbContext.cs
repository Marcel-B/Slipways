using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
            e.PrimitiveCollection(x => x.OsmElements);
        });

        builder.Entity<Slipway>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Costs).HasPrecision(8, 2);
            e.HasOne(x => x.Water).WithMany().HasForeignKey(x => x.WaterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Marina).WithMany(x => x.Slipways).HasForeignKey(x => x.MarinaId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Extras).WithMany(x => x.Slipways).UsingEntity("SlipwayExtras");
        });

        builder.Entity<Marina>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Water).WithMany().HasForeignKey(x => x.WaterId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Extra>(e => e.Property(x => x.Name).HasMaxLength(100));

        builder.Entity<Manufacturer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasMany(x => x.Services).WithMany(x => x.Manufacturers).UsingEntity("ManufacturerServices");
        });

        builder.Entity<Service>(e => e.Property(x => x.Name).HasMaxLength(200));

        builder.Entity<Station>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.WaterLevelState).HasMaxLength(20);
            e.HasIndex(x => x.ExternalId).IsUnique();
            e.HasOne(x => x.Water).WithMany().HasForeignKey(x => x.WaterId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
