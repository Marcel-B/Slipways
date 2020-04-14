using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web
{
    public class PortDetailsModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ISecretProvider _secret;
        private ILogger<PortDetailsModel> _logger;

        [BindProperty]
        public Slipways.Data.Models.Port Port { get; set; }

        [BindProperty]
        public string ApiKey { get; set; }

        public PortDetailsModel(
            IStoreWrapper dataStore,
            ISecretProvider secret,
            ILogger<PortDetailsModel> logger)
        {
            _dataStore = dataStore;
            _secret = secret;
            _logger = logger;
        }

        public async Task OnGetAsync(
            Guid id)
        {
            ApiKey = _secret.GetSecret("google_maps_secret");
            var ports = await _dataStore.Ports.GetValuesAsync();
            var port = ports.First(_ => _.Id == id);
            Port = port;
        }
    }
}