using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Slipways.Infrastructure;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

/// <summary>Runs the API against an in-memory SQLite database seeded with the real seed data.</summary>
public sealed class SlipwaysApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:InitializeOnStartup", "false");
        builder.UseSetting("Pegelonline:Enabled", "false");
        builder.UseSetting("Rijkswaterstaat:Enabled", "false");
        builder.UseSetting("Nominatim:Enabled", "false");
        builder.UseSetting("Osm:Enabled", "false");
        builder.UseSetting("Websites:Enabled", "false");
        builder.UseSetting("Monitoring:CatalogMetricsEnabled", "false");
        builder.UseSetting("RateLimit:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SlipwaysDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SlipwaysDbContext>>();
            services.AddDbContext<SlipwaysDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        SqliteUnicodeInterceptor.Register(_connection);
        await Services.InitializeDatabaseAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
