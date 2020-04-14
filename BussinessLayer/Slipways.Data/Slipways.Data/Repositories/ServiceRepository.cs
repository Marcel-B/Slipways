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
    public class ServiceRepository : RepositoryBase<Service>, IServiceRepository
    {
        public ServiceRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<Service>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.Services;
        }

        public async Task<ILookup<Guid, Service>> GetServicesByManufacturerIdAsync(
            IEnumerable<Guid> manufacturerIds,
            CancellationToken cancellationToken)
        {
            if (manufacturerIds == null)
                throw new ArgumentNullException("ManufacturerIDs are null");

            try
            {
                var services = await SelectAllAsync(cancellationToken);
                if (!MemoryCache.TryGetValue(Cache.ManufacturerServices, out HashSet<ManufacturerService> manufacturerServicesAll))
                {
                    manufacturerServicesAll = Context.ManufacturerServices.ToHashSet();
                    MemoryCache.Set(Cache.ManufacturerServices, manufacturerServicesAll);
                }

                var manufacturerServices = manufacturerServicesAll.Where(_ => manufacturerIds.Contains(_.ManufacturerFk));
                var result = new List<Service>();

                foreach (var manufacturerService in manufacturerServices)
                {
                    var service = services.FirstOrDefault(_ => _.Id == manufacturerService.ServiceFk);

                    if (service != null)
                        result.Add(new Service
                        {
                            Id = service.Id,
                            Name = service.Name,
                            Street = service.Street,
                            Postalcode = service.Postalcode,
                            City = service.City,
                            Phone = service.Phone,
                            Url = service.Url,
                            Updated = service.Updated,
                            Longitude = service.Longitude,
                            Latitude = service.Latitude,
                            Email = service.Email,
                            Created = service.Created,
                            ManufacturerFk = manufacturerService.ManufacturerFk
                        });
                }
                return result.ToLookup(_ => _.ManufacturerFk);
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, $"Error occurred while getting Services by ManufacturerIDs", e);
            }
            catch (Exception e)
            {
                Logger.LogError(6666, $"Error occurred while getting Services by ManufacturerIDs", e);
            }
            return default;
        }
    }
}
