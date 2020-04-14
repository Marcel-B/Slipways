using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class ExtraRepository : RepositoryBase<Extra>, IExtraRepository
    {
        public ExtraRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Extra>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Extras;
        }

        public async Task<ILookup<Guid, Extra>> GetExtrasBySlipwayIdAsync(
            IEnumerable<Guid> slipwaysIds,
            CancellationToken cancellationToken)
        {
            if (slipwaysIds == null)
                throw new ArgumentNullException("SlipwaysIDs are null");

            try
            {
                var extras = await SelectAllAsync(cancellationToken);

                if (!MemoryCache.TryGetValue(Cache.SlipwayExtras, out HashSet<SlipwayExtra> slipwayExtrasAll))
                {
                    slipwayExtrasAll = Context.SlipwayExtras.ToHashSet();
                    MemoryCache.Set(Cache.SlipwayExtras, slipwayExtrasAll);
                }

                var extraIds = slipwayExtrasAll.Where(_ => slipwaysIds.Contains(_.SlipwayFk));
                var result = new List<Extra>();

                foreach (var extraId in extraIds)
                {
                    var extra = extras.FirstOrDefault(_ => _.Id == extraId.ExtraFk);
                    if (extra != null)
                        result.Add(new Extra
                        {
                            Id = extra.Id,
                            Name = extra.Name,
                            Created = extra.Created,
                            Updated = extra.Updated,
                            SlipwayFk = extraId.SlipwayFk
                        });
                }
                return result.ToLookup(_ => _.SlipwayFk);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Extras by SlipwayIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Extras by SlipwayIDs", e);
            }
            return default;
        }
    }
}
