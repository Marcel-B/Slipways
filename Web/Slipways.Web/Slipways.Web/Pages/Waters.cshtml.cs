using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Pages
{
    public class WatersModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<WatersModel> _logger;

        [BindProperty]
        public HashSet<Water> Waters { get; set; }

        public WatersModel(
            IStoreWrapper dataStore,
            ILogger<WatersModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
            Waters = new HashSet<Water>();
        }

        public async Task OnGetAsync()
        {
            var waters = await _dataStore.Waters.GetValuesAsync();

            foreach (var water in waters)
            {
                water.Longname = water.Longname.FirstUpper();
                Waters.Add(water);
            }
        }
    }
}
