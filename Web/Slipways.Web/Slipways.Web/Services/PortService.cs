using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace com.b_velop.Slipways.Web.Services
{
    public class PortService : TokenService<PortDto>, IPortService
    {
        public PortService(
            HttpClient client, 
            ApplicationInfo applicationInfo,
            IWebHostEnvironment environment, 
            ILogger<PortService> logger) : base(client, applicationInfo, environment, logger)
        {
            ApiPath = "port";
        }
    }
}
