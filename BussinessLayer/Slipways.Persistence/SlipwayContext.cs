using com.b_velop.Slipways.Domain.Identity;
using com.b_velop.Slipways.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace com.b_velop.Slipways.Persistence
{
    public class SlipwaysContext : IdentityDbContext<AppUser, Role, string, IdentityUserClaim<string>, UserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public DbSet<Water> Waters { get; set; }
        public DbSet<Slipway> Slipways { get; set; }
        public DbSet<Marina> Marinas { get; set; }
        public DbSet<Extra> Extras { get; set; }
        public DbSet<SlipwayExtra> SlipwayExtras { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ManufacturerService> ManufacturerServices { get; set; }
        public DbSet<Station> Stations { get; set; }
        
        public SlipwaysContext(
            DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(
              ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });
                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                     .WithMany(r => r.UserRoles)
                     .HasForeignKey(ur => ur.UserId)
                     .IsRequired();
            });

            modelBuilder
                .Entity<SlipwayExtra>(x => x.HasKey(key => new { key.SlipwayId, key.ExtraId }));

            modelBuilder.Entity<SlipwayExtra>()
                .HasOne(_ => _.Slipway)
                .WithMany(_ => _.SlipwayExtras)
                .HasForeignKey(_ => _.SlipwayId);

            modelBuilder
                .Entity<SlipwayExtra>()
                .HasOne(_ => _.Extra)
                .WithMany(_ => _.SlipwayExtras)
                .HasForeignKey(_ => _.ExtraId);

            modelBuilder
                .Entity<ManufacturerService>(x => x.HasKey(key => new { key.ManufacturerId, key.ServiceId }));

            modelBuilder.Entity<ManufacturerService>()
                .HasOne(_ => _.Service)
                .WithMany(_ => _.ManufacturerServices)
                .HasForeignKey(_ => _.ServiceId);

            modelBuilder
                .Entity<ManufacturerService>()
                .HasOne(_ => _.Manufacturer)
                .WithMany(_ => _.ManufacturerServices)
                .HasForeignKey(_ => _.ManufacturerId);
        }
    }
}
