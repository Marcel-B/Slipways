using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Water
{
    public class IndexModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<IndexModel> _logger;

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public HashSet<b_velop.Slipways.Data.Models.Water> Waters { get; set; }

        public IndexModel(
            IStoreWrapper dataStore,
            ILogger<IndexModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetDeleteAsync(
            Guid id)
        {
            if (id != Guid.Empty)
                Waters = await _dataStore.Waters.RemoveAsync(id);
        }

        public async Task OnGetAsync()
        {
            Waters = await _dataStore.Waters.GetValuesAsync();
        }
    }
}
