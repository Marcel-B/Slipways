using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Service
{
    public class CreateModel : PageModel
    {
        public class ManufacturerSelection
        {
            public Guid Id { get; set; }
            public bool Selected { get; set; }
        }

        private IStoreWrapper _dataStore;
        private ILogger<CreateModel> _logger;

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public b_velop.Slipways.Data.Models.Service Service { get; set; }

        [BindProperty]
        public Dictionary<string, ManufacturerSelection> Manufacturers { get; set; }

        public CreateModel(
            IStoreWrapper dataStore,
            ILogger<CreateModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
            Manufacturers = new Dictionary<string, ManufacturerSelection>();
        }

        public async Task OnGetAsync()
        {
            var manufacturers = await _dataStore.Manufacturers.GetValuesAsync();
            if (manufacturers != null)
            {
                foreach (var manufacturer in manufacturers)
                {
                    Manufacturers[manufacturer.Name] = new ManufacturerSelection { Id = manufacturer.Id, Selected = false };
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                foreach (var key in Manufacturers.Keys)
                {
                    if (Manufacturers[key].Selected)
                    {
                        var id = Manufacturers[key].Id;
                        Service.Manufacturers.Add(new Manufacturer { Id = id, Name = key });
                    }
                }
                var result = await _dataStore.Services.AddAsync(Service);
                if (result == null)
                {
                    Message = "Fehler beim erstellen des Services";
                    return Page();
                }
                return new RedirectToPageResult("./");
            }

            Message = "Eingabe ungültig";
            return Page();
        }
    }
}
