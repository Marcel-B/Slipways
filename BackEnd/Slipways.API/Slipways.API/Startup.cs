using System;
using System.Net;
using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Utilities.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using com.b_velop.Utilities.Docker;

namespace Slipways.API
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public ILogger<Startup> Logger { get; set; }
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            var secretProvider = new SecretProvider();
            services.AddCors();

            var port = Environment.GetEnvironmentVariable("PORT");
            var server = Environment.GetEnvironmentVariable("SERVER");
            var user = Environment.GetEnvironmentVariable("USER");
            var database = Environment.GetEnvironmentVariable("DATABASE");

            var password = string.Empty;

            if (WebHostEnvironment.IsStaging())
            {
                password = secretProvider.GetSecret("dev_slipway_db");
            }
            else if (WebHostEnvironment.IsProduction())
            {
                password = secretProvider.GetSecret("sqlserver");
            }
            else
            {
                password = "foo123bar!";
            }
            var connectionString = $"Server={server},{port};Database={database};User Id={user};Password={password}";
#if DEBUG
            connectionString = $"Server=localhost,1433;Database=Slipways;User Id=sa;Password=foo123bar!";
#endif
            services.AddSlipwaysData(connectionString);
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            ILogger<Startup> logger)
        {
            Logger = logger;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            Logger.LogError(9999, error.Error, error.Error.Message);
                            context.Response.AddApplicationError(error.Error.Message); 
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }

            app.UseCors(
          options =>
              options
              .AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());

            app.UseHttpMetrics();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
    }
}
