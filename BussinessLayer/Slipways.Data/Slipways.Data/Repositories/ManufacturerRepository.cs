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
    public class ManufacturerRepository : RepositoryBase<Manufacturer>, IManufacturerRepository
    {
        public ManufacturerRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Manufacturer>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Manufacturers;
        }

        public async Task<ILookup<Guid, Manufacturer>> GetManufacturerByServiceIdAsync(
            IEnumerable<Guid> serviceIds,
            CancellationToken cancellationToken)
        {
            if (serviceIds == null)
                throw new ArgumentNullException("ServiceIDs are null");

            try
            {
                var manufacturers = await SelectAllAsync(cancellationToken);
                if (!MemoryCache.TryGetValue(Cache.ManufacturerServices, out HashSet<ManufacturerService> manufacturerServicesAll))
                {
                    manufacturerServicesAll = Context.ManufacturerServices.ToHashSet();
                    MemoryCache.Set(Cache.ManufacturerServices, manufacturerServicesAll);
                }
                var manufacturerServices = manufacturerServicesAll.Where(_ => serviceIds.Contains(_.ServiceFk));
                var result = new List<Manufacturer>();

                foreach (var manufacturerService in manufacturerServices)
                {
                    var manufacturer = manufacturers.FirstOrDefault(_ => _.Id == manufacturerService.ManufacturerFk);
                    if (manufacturer != null)
                        result.Add(new Manufacturer
                        {
                            Id = manufacturer.Id,
                            Name = manufacturer.Name,
                            Updated = manufacturer.Updated,
                            Created = manufacturer.Created,
                            ServiceFk = manufacturerService.ServiceFk
                        });
                }
                return result.ToLookup(_ => _.ServiceFk);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Manufacturers by ServiceIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Manufacturers by ServiceIDs", e);
            }
            return default;
        }
    }
}
