using System.Collections.Generic;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Service
{
    public class IndexModel : PageModel
    {
        private IStoreWrapper _dataStore;

        [BindProperty]
        public HashSet<b_velop.Slipways.Data.Models.Service> Services { get; set; }

        public IndexModel(
            IStoreWrapper dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task OnGetAsync()
        {
            Services = await _dataStore.Services.GetValuesAsync();
        }
    }
}
