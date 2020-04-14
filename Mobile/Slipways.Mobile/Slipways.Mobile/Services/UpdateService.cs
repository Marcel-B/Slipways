using Prism.Events;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Events;
using Slipways.Mobile.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slipways.Mobile.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IGraphQLService _graphQLService;

        public UpdateService(
            IEventAggregator eventAggregator,
            IRepositoryWrapper repositoryWrapper,
            IGraphQLService graphQLService)
        {
            eventAggregator.GetEvent<InitializationReadyEvent>().Subscribe(Update);
            _eventAggregator = eventAggregator;
            _repositoryWrapper = repositoryWrapper;
            _graphQLService = graphQLService;
        }

        public async void Update(
            bool start)
        {
            await UpdateExtra().ConfigureAwait(false);
            await UpdateManufacturer().ConfigureAwait(false);
            await UpdateWater().ConfigureAwait(false);
            await UpdateSlipway().ConfigureAwait(false);
            await UpdateMarina().ConfigureAwait(false);
            await UpdateStation().ConfigureAwait(false);
        }

        public async Task UpdateExtra()
        {
            var response = await _graphQLService
                .FetchValuesAsync<ExtrasResponse>(Queries.Extras)
                .ConfigureAwait(false);

            if (response == null)
                return;

            foreach (var extra in response.Extras.OrderBy(_ => _.Name))
            {
                var tmp = await _repositoryWrapper.Extras.GetByUuidAsync(extra.Pk);
                if (tmp == null)
                {
                    await _repositoryWrapper.Extras.InsertAsync(extra);
                }
                else if (tmp.Updated != extra.Updated)
                {
                    extra.Id = tmp.Id;
                    await _repositoryWrapper.Extras.UpdateAsync(tmp.Id, extra);
                }
            }

            var eventArgs = new DataUpdateEventArgs<Extra>
            {
                Type = DataT.Extra,
                Data = await _repositoryWrapper.Extras.GetAllAsync()
            };

            _eventAggregator
                .GetEvent<UpdateReadyEvent<Extra>>()
                .Publish(eventArgs);
        }

        public async Task UpdateMarina()
        {
            var response = await _graphQLService
                .FetchValuesAsync<MarinasResponse>(Queries.Marinas)
                .ConfigureAwait(false);

            foreach (var marina in response.Ports.OrderBy(_ => _.Name))
            {
                marina.WaterPk = marina.Water.Pk;
                var tmp = await _repositoryWrapper.Marinas.GetByUuidAsync(marina.Pk);
                if (tmp == null)
                {
                    await _repositoryWrapper.Marinas.InsertAsync(marina);
                }
                else if (tmp.Updated != marina.Updated)
                {
                    marina.Id = tmp.Id;
                    await _repositoryWrapper.Marinas.UpdateAsync(tmp.Id, marina);
                }
            }

            var eventArgs = new DataUpdateEventArgs<Marina>
            {
                Type = DataT.Marina,
                Data = await _repositoryWrapper.Marinas.GetAllAsync()
            };

            _eventAggregator
                .GetEvent<UpdateReadyEvent<Marina>>()
                .Publish(eventArgs);
        }

        public async Task UpdateManufacturer()
        {
            var response = await _graphQLService
                .FetchValuesAsync<ManufacturersResponse>(Queries.Manufacturers)
                .ConfigureAwait(false);

            foreach (var manufacturer in response.Manufacturers)
            {
                var tmp = _repositoryWrapper.Manufacturers.GetByUuidAsync(manufacturer.Pk);
                if (tmp == null)
                    await _repositoryWrapper.Manufacturers.InsertAsync(manufacturer);
                else
                {
                    manufacturer.Id = tmp.Id;
                    await _repositoryWrapper.Manufacturers.UpdateAsync(manufacturer.Id, manufacturer);
                }
            }
            // Fire event
            var eventArgs = new DataUpdateEventArgs<Manufacturer>
            {
                Type = DataT.Manufacturer,
                Data = await _repositoryWrapper.Manufacturers.GetAllAsync()
            };

            _eventAggregator
                .GetEvent<UpdateReadyEvent<Manufacturer>>()
                .Publish(eventArgs);
        }

        public async Task UpdateSlipway()
        {
            var response = await _graphQLService
                .FetchValuesAsync<SlipwaysResponse>(Queries.Slipways)
                .ConfigureAwait(false);

            if (response == null)
                return;

            var slipways = new List<Slipway>();
            foreach (var slipway in response.Slipways)
            {
                foreach (var extra in slipway.Extras)
                {
                    var se = await _repositoryWrapper.SlipwayExtras.GetBySlipwayPkAsync(slipway.Pk);
                    if (se == null)
                    {
                        var l = new SlipwayExtra
                        {
                            Pk = Guid.NewGuid(),
                            SlipwayPk = slipway.Pk,
                            ExtraPk = extra.Pk
                        };
                        var id = await _repositoryWrapper.SlipwayExtras.InsertAsync(l);
                    }
                    else if (se.ExtraPk != extra.Pk)
                    {
                        var l = new SlipwayExtra
                        {
                            Pk = Guid.NewGuid(),
                            SlipwayPk = slipway.Pk,
                            ExtraPk = extra.Pk
                        };
                        var id = await _repositoryWrapper.SlipwayExtras.InsertAsync(l);
                    }
                }

                slipway.WaterPk = slipway.Water.Pk;
                slipway.MarinaPk = slipway.Marina == null ? Guid.Empty : slipway.Marina.Pk;
                var tmp = await _repositoryWrapper.Slipways.GetByUuidAsync(slipway.Pk);

                if (tmp == null)
                {
                    slipways.Add(slipway);
                }
                else if (tmp.Updated != slipway.Updated)
                {
                    slipway.Id = tmp.Id;
                    await _repositoryWrapper.Slipways.UpdateAsync(slipway.Id, slipway);
                }
            }
            if (slipways.Count > 0)
            {
                var result = await _repositoryWrapper.Slipways.InsertBunchAsync(slipways);
            }

            // Fire event
            var eventArgs = new DataUpdateEventArgs<Slipway>
            {
                Type = DataT.Slipway,
                Data = await _repositoryWrapper.Slipways.GetAllAsync()
            };
            _eventAggregator
                .GetEvent<UpdateReadyEvent<Slipway>>()
                .Publish(eventArgs);
        }

        public async Task UpdateStation()
        {
            var response = await _graphQLService.FetchValuesAsync<StationResponse>(Queries.Stations);
            foreach (var station in response.Stations)
            {
                var tmp = await _repositoryWrapper.Stations.GetByUuidAsync(station.Pk);
                if(tmp == null)
                {
                    station.WaterPk = station.Water.Pk;
                    await _repositoryWrapper.Stations.InsertAsync(station);
                }else if(tmp.Updated != station.Updated)
                {
                    station.Id = tmp.Id;
                    station.WaterPk = station.Water.Pk;
                    await _repositoryWrapper.Stations.UpdateAsync(station.Id, station);
                }
            }

            // Fire event
            var eventArgs = new DataUpdateEventArgs<Station>
            {
                Type = DataT.Station,
                Data = await _repositoryWrapper.Stations.GetAllAsync()
            };

            _eventAggregator
                .GetEvent<UpdateReadyEvent<Station>>()
                .Publish(eventArgs);
        }

        public async Task UpdateWater()
        {
            // Fetch values from API
            var response = await _graphQLService.FetchValuesAsync<WatersResponse>(Queries.Waters);

            if (response == null)
                return;

            var entities = new List<Water>();

            // Insert or Update values
            foreach (var water in response.Waters)
            {
                var tmp = await _repositoryWrapper.Waters.GetByUuidAsync(water.Pk);
                if (tmp == null)
                {
                    entities.Add(water);
                }
                else if (tmp.Updated != water.Updated)
                {
                    water.Id = tmp.Id;
                    await _repositoryWrapper.Waters.UpdateAsync(water.Id, water);
                }
            }

            if (entities.Count > 0)
            {
                var result = await _repositoryWrapper.Waters.InsertBunchAsync(entities);
            }
            // Fire event
            var eventArgs = new DataUpdateEventArgs<Water>
            {
                Type = DataT.Water,
                Data = await _repositoryWrapper.Waters.GetAllAsync()
            };

            _eventAggregator
                .GetEvent<UpdateReadyEvent<Water>>()
                .Publish(eventArgs);
        }
    }
}
