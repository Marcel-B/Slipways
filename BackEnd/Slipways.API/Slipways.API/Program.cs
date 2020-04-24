using System;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Identity;
using com.b_velop.Slipways.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using Slipways.API;

namespace com.b_velop.Slipways.API
{
    public class Program
    {
        public static async Task  Main(
            string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var file = "dev-nlog.config";
            if (env == "Production")
                file = "nlog.config";

            var logger = NLogBuilder.ConfigureNLog(file).GetCurrentClassLogger();

            try
            {
                logger.Debug("init main");
                var host = CreateHostBuilder(args).Build();
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<SlipwaysContext>();
                    var userManager = services.GetRequiredService<UserManager<AppUser>>();
                    context.Database.Migrate();
                    await Seed.SeedData(context,userManager);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Stopped program because of exception");
                    throw;
                }
                host.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(
            string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://*:8095");
                webBuilder.UseStartup<Startup>();
            })
            .UseNLog();
    }
}
