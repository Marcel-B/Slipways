using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Helper;
using com.b_velop.Slipways.Data.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class ManufacturerServicesRepository : RepositoryBase<ManufacturerService>, IManufacturerServicesRepository
    {
        public ManufacturerServicesRepository(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<ManufacturerService>> logger) : base(context, memoryCache, logger)
        {
            Key = Cache.ManufacturerServices;
        }
    }
}
