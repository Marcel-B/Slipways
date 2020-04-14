using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Data
{
    public class WaterStore : DataStore<Water, WaterDto>, IWaterStore
    {
        public WaterStore(
            IMemoryCache cache,
            IWaterService service,
            IGraphQLService graphQLService) : base(cache, service, graphQLService)
        {
            Key = Cache.Waters;
            Method = Queries.Waters.Item1;
            Query = Queries.Waters.Item2;
        }

        public override Water ConvertToClass(
            WaterDto item)
            => item.ToClass();

        public override WaterDto ConvertToDto(
            Water item)
            => item.ToDto();

        public override async Task<HashSet<Water>> GetValuesAsync()
        {
            if (!_cache.TryGetValue(Key, out HashSet<Water> entities))
            {
                var values = await _graphQLService.GetValuesAsync<IEnumerable<Water>>(Method, Query);
                if (values == null)
                    return null;
                values = Sort(values);

                foreach (var value in values)
                    value.Longname = value.Longname.FirstUpper();

                entities = values.ToHashSet();
                _cache.Set(Key, entities);
            }
            return entities;
        }

        public override IEnumerable<Water> Sort(IEnumerable<Water> set)
            => set.OrderBy(_ => _.Longname);
    }
}
