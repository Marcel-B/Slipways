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
    public class PortRepository : RepositoryBase<Port>, IPortRepository
    {
        public PortRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Port>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Ports;
        }

        public async Task<IDictionary<Guid, Port>> GetPortsByIdAsync(
            IEnumerable<Guid> portIds,
            CancellationToken cancellationToken)
        {
            if (portIds == null)
                throw new ArgumentNullException("PortIDs are null");

            try
            {
                var ports = await SelectAllAsync(cancellationToken);
                var result = ports.Where(_ => portIds.Contains(_.Id));
                return result.ToDictionary(x => x.Id);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6664, $"Error occurred while getting Ports by ID", e);
            }
            catch (ArgumentException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Ports by ID", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Ports by ID", e);
            }
            return default;
        }

        public async Task<ILookup<Guid, Port>> GetPortsByWaterIdAsync(
            IEnumerable<Guid> waterIds,
            CancellationToken cancellationToken)
        {
            if (waterIds == null)
                throw new ArgumentNullException("WaterIDs were null");

            try
            {
                var ports = await SelectAllAsync(cancellationToken);

                if (!MemoryCache.TryGetValue(Cache.Waters, out HashSet<Water> watersAll))
                {
                    watersAll = Context.Waters.ToHashSet();
                    MemoryCache.Set(Cache.Waters, watersAll);
                }

                var waters = watersAll.Where(_ => waterIds.Contains(_.Id));
                var result = new List<Port>();

                foreach (var water in waters)
                {
                    var port = ports.FirstOrDefault(_ => _.WaterFk == water.Id);
                    if (port != null)
                        result.Add(port);
                }

                return result.ToLookup(_ => _.WaterFk);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Ports by WaterIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Ports by WaterIDs", e);
            }
            return default;
        }
    }
}
