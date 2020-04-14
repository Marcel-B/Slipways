using System;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Water
{
    public class DetailsModel : PageModel
    {
        private IStoreWrapper _dataStore;

        [BindProperty]
        public b_velop.Slipways.Data.Models.Water Water { get; set; }

        public DetailsModel(
            IStoreWrapper dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task OnGet(
            Guid id)
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            Water = waters.FirstOrDefault(_ => _.Id == id);
        }
    }
}
