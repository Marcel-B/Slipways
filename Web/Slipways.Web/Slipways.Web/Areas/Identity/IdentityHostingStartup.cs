using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(com.b_velop.Slipways.Web.Areas.Identity.IdentityHostingStartup))]
namespace com.b_velop.Slipways.Web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}