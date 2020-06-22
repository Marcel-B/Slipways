using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Port
{
    public class CreateModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<CreateModel> _logger;

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public Guid WaterId { get; set; }

        public SelectList Waters { get; set; }

        [BindProperty]
        public Guid SlipwayId { get; set; }

        public SelectList Slipways { get; set; }

        [BindProperty]
        public b_velop.Slipways.Domain.Models.Marina Port { get; set; }

        public CreateModel(
            IStoreWrapper dataStore,
            ILogger<CreateModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            Waters = new SelectList(waters.OrderBy(_ => _.Longname), "Id", "Longname");
            var slipways = await _dataStore.Slipways.GetValuesAsync();
            Slipways = new SelectList(slipways, "Id", "Name");
        }

        public async Task<IActionResult> OnGetWaterBySlipwayIdAsync(
            [FromQuery]Guid id)
        {
            Console.WriteLine(id);
            var slipways = await _dataStore.Slipways.GetValuesAsync();
            var slipway = slipways.FirstOrDefault(_ => _.Id == id);
            var water = slipway.Water;
            var waters = await _dataStore.Waters.GetValuesAsync();
            Waters = new SelectList(waters.OrderBy(_ => _.Longname), "Id", "Longname", id);
            return new JsonResult(new { water.Id, water.Longname });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            Waters = new SelectList(waters.OrderBy(_ => _.Longname), "Id", "Longname");
            var slipways = await _dataStore.Slipways.GetValuesAsync();
            Slipways = new SelectList(slipways, "Id", "Name");
            if (!ModelState.IsValid)
            {
                Message = "Eingabefehler";
                return Page();
            }
            try
            {
                Port.WaterFk = WaterId;

                if (SlipwayId != Guid.Empty)
                {
                    var tmp = new List<b_velop.Slipways.Data.Models.Slipway>
                    {
                        slipways.First(_ => _.Id == SlipwayId)
                    };
                    Port.Slipways = tmp;
                }

                var result = await _dataStore.Ports.AddAsync(Port);
                if (result != null)
                {
                    Message = $"Marina '{Port.Name}' erfolgreich hinzugefügt";
                    return RedirectToPage("./Index");
                }
                else
                {
                    Message = $"Fehler beim hinzufügen von '{Port.Name}'";
                }
            }
            catch (Exception e)
            {
                _logger.LogError(6666, $"Unexpected error occurred while inserting '{Port.Name}'", e);
                Message = "Unerwarteter Fehler";
            }
            return Page();
        }
    }
}
