using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class StationRepository : RepositoryBase<Station>, IStationRepository
    {
        public StationRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Station>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Stations;
        }

        public async Task<ILookup<Guid, Station>> GetStationsByWaterIdAsync(
             IEnumerable<Guid> waterIds,
             CancellationToken cancellationToken)
        {
            if (waterIds == null)
                throw new ArgumentNullException("WaterIDs are null");

            try
            {
                var stations = await SelectAllAsync(cancellationToken);
                var result = stations.Where(_ => waterIds.Contains(_.WaterFk));
                return result.ToLookup(x => x.WaterFk);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Stations by WaterIds", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Stations by WaterIds", e);
            }
            return default;
        }
    }
}
