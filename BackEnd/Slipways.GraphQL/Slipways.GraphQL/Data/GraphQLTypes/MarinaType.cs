using System;
using System.Collections.Generic;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Infrastructure;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace com.b_velop.Slipways.GrQl.Data.GraphQLTypes
{
    public class MarinaType : ObjectGraphType<Marina>
    {
        public MarinaType(
            ISlipwayRepository slipways,
            IWaterRepository waters,
            IDataLoaderContextAccessor accessor)
        {
            Name = nameof(Marina);

            Field(_ => _.Id, type: typeof(NonNullGraphType<IdGraphType>));

            Field(_ => _.Name);
            Field(_ => _.Created);
            Field(_ => _.Updated, nullable: true);
            Field(_ => _.Longitude);
            Field(_ => _.Latitude);
            Field(_ => _.Email, nullable: true);
            Field(_ => _.Phone, nullable: true);
            Field(_ => _.Street);
            Field(_ => _.Postalcode);
            Field(_ => _.City);
            Field(_ => _.Url, nullable: true);

            FieldAsync<WaterType, Water>(
                nameof(Water),
                "The Water on which the Port is located",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<Guid, Water>("GetWatersByMarinas", waters.GetWaterDictById);
                    return await loader.LoadAsync(context.Source.WaterId);
                });

            FieldAsync<ListGraphType<SlipwayType>, IEnumerable<Slipway>>(
                TypeName.Slipways,
                "Slipways located at this marina",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Slipway>("GetSlipwaysByMarinas", slipways.GetSlipwaysByMarinas);
                    return await loader.LoadAsync(context.Source.WaterId);
                });
        }
    }
}
