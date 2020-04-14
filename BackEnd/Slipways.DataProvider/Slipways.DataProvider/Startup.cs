using System;
using com.b_velop.Slipways.Data;
using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Slipways.DataProvider.Contracts;
using com.b_velop.Slipways.DataProvider.Infrastructure;
using com.b_velop.Slipways.DataProvider.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace com.b_velop.Slipways.DataProvider
{
    public class Startup
    {
        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddHostedService<BackUp>();
            services.AddHostedService<CacheLoader>();

            var secretProvider = new SecretProvider();

            var server = Environment.GetEnvironmentVariable("SERVER");
            var database = Environment.GetEnvironmentVariable("DATABASE");
            var user = Environment.GetEnvironmentVariable("USER");
            var port = Environment.GetEnvironmentVariable("PORT");

            var pw = secretProvider.GetSecret(server);

#if DEBUG
            var connectionString = $"Server=localhost,1433;Database=Slipways;User Id=sa;Password=foo123bar!";
#else
            var connectionString = $"Server={server},{port};Database={database};User Id={user};Password={pw}";
#endif
            Console.WriteLine(connectionString);
            services.AddSlipwaysData();

            services.AddScoped<ISecretProvider, SecretProvider>();
            services.AddScoped<IInitializer, Initializer>();

            services.AddDbContext<SlipwaysContext>(options =>
            {
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Slipways.DataProvider"));
            });

            services.AddControllers();
        }

        public void Configure(
            ILogger<Startup> logger,
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpMetrics();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
            InitializeDatabase(logger, app);
        }

        private void InitializeDatabase(
            ILogger<Startup> logger,
            IApplicationBuilder app)
        {
            logger.LogInformation($"Start Database Migrations");
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SlipwaysContext>();
            context.Database.Migrate();
            context.SaveChanges();
            logger.LogInformation($"Database Migrations done");
        }
    }
}
