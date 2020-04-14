using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Services
{
    public class CacheLoader : IHostedService, IDisposable
    {
        private readonly ILogger<CacheLoader> _logger;
        private Timer _timer;

        public CacheLoader(
        IServiceProvider services,
        ILogger<CacheLoader> logger)
        {
            Services = services;
            _logger = logger;
        }

        public IServiceProvider Services { get; }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheLoader Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private async void DoWork(
            object state)
        {
            try
            {
                using var scope = Services.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                var graphQLService = scope.ServiceProvider.GetRequiredService<IGraphQLService>();

                var waters = await graphQLService.GetValuesAsync<IEnumerable<WaterDto>>(Queries.Waters.Item1, Queries.Waters.Item2);

                if (waters != null)
                {
                    var whs = waters.ToHashSet();
                    cache.Set(Cache.Waters, whs);
                }

                var slipways = await graphQLService.GetValuesAsync<IEnumerable<SlipwayDto>>(Queries.Slipways.Item1, Queries.Slipways.Item2);

                if (slipways != null)
                {
                    var shs = slipways.ToHashSet();
                    cache.Set(Cache.Slipways, shs);
                }

                var services = await graphQLService.GetValuesAsync<IEnumerable<ServiceDto>>(Queries.Services.Item1, Queries.Services.Item2);

                if (services != null)
                {
                    var sehs = services.ToHashSet();
                    cache.Set(Cache.Services, sehs);
                }

                var ports = await graphQLService.GetValuesAsync<IEnumerable<PortDto>>(Queries.Ports.Item1, Queries.Ports.Item2);

                if (ports != null)
                {
                    var sehs = ports.ToHashSet();
                    cache.Set(Cache.Ports, sehs);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(6666, e, "Unexpected error occurred while reload cache");
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}