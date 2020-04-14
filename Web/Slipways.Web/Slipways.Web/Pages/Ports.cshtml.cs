using System.Collections.Generic;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web
{
    public class PortsModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<PortsModel> _logger;

        [BindProperty]
        public HashSet<Port> Ports { get; set; }

        public PortsModel(
            IStoreWrapper dataStore,
            ILogger<PortsModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var ports = await _dataStore.Ports.GetValuesAsync();
            Ports = ports;
        }
    }
}