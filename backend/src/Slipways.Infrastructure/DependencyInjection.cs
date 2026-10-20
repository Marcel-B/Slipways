using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Gauges;
using Slipways.Infrastructure.Identity;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Shapes;

namespace Slipways.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Slipways")
            ?? throw new InvalidOperationException("Connection string 'Slipways' is not configured.");

        services.AddDbContext<SlipwaysDbContext>(options => options
            .UseSqlite(connectionString)
            .AddInterceptors(new SqliteUnicodeInterceptor()));

        services.AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<SlipwaysDbContext>();

        services.AddOptions<PegelonlineOptions>().Bind(configuration.GetSection(PegelonlineOptions.SectionName));
        services.AddHttpClient<PegelonlineClient>((sp, http) =>
        {
            http.BaseAddress = sp.GetRequiredService<IOptions<PegelonlineOptions>>().Value.BaseUrl;
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Slipways (+https://github.com/Marcel-B/Slipways)");
        });
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<GaugeSync>();
        services.AddHostedService<GaugeSyncWorker>();

        services.AddOptions<NominatimOptions>().Bind(configuration.GetSection(NominatimOptions.SectionName));
        services.AddHttpClient<NominatimClient>((sp, http) =>
        {
            http.BaseAddress = sp.GetRequiredService<IOptions<NominatimOptions>>().Value.BaseUrl;
            // Nominatim's usage policy asks for a user agent that identifies the application.
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Slipways (+https://github.com/Marcel-B/Slipways)");
        });
        services.AddScoped<ShapeSync>();
        services.AddHostedService<ShapeSyncWorker>();

        return services;
    }

    /// <summary>Applies pending migrations and seeds an empty database.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        await SeedData.SeedAsync(db, cancellationToken);
    }
}
