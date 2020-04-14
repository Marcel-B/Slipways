using com.b_velop.Slipways.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace com.b_velop.Slipways.Data
{
    public class SlipwaysContext : DbContext
    {
        public DbSet<Water> Waters { get; set; }
        public DbSet<Slipway> Slipways { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<ManufacturerService> ManufacturerServices { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<Extra> Extras { get; set; }
        public DbSet<SlipwayExtra> SlipwayExtras { get; set; }

        public SlipwaysContext(
            DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(
              ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<SlipwayExtra>()
                .HasOne(_ => _.Slipway)
                .WithMany(_ => _.SlipwayExtras)
                .HasForeignKey(_ => _.SlipwayFk)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<SlipwayExtra>()
                .HasOne(_ => _.Extra)
                .WithMany(_ => _.SlipwayExtras)
                .HasForeignKey(_ => _.ExtraFk)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
