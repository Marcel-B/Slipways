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
    public class SlipwayRepository : RepositoryBase<Slipway>, ISlipwayRepository
    {
        public SlipwayRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Slipway>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Slipways;
        }

        public async Task<ILookup<Guid, Slipway>> GetSlipwaysByPortIdAsync(
            IEnumerable<Guid> portIds,
            CancellationToken cancellationToken)
        {
            if (portIds == null)
                throw new ArgumentNullException("PortIDs are null");

            try
            {
                var slipways = await SelectAllAsync(cancellationToken);
                var result = new List<Slipway>();

                foreach (var slipway in slipways)
                    if (portIds.Contains(slipway.PortFk ?? Guid.Empty))
                        result.Add(slipway);

                return result.ToLookup(x => x.PortFk ?? Guid.Empty);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Slipways by PortIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Slipways by PortIDs", e);
            }
            return default;
        }

        public async Task<ILookup<Guid, Slipway>> GetSlipwayByWaterIdAsync(
            IEnumerable<Guid> waterIds,
            CancellationToken cancellationToken)
        {
            if (waterIds == null)
                throw new ArgumentNullException("WaterIDs were null");

            try
            {
                var slipways = await SelectAllAsync(cancellationToken);
                var result = new List<Slipway>();

                foreach (var slipway in slipways)
                    if (waterIds.Contains(slipway.WaterFk))
                        result.Add(slipway);

                return result.ToLookup(x => x.WaterFk);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Slipways by WaterIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Slipways by WaterIDs", e);
            }
            return default;
        }

        public async Task<ILookup<Guid, Slipway>> GetSlipwaysByExtraIdAsync(
             IEnumerable<Guid> extraIds,
             CancellationToken cancellationToken)
        {
            if (extraIds == null)
                throw new ArgumentNullException("ExtraIDs were null");

            try
            {
                var slipways = await SelectAllAsync(cancellationToken);
                if (!MemoryCache.TryGetValue(Cache.SlipwayExtras, out HashSet<SlipwayExtra> slipwayExtrasAll))
                {
                    slipwayExtrasAll = Context.SlipwayExtras.ToHashSet();
                    MemoryCache.Set(Cache.SlipwayExtras, slipwayExtrasAll);
                }
                var slipwayExtras = slipwayExtrasAll.Where(_ => extraIds.Contains(_.ExtraFk));

                var result = new List<Slipway>();

                foreach (var slipwayExtra in slipwayExtras)
                {
                    var slipway = slipways.FirstOrDefault(_ => _.Id == slipwayExtra.SlipwayFk);
                    if (slipway != null)
                        result.Add(new Slipway
                        {
                            Id = slipway.Id,
                            //ExtraFk = slipwayExtra.ExtraFk,
                            City = slipway.City,
                            Latitude = slipway.Latitude,
                            Longitude = slipway.Longitude,
                            Name = slipway.Name,
                            Postalcode = slipway.Postalcode,
                            WaterFk = slipway.WaterFk,
                            Updated = slipway.Updated,
                            Street = slipway.Street,
                            Rating = slipway.Rating,
                            Pro = slipway.Pro,
                            Created = slipway.Created,
                            Costs = slipway.Costs,
                            Contra = slipway.Contra,
                            Comment = slipway.Comment
                        });
                }
                return null;
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Slipways by ExtraIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Slipways by ExtraIDs", e);
            }
            return default;
        }

        public async Task<Slipway> AddPortToSlipwayAsync(
            Guid slipwayId,
            Guid portId)
        {
            Slipway tmp = null;
            try
            {
                tmp = await Context.Slipways.FirstOrDefaultAsync(_ => _.Id == slipwayId);
                if (tmp == null)
                    return null;
                tmp.Updated = DateTime.Now;
                tmp.PortFk = portId;
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while adding Port to Slipways\nPort: '{portId}'\nSlipway: '{slipwayId}'", e);
            }

            try
            {
                Context.SaveChanges();
                return tmp;
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while saving Slipway Context after update\nPort: '{portId}'\nSlipway: '{slipwayId}'", e);
            }
            return null;
        }
    }
}
