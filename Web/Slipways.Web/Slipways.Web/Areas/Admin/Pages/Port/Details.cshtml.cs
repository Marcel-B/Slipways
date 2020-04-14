using System;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Port
{
    public class DetailsModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<DetailsModel> _logger;

        public b_velop.Slipways.Data.Models.Port Port { get; set; }

        [TempData]
        public string Message { get; set; }

        public DetailsModel(
            IStoreWrapper dataStore,
            ILogger<DetailsModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetAsync(
            Guid id)
        {
            var ports = await _dataStore.Ports.GetValuesAsync();
            var port = ports.FirstOrDefault(_ => _.Id == id);
            Port = port;
        }
    }
}
