using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Events;

namespace Slipways.Mobile.Data
{
    public class DataStore : IDataStore
    {
        private IDataContext _context;
        private IEventAggregator _eventAggregator;
        private IRepositoryWrapper _repository;
        private IUpdateService _updateService;

        //public IEnumerable<Water> Waters => _repository.Waters.GetAllAsync();
        //public IEnumerable<Slipway> Slipways => _repository.Slipways.GetAllAsync();
        //public IEnumerable<Manufacturer> Manufacturers => _repository.Manufacturers.GetAllAsync();
        //public IEnumerable<Extra> Extras => _repository.Extras.GetAllAsync();
        //public IEnumerable<Marina> Marinas => _repository.Marinas.GetAllAsync();
        //public IEnumerable<Service> Services => _repository.Services.GetAllAsync();
        //public IEnumerable<Station> Stations => _repository.Stations.GetAllAsync();

        public DataStore(
            IDataContext context,
            IEventAggregator eventAggregator,
            IRepositoryWrapper repository,
            IUpdateService updateService)
        {
            _context = context;
            _eventAggregator = eventAggregator;
            _repository = repository;
            _updateService = updateService;
        }

        /// <summary>
        /// Load Data from SQLite Database to Memory
        /// </summary>
        public async Task LoadData()
        {
            var waters = await _repository.Waters.GetAllAsync();
            if (waters == null || waters.Count() == 0)
                await _updateService
                    .UpdateWater()
                    .ConfigureAwait(false);

            //var slipways = Slipways;
            //if (slipways == null || slipways.Count() == 0)
            //    await _updateService
            //        .UpdateSlipway()
            //        .ConfigureAwait(false);

            //var manufacturers = Manufacturers;
            //if (manufacturers == null || manufacturers.Count() == 0)
            //    await _updateService
            //        .UpdateManufacturer()
            //        .ConfigureAwait(false);

            //var marinas = Marinas;
            //if (marinas == null || marinas.Count() == 0)
            //    await _updateService
            //        .UpdateMarina()
            //        .ConfigureAwait(false);

            //var stations = Stations;
            //if (stations == null || stations.Count() == 0)
            //    await _updateService
            //        .UpdateStation()
            //        .ConfigureAwait(false);

            //_eventAggregator.GetEvent<UpdateReadyEvent>().Publish("rdy");
        }
    }
}