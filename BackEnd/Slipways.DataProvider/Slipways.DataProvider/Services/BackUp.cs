using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using com.b_velop.Slipways.Data.Contracts;
using System.IO;
using System.Collections.Generic;
using System.Security;
using com.b_velop.Slipways.Data.Helper;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.DataProvider.Services
{
    public class BackUp : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<BackUp> _logger;
        private Timer _timer;
        private const string BackUpPath = "./backUp/";

        public BackUp(
            IServiceProvider services,
            ILogger<BackUp> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackUp service running");
            _timer = new Timer(DoWork, cancellationToken, TimeSpan.FromMinutes(15), TimeSpan.FromHours(2));
            return Task.CompletedTask;
        }

        private async Task BackUpAsync<T>(
            IEnumerable<T> objects,
            string type,
            CancellationToken cancellationToken = default)
        {
            var fileName = Path.Combine(BackUpPath, $"{type}.json");
            try
            {
                var file = new FileInfo(fileName);
                var o = JsonConvert.SerializeObject(objects, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented });
                using var s = file.CreateText();
                await s.WriteAsync(o);
                await s.FlushAsync();
                s.Close();
            }
            catch (ArgumentNullException e)
            {
                _logger?.LogError(6659, $"Error occurred while back up '{type}' in file '{fileName}'", e);
            }
            catch (ArgumentException e)
            {
                _logger?.LogError(6660, $"Error occurred while back up '{type}' in file '{fileName}'", e);
            }
            catch (PathTooLongException e)
            {
                _logger?.LogError(6661, $"Error occurred while back up '{type}' in file '{fileName}'", e);
            }
            catch (NotSupportedException e)
            {
                _logger?.LogError(6662, $"Error occurred while back up '{type}' in file '{fileName}'", e);
            }
            catch (UnauthorizedAccessException e)
            {
                _logger?.LogError(6663, $"Error occurred while back up '{type}' in file '{fileName}' - Unauthorized", e);
            }
            catch (IOException e)
            {
                _logger?.LogError(6664, $"Error occurred while back up '{type}' in file '{fileName}'", e);
            }
            catch (SecurityException e)
            {
                _logger?.LogError(6665, $"Error occurred while back up '{type}' in file '{fileName}'", e);
            }
            catch (Exception e)
            {
                _logger?.LogError(6666, $"Unexpected error occurred while back up '{type}' in file '{fileName}'\n{e.Message}\n{e.StackTrace}\n{e.InnerException}", e);
            }
        }

        private async Task BackUpSlipwaysAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slipways = await wrapper.Slipway.SelectAllAsync(cancellationToken);
                await BackUpAsync(slipways, Cache.Slipways);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch slipways from RepositoryWrapper", e);
            }
        }

        private async Task BackUpStationsAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slipways = await wrapper.Station.SelectAllAsync(cancellationToken);
                await BackUpAsync(slipways, Cache.Stations);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch stations from RepositoryWrapper", e);
            }
        }

        private async Task BackUpServicesAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var services = await wrapper.Service.SelectAllAsync(cancellationToken);
                await BackUpAsync(services, Cache.Services);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch services from RepositoryWrapper", e);
            }
        }

        private async Task BackUpPortsAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var ports = await wrapper.Marina.SelectAllAsync(cancellationToken);
                await BackUpAsync(ports, Cache.Ports);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch ports from RepositoryWrapper", e);
            }
        }

        private async Task BackUpManufacturersAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slipways = await wrapper.Manufacturer.SelectAllAsync(cancellationToken);
                await BackUpAsync(slipways, Cache.Manufacturers);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch manufacturers from RepositoryWrapper", e);
            }
        }

        private async Task BackUpExtrasAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slipways = await wrapper.Extra.SelectAllAsync(cancellationToken);
                await BackUpAsync(slipways, Cache.Extras);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch extras from RepositoryWrapper", e);
            }
        }

        private async Task BackUpSlipwayExtrasAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slipways = await wrapper.SlipwayExtra.SelectAllAsync(cancellationToken);
                await BackUpAsync(slipways, Cache.SlipwayExtras);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch slipwayExtras from RepositoryWrapper", e);
            }
        }

        private async Task BackUpManufacturerServicesAsync(
            IRepositoryWrapper wrapper,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slipways = await wrapper.ManufacturerServices.SelectAllAsync(cancellationToken);
                await BackUpAsync(slipways, Cache.ManufacturerServices);
            }
            catch (Exception e)
            {
                _logger.LogError(6666, "Unexpected error occurred while fetch manufacturerServices from RepositoryWrapper", e);
            }
        }

        private async void DoWork(
            object state)
        {
            try
            {
                var cancellationToken = (CancellationToken)state;
                using var scope = _services.CreateScope();
                var directory = new DirectoryInfo("backUp");

                if (!directory.Exists)
                    directory.Create();

                _logger.LogInformation("Start BackUp Database");
                var wrapper = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();

                await BackUpSlipwaysAsync(wrapper, cancellationToken);
                await BackUpServicesAsync(wrapper, cancellationToken);
                await BackUpManufacturersAsync(wrapper, cancellationToken);
                await BackUpSlipwayExtrasAsync(wrapper, cancellationToken);
                await BackUpExtrasAsync(wrapper, cancellationToken);
                await BackUpManufacturerServicesAsync(wrapper, cancellationToken);
                await BackUpStationsAsync(wrapper, cancellationToken);
                await BackUpPortsAsync(wrapper, cancellationToken);
            }
            catch (InvalidOperationException e)
            {
                _logger?.LogError(6662, $"Error occurred while backup database", e);
            }
            catch (ArgumentNullException e)
            {
                _logger?.LogError(6663, $"Error occurred while backup database", e);
            }
            catch (ArgumentException e)
            {
                _logger?.LogError(6664, $"Error occurred while backup database", e);
            }
            catch (IOException e)
            {
                _logger?.LogError(6665, $"Error occurred while backup database", e);
            }
            catch (Exception e)
            {
                _logger?.LogError(6666, $"Unexpected error occurred while backup database", e);
            }
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("BackUp Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
