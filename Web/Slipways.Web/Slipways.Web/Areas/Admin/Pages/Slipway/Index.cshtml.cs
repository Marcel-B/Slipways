using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Slipways
{
    public class IndexModel : PageModel
    {
        [TempData]
        public string Message { get; set; }

        private readonly IStoreWrapper _dataStore;
        private readonly ILogger<IndexModel> _logger;

        public HashSet<b_velop.Slipways.Data.Models.Slipway> Slipways { get; set; }

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
            var slipways = await _dataStore.Slipways.RemoveAsync(id);
            if (slipways == null)
            {
                _logger.LogWarning($"RemoveAsync return null value");
            }
            else
            {
                Slipways = slipways;
                var slipway = slipways.FirstOrDefault(_ => _.Id == id);
                Message = $"Slipanlage {slipway?.Name} gelöscht";
            }
        }

        public async Task OnGetAsync()
        {
            var slipways = await _dataStore.Slipways.GetValuesAsync();
            Slipways = slipways.OrderBy(_ => _.Name).ToHashSet();
        }
    }
}