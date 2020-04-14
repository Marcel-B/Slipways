using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace com.b_velop.Slipways.Web.Data
{

    public class ManufacturerStore : DataStore<Manufacturer, ManufacturerDto>, IManufacturerStore
    {
        private ILogger<ManufacturerStore> _logger;

        public ManufacturerStore(
            IMemoryCache cache,
            IManufacturerService service,
            IGraphQLService graphQLService,
            ILogger<ManufacturerStore> logger) : base(cache, service, graphQLService)
        {
            _logger = logger;
            Key = Cache.Manufacturers;
            Method = Queries.Manufacturers.Item1;
            Query = Queries.Manufacturers.Item2;
        }

        public override Manufacturer ConvertToClass(
            ManufacturerDto item)
            => item.ToClass();

        public override ManufacturerDto ConvertToDto(
            Manufacturer item)
            => item.ToDto();

        public override IEnumerable<Manufacturer> Sort(IEnumerable<Manufacturer> set)
            => set.OrderBy(_ => _.Name);
    }
}
