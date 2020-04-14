using com.b_velop.Slipways.Data.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly ILogger<RepositoryWrapper> _logger;
        public SlipwaysContext Context { get; }

        public IWaterRepository Water { get; }
        public IStationRepository Station { get; }
        public ISlipwayRepository Slipway { get; }
        public IExtraRepository Extra { get; }
        public IServiceRepository Service { get; }
        public IManufacturerRepository Manufacturer { get; }
        public IPortRepository Port { get; }
        public ISlipwayExtraRepository SlipwayExtra { get; }
        public IManufacturerServicesRepository ManufacturerServices { get; }

        public RepositoryWrapper(
            SlipwaysContext context,
            IWaterRepository waterRepository,
            IStationRepository stationRepository,
            ISlipwayRepository slipwayRepository,
            IExtraRepository extraRepository,
            IServiceRepository serviceRepository,
            IManufacturerRepository manufacturerRepository,
            IPortRepository portRepository,
            ISlipwayExtraRepository slipwayExtraRepository,
            IManufacturerServicesRepository manufacturerServicesRepository,
            ILogger<RepositoryWrapper> logger)
        {
            Context = context;
            Water = waterRepository;
            Station = stationRepository;
            Slipway = slipwayRepository;
            Extra = extraRepository;
            Service = serviceRepository;
            Manufacturer = manufacturerRepository;
            Port = portRepository;
            SlipwayExtra = slipwayExtraRepository;
            ManufacturerServices = manufacturerServicesRepository;
            _logger = logger;
        }

        public void SaveChanges()
        {
            try
            {
                Context.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError(6666, $"Unexpected error occurred while saving context\n{e.Message}\n{e.StackTrace}\n{e.InnerException}", e);
            }
        }
    }
}
