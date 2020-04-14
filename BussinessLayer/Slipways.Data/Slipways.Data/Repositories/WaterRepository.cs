using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class WaterRepository : RepositoryBase<Water>, IWaterRepository
    {
        public WaterRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Water>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Waters;
        }

        public async Task<IDictionary<Guid, Water>> GetWatersByIdAsync(
            IEnumerable<Guid> waterIds,
            CancellationToken cancellationToken)
        {
            if (waterIds == null)
                throw new ArgumentNullException("WaterIDs are null");

            try
            {
                var waters = await SelectAllAsync(cancellationToken);
                var result = waters.Where(_ => waterIds.Contains(_.Id));
                return result.ToDictionary(x => x.Id);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6664, $"Error occurred while getting Waters by ID", e);
            }
            catch (ArgumentException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Waters by ID", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Waters by ID", e);
            }
            return default;
        }
    }
}
