using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Gauges;
using Slipways.Infrastructure.Identity;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Osm;
using Slipways.Infrastructure.Persistence;
using Slipways.Infrastructure.Shapes;
using Slipways.Infrastructure.Suggestions;
using Slipways.Infrastructure.Websites;

namespace Slipways.Infrastructure;

public static class DependencyInjection
{
    // PEGELONLINE, Nominatim and Overpass all ask for a user agent that identifies the application. Its first word is
    // also what robots.txt files name to address us (RobotsTxt.ProductToken).
    private const string UserAgent = "Slipways (+https://github.com/Marcel-B/Slipways)";

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
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<GaugeSync>();
        services.AddHostedService<GaugeSyncWorker>();

        services.AddOptions<RijkswaterstaatOptions>().Bind(configuration.GetSection(RijkswaterstaatOptions.SectionName));
        services.AddHttpClient<RijkswaterstaatClient>((sp, http) =>
        {
            http.BaseAddress = sp.GetRequiredService<IOptions<RijkswaterstaatOptions>>().Value.BaseUrl;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.AddScoped<RijkswaterstaatSync>();
        services.AddHostedService<RijkswaterstaatWorker>();

        services.AddOptions<NominatimOptions>().Bind(configuration.GetSection(NominatimOptions.SectionName));
        services.AddHttpClient<NominatimClient>((sp, http) =>
        {
            http.BaseAddress = sp.GetRequiredService<IOptions<NominatimOptions>>().Value.BaseUrl;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.AddScoped<ShapeSync>();
        services.AddHostedService<ShapeSyncWorker>();

        services.AddOptions<OsmOptions>().Bind(configuration.GetSection(OsmOptions.SectionName));
        services.AddHttpClient<OverpassClient>((sp, http) =>
            {
                var osm = sp.GetRequiredService<IOptions<OsmOptions>>().Value;
                http.BaseAddress = osm.BaseUrl;
                // Overpass stops a query after QueryTimeout; give the answer a little longer to arrive.
                http.Timeout = osm.QueryTimeout + TimeSpan.FromMinutes(1);
                http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            // A country's slipways are a few MB of JSON; compressed they are a fraction of that.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        services.AddScoped<OsmImport>();
        services.AddHostedService<OsmImportWorker>();

        services.AddOptions<WebsiteOptions>().Bind(configuration.GetSection(WebsiteOptions.SectionName));
        services.AddHttpClient<WebsiteClient>(http =>
            {
                // The client cancels each request itself after WebsiteOptions.Timeout.
                http.Timeout = Timeout.InfiniteTimeSpan;
                http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.1");
                http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de,en;q=0.5");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Redirects are followed by the client, which checks robots.txt for every host on the way.
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                ConnectCallback = PublicAddressGuard.ConnectAsync,
                // A proxy would make the guard check the proxy instead of the site.
                UseProxy = false,
            })
            // Every request would be logged with its address; the job logs a summary per run instead.
            .RemoveAllLoggers();
        services.AddScoped<WebsiteSync>();
        services.AddHostedService<WebsiteSyncWorker>();

        services.AddOptions<SuggestionOptions>().Bind(configuration.GetSection(SuggestionOptions.SectionName));
        services.AddSingleton<ProofOfWork>();
        services.AddScoped<SuggestionService>();

        services.AddOptions<MonitoringOptions>().Bind(configuration.GetSection(MonitoringOptions.SectionName));
        services.AddHostedService<CatalogMetricsWorker>();
        if (configuration[$"{MonitoringOptions.SectionName}:Loki:Url"] is { Length: > 0 } lokiUrl)
        {
            services.AddSingleton<LokiLogQueue>();
            services.AddSingleton<ILoggerProvider, LokiLoggerProvider>();
            services.AddHttpClient(LokiShipper.HttpClientName, http =>
                {
                    http.BaseAddress = new Uri(lokiUrl);
                    http.Timeout = TimeSpan.FromSeconds(10);
                    http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                })
                .RemoveAllLoggers();
            services.AddHostedService<LokiShipper>();
        }

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
