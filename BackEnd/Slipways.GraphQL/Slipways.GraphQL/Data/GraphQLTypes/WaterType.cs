using System;
using System.Collections.Generic;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Infrastructure;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace com.b_velop.Slipways.GrQl.Data.GraphQLTypes
{
    public class WaterType : ObjectGraphType<Water>
    {
        public WaterType(
            ISlipwayRepository slipways,
            IStationRepository stations,
            IMarinaRepository  marinas,
            IDataLoaderContextAccessor accessor)
        {
            Name = nameof(Water);

            Field(_ => _.Id, type: typeof(NonNullGraphType<IdGraphType>));
            Field(_ => _.Shortname);
            Field(_ => _.Name);
            Field(_ => _.Updated, nullable: true);

            FieldAsync<ListGraphType<SlipwayType>, IEnumerable<Slipway>>(
                TypeName.Slipways,
                "All Slipways that are located on these Waters",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Slipway>("GetSlipwaysByWaters", slipways.GetSlipwaysByWaters);
                    return await loader.LoadAsync(context.Source.Id);
                });

            FieldAsync<ListGraphType<StationType>, IEnumerable<Station>>(
                TypeName.Stations,
                "All Stations that are located on these Waters",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Station>("GetStationsByWaters", stations.GetStationsLookupById);
                    return await loader.LoadAsync(context.Source.Id);
                });

            FieldAsync<ListGraphType<MarinaType>, IEnumerable<Marina>>(
                TypeName.Ports,
                "All Ports that are located on these Waters",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Marina>("GetMarinasByWaters", marinas.GetMarinasByWaters);
                    return await loader.LoadAsync(context.Source.Id);
                });
        }
    }
}
