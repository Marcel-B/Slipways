using System;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Slipway
{
    public class DetailsModel : PageModel
    {
        private IStoreWrapper _dataStore;

        [BindProperty]
        public b_velop.Slipways.Data.Models.Slipway Slipway { get; set; }

        public DetailsModel(
            IStoreWrapper dataStore)
        {
            _dataStore = dataStore;
        }
        public async Task<IActionResult> OnGetAsync(
            Guid id)
        {
            var slipways = await _dataStore.Slipways.GetValuesAsync();
            Slipway = slipways.FirstOrDefault(_ => _.Id == id);
            return Page();
        }
    }
}
