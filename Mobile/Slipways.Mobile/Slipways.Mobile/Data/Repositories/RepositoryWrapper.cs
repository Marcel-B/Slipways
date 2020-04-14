using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slipways.Mobile.Data.Repositories
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        public ISlipwayRepository Slipways { get; }
        public IExtraRepository Extras { get; }
        public IWaterRepository Waters { get; }
        public IManufacturerRepository Manufacturers { get; }
        public IStationRepository Stations { get; }
        public IMarinaRepository Marinas { get; }
        public IServiceRepository Services { get; }
        public ISlipwayExtraRepository SlipwayExtras { get; }

        protected readonly IDataContext Context;

        public RepositoryWrapper(
            IDataContext context,
            ISlipwayRepository slipwaysRepository,
            IWaterRepository waterRepository,
            IManufacturerRepository manufacturerRepository,
            IServiceRepository serviceRepository,
            IMarinaRepository marinaRepository,
            IStationRepository stationRepository,
            ISlipwayExtraRepository slipwayExtraRepository,
            IExtraRepository extraRepository)
        {
            Context = context;
            Slipways = slipwaysRepository;
            Stations = stationRepository;
            Marinas = marinaRepository;
            Services = serviceRepository;
            Waters = waterRepository;
            Manufacturers = manufacturerRepository;
            SlipwayExtras = slipwayExtraRepository;
            Extras = extraRepository;
        }

        public async Task<IEnumerable<Slipway>> GetAllSlipwaysWithSubsets()
        {
            var slipways = await Slipways.GetAllAsync();
            foreach (var slipway in slipways)
            {
                var water = await Waters.GetByUuidAsync(slipway.WaterPk);
                slipway.Water = water;
            }
            return slipways;
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>() where T : IEntity, new()
            => await Context.Table<T>().ToListAsync();
    }
}
