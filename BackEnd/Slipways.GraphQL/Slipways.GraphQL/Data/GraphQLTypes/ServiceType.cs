using System;
using System.Collections.Generic;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Data.Models;
using com.b_velop.Slipways.GrQl.Infrastructure;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace com.b_velop.Slipways.GrQl.Data.GraphQLTypes
{
    public class ServiceType : ObjectGraphType<Service>
    {
        public ServiceType(
            IGraphQlRepository repo,
            IDataLoaderContextAccessor accessor)
        {
            Name = nameof(Service);

            Field(_ => _.Id, type: typeof(NonNullGraphType<IdGraphType>));
            Field(_ => _.Name);
            Field(_ => _.Created);
            Field(_ => _.Updated, nullable: true);
            Field(_ => _.Street);
            Field(_ => _.Postalcode);
            Field(_ => _.City);
            Field(_ => _.Phone, nullable: true);
            Field(_ => _.Url, nullable: true);
            Field(_ => _.Email, nullable: true);
            Field(_ => _.Latitude);
            Field(_ => _.Longitude);

            FieldAsync<ListGraphType<ManufacturerType>, IEnumerable<ManufacturerDto>>(
                TypeName.Manufacturers,
                "Manufacturers which are repaired by this Service",
                resolve: async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, ManufacturerDto>("GetManufacturersByServices", repo.GetManufacturersByServices);
                    return await loader.LoadAsync(context.Source.Id);
                });
        }
    }
}
