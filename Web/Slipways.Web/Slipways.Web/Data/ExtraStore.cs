using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Data.Extensions;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Web.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace com.b_velop.Slipways.Web.Data
{
    public class ExtraStore : DataStore<Extra, ExtraDto>, IExtraStore
    {
        public ExtraStore(
            IMemoryCache cache,
            IExtraService service,
            IGraphQLService graphQLService) : base(cache, service, graphQLService)
        {
            Key = Cache.Extras;
            Query = Queries.Extras.Item2;
            Method = Queries.Extras.Item1;
        }

        public override Extra ConvertToClass(
            ExtraDto item)
            => item.ToClass();

        public override ExtraDto ConvertToDto(
            Extra item)
            => item.ToDto();

        public override IEnumerable<Extra> Sort(IEnumerable<Extra> set)
            => set.OrderBy(_ => _.Name);

    }
}
