using System.Collections.Generic;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Data.Models;
using com.b_velop.Slipways.GrQl.Infrastructure;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace com.b_velop.Slipways.GrQl.Data.GraphQLTypes
{
    public class AppDataType : ObjectGraphType<AppData>
    {
        public AppDataType(
            IDataLoaderContextAccessor accessor,
            ISlipwayRepository slipways,
            IServiceRepository services,
            IStationRepository stations,
            IMarinaRepository marinas,
            IManufacturerRepository manufacturers,
            IExtraRepository extras,
            IWaterRepository waters)
        {
            Name = nameof(AppData);
            Description = "All values for mobile Applications";

            FieldAsync<ListGraphType<SlipwayType>, IEnumerable<Slipway>>(
                TypeName.Slipways,
                Cache.Slipways,
                resolve: async context => await slipways.GetSlipways());

            FieldAsync<ListGraphType<WaterType>, IEnumerable<Water>>(
                TypeName.Waters,
                Cache.Waters,
                resolve: async context => await waters.GetWaters());

            FieldAsync<ListGraphType<StationType>, IEnumerable<Station>>(
              TypeName.Stations,
              Cache.Stations,
              resolve: async context => await stations.GetStations());

            FieldAsync<ListGraphType<MarinaType>, IEnumerable<Marina>>(
              TypeName.Ports,
              Cache.Ports,
              resolve: async context => await marinas.GetMarinas());

            FieldAsync<ListGraphType<ServiceType>, IEnumerable<Service>>(
              TypeName.Services,
              Cache.Services,
              resolve: async context => await services.GetServices());

            FieldAsync<ListGraphType<ExtraType>, IEnumerable<Extra>>(
                TypeName.Extras,
                TypeName.Extras,
                resolve: async context => await extras.GetExtras());

            FieldAsync<ListGraphType<ManufacturerType>, IEnumerable<Manufacturer>>(
                  TypeName.Manufacturers,
                  TypeName.Manufacturers,
                  resolve: async context => await manufacturers.GetManufacturers());
        }
    }
}
