using System;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Data.GraphQLTypes;
using com.b_velop.Slipways.GrQl.Data.Models;
using com.b_velop.Slipways.GrQl.Infrastructure;
using GraphQL.Types;
using Prometheus;

namespace com.b_velop.Slipways.GrQl.Data.GraphQLQueries
{
    public class AppQuery : ObjectGraphType
    {
        public AppQuery(
            IWaterRepository waters,
            ISlipwayRepository slipways,
            IExtraRepository extras,
            IManufacturerRepository manufacturers,
            IServiceRepository services,
            IMarinaRepository marinas,
            IStationRepository stations)
        {
            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_waters_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<WaterType>>(
                    name: TypeName.Waters,
                    description: "Provides all Waters",
                    resolve: async context => await waters.GetWaters());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_stations_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<StationType>>(
                    name: TypeName.Stations,
                    description: "Provides all Stations",
                    resolve: async context => await stations.GetStations());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_slipways_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<SlipwayType>>(
                    name: TypeName.Slipways,
                    description: "Provides all Slipways",
                    resolve: async context => await slipways.GetSlipways());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<ExtraType>>(
                    name: TypeName.Extras,
                    description: "Provides all Extras",
                    resolve: async context => await extras.GetExtras());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_ports_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<MarinaType>>(
                   name: TypeName.Ports,
                   description: "Provides all Ports",
                   resolve: async context => await marinas.GetMarinas());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_services_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<ServiceType>>(
                    name: TypeName.Services,
                    description: "Provides all Services",
                    resolve: async context => await services.GetServices());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_manufacturers_seconds", "").NewTimer())
            {
                FieldAsync<ListGraphType<ManufacturerType>>(
                    name: TypeName.Manufacturers,
                    description: "Provides all Manufacturers",
                    resolve: async context => await manufacturers.GetManufacturers());
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_slipway_seconds", "").NewTimer())
            {
                FieldAsync<SlipwayType>(
                    nameof(Slipway),
                    "Select Slipway by ID",
                    new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = nameof(Slipway.Id), Description = "The unique identifier of the unit" }),
                    resolve: async context =>
                    {
                        var id = context.GetArgument<Guid>(nameof(Slipway.Id));
                        var slipway = await slipways.GetSlipway(id);
                        return slipway;
                    });
            }

            using (Metrics.CreateHistogram("slipways_gql_duration_graphql_query_appdata_seconds", "").NewTimer())
            {
                FieldAsync<AppDataType>(
                    nameof(AppData),
                    "Select Application Data",
                    resolve: async context =>
                    {
                        var appData = new AppData
                        {
                            Slipways = await slipways.GetSlipways(),
                            Waters = await waters.GetWaters(),
                            Stations = await stations.GetStations(),
                            Services = await services.GetServices(),
                            Extras = await extras.GetExtras(),
                            Manufacturers = await manufacturers.GetManufacturers(),
                            Marinas = await marinas.GetMarinas()
                        };
                        return appData;
                    });
            }
        }
    }
}
