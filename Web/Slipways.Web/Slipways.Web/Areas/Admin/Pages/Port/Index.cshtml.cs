using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Port
{
    public class IndexModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<IndexModel> _logger;

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public HashSet<b_velop.Slipways.Domain.Models.Marina> Ports { get; set; }

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
            var ports = await _dataStore.Ports.RemoveAsync(id);
            if (ports == null)
            {
                _logger.LogWarning($"RemoveAsync return null value");
            }
            else
            {
                Ports = ports;
                var port = ports.FirstOrDefault(_ => _.Id == id);
                Message = $"Marina {port?.Name} gelöscht";
            }
        }

        public async Task OnGetAsync()
        {
            var ports = await _dataStore.Ports.GetValuesAsync();
            Ports = ports ?? new HashSet<b_velop.Slipways.Domain.Models.Marina>();
        }
    }
}
