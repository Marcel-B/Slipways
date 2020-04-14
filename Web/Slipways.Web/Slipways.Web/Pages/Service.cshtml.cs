using com.b_velop.Slipways.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;

namespace com.b_velop.Slipways.Web
{
    public class ServiceModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<ServiceModel> _logger;

        [BindProperty]
        public HashSet<Service> Services { get; set; }

        public ServiceModel(
            IStoreWrapper dataStore,
            ILogger<ServiceModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var services = await _dataStore.Services.GetValuesAsync();
            Services = services.OrderBy(_ => _.Name).ToHashSet();
        }
    }
}