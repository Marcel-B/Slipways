using com.b_velop.Slipways.Data.Contracts;
using GraphQL.DataLoader;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Data.Models;

namespace com.b_velop.Slipways.GrQl.Data.GraphQLTypes
{
    public class SlipwayType : ObjectGraphType<Slipway>
    {
        public SlipwayType(
            IWaterRepository waters,
            IMarinaRepository marinas,
            IGraphQlRepository graphQls,
            IDataLoaderContextAccessor accessor)
        {
            Name = nameof(Slipway);

            Field(_ => _.Id, type: typeof(NonNullGraphType<IdGraphType>));
            Field(_ => _.Created);
            Field(_ => _.Name);
            Field(_ => _.Street);
            Field(_ => _.City);
            Field(_ => _.Postalcode);
            Field(_ => _.Costs);
            Field(_ => _.Rating);
            Field(_ => _.Updated, nullable: true);
            Field(_ => _.Comment, nullable: true);
            Field(_ => _.Pro, nullable: true);
            Field(_ => _.Contra, nullable: true);
            Field(_ => _.Country, nullable: true);
            Field(_ => _.Latitude);
            Field(_ => _.Longitude);


            FieldAsync<WaterType, Water>(
                nameof(Water),
                description: "The Water where the Slipway is located",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<Guid, Water>("GetWatersBySlipways", waters.GetWaterDictById);
                    return await loader.LoadAsync(context.Source.WaterId);
                });
            
            FieldAsync<MarinaType, Marina>(
                nameof(Marina),
                description: "A Port where the Slipway is located",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<Guid, Marina>("GetMarinasBySlipways", marinas.GetMarinaDictById);
                    return await loader.LoadAsync(context.Source.MarinaId ?? Guid.Empty);
                });

            FieldAsync<ListGraphType<ExtraType>, IEnumerable<ExtraDto>>(
               nameof(Extra),
                "Extras which the Slipway has to offer",
                resolve:  context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, ExtraDto>("GetExtrasBySlipways", graphQls.GetExtrasBySlipways);
                    return  loader.LoadAsync(context.Source.Id);
                });
        }
    }
}
