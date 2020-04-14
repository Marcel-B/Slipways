using System;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Data;
using com.b_velop.Slipways.Web.Infrastructure;
using com.b_velop.Slipways.Web.Services;
using com.b_velop.Slipways.Web.ViewModels;
using GraphQL.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoMapper;
using NLog.Web;
using Prometheus;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.b_velop.Slipways.Web
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Env = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddHostedService<CacheLoader>();

            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var sendGridUser = Environment.GetEnvironmentVariable("SEND_GRID_USER");
            var graphQLEndpoint = Environment.GetEnvironmentVariable("GRAPH_QL_ENDPOINT");
            var apiEndpoint = Environment.GetEnvironmentVariable("API_ENDPOINT");
            var apiPort = Environment.GetEnvironmentVariable("API_PORT");

            var secretProvider = new SecretProvider();
            var key = secretProvider.GetSecret("send_grid_key");

            if (Env.IsDevelopment())
            {
                key = Environment.GetEnvironmentVariable("SEND_GRID_KEY");
            }

            services.AddTransient<IEmailSender, EmailSender>();

            services.AddTransient(_ => new AuthMessageSenderOptions
            {
                SendGridKey = key,
                SendGridUser = sendGridUser
            });

            services.AddAutoMapper(typeof(Program).Assembly);

            services.AddScoped(_ => new GraphQLClient(graphQLEndpoint));
            services.AddScoped<ISecretProvider, SecretProvider>();
            services.AddScoped<IStoreWrapper, StoreWrapper>();
            services.AddScoped<ISlipwayStore, SlipwayStore>();
            services.AddScoped<IExtraStore, ExtraStore>();
            services.AddScoped<IManufacturerStore, ManufacturerStore>();
            services.AddScoped<IWaterStore, WaterStore>();
            services.AddScoped<IServiceStore, ServiceStore>();
            services.AddScoped<IPortStore, PortStore>();
            services.AddScoped<WaterViewModel>();
            services.AddScoped<IGraphQLService, GraphQLService>();

            services.AddTransient(_ => new ApplicationInfo { ApiEndpoint = $"{apiEndpoint}:{apiPort}", GraphQlEndpoint = graphQLEndpoint });

            services.AddHttpClient<ISlipwayService, SlipwayService>("slipwayClient", options =>
            {
                options.BaseAddress = new Uri($"{apiEndpoint}:{apiPort}");
            });
            services.AddHttpClient<IServiceService, ServiceService>("serviceClient", options =>
            {
                options.BaseAddress = new Uri($"{apiEndpoint}:{apiPort}");
            });
            services.AddHttpClient<IExtraService, ExtraService>("extraClient", options =>
            {
                options.BaseAddress = new Uri($"{apiEndpoint}:{apiPort}");
            });
            services.AddHttpClient<IWaterService, WaterService>("waterClient", options =>
            {
                options.BaseAddress = new Uri($"{apiEndpoint}:{apiPort}");
            });
            services.AddHttpClient<IManufacturerService, ManufacturerService>("manufacturerClient", options =>
            {
                options.BaseAddress = new Uri($"{apiEndpoint}:{apiPort}");
            });
            services.AddHttpClient<IPortService, PortService>("portClient", options =>
            {
                options.BaseAddress = new Uri($"{apiEndpoint}:{apiPort}");
            });

            if (!Env.IsDevelopment())
                services.AddDataProtection()
                .SetApplicationName("slipways-web")
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"/app/keys/"));

            services.AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeFolder("/Slipways");
                    if (!Env.IsDevelopment())
                    {
                        options.Conventions.AuthorizeAreaFolder("Admin", "/Slipway");
                        options.Conventions.AuthorizeAreaFolder("Admin", "/Water");
                        options.Conventions.AuthorizeAreaFolder("Admin", "/Service");
                        options.Conventions.AuthorizeAreaFolder("Admin", "/Port");
                    }
                }).AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss";
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            var password = string.Empty;

            if (Env.IsProduction())
                password = secretProvider.GetSecret("sqlserver");
            else if (Env.IsStaging())
                password = secretProvider.GetSecret("dev_slipway_db");
            else
                password = "foo123bar!";

            var server = Environment.GetEnvironmentVariable("SERVER");
            var user = Environment.GetEnvironmentVariable("USER");
            var db = Environment.GetEnvironmentVariable("DATABASE");
            var port = Environment.GetEnvironmentVariable("PORT");

            var str = $"Server={server},{port};Database={db};User Id={user};Password={password}";
            services.AddDbContext<ApplicationDbContext>(_ => _.UseSqlServer(str));
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            UpdateDatabase(app);
            app.UseForwardedHeaders();
            app.UseHttpMetrics(options =>
            {
                options.RequestCount.Enabled = false;
                options.RequestDuration.Histogram = Metrics.CreateHistogram("slipways_web_http_request_duration_seconds", "Some help text",
                    new HistogramConfiguration
                    {
                        Buckets = Histogram.LinearBuckets(start: 1, width: 1, count: 64),
                        LabelNames = new[] { "code", "method" }
                    });
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                //app.UseHsts();
            }
            app.UseCookiePolicy();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapMetrics();
            });
        }

        private void UpdateDatabase(
            IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                var file = "dev-nlog.config";

                if (Env.IsProduction())
                    file = "nlog.config";

                var logger = NLogBuilder.ConfigureNLog(file).GetCurrentClassLogger();
                logger.Fatal(e, $"Error occurred while migrating Database", 6666);
            }
        }
    }
}
