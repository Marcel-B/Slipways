using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Slipway
{
    public class CreateModel : PageModel
    {
        public class ExtraSelection
        {
            public Guid Id { get; set; }
            public bool Selected { get; set; }
        }

        private readonly IStoreWrapper _dataStore;

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public Dictionary<string, ExtraSelection> Extras { get; set; }

        [BindProperty]
        public b_velop.Slipways.Data.Models.Slipway Slipway { get; set; }

        [BindProperty]
        public bool ParkingPlace { get; set; }

        [BindProperty]
        public bool Steg { get; set; }

        [BindProperty]
        public bool CampingArea { get; set; }

        [BindProperty]
        public Guid WaterId { get; set; }

        public SelectList Waters { get; set; }

        public CreateModel(
            IStoreWrapper dataStore)
        {
            _dataStore = dataStore;
            Extras = new Dictionary<string, ExtraSelection>();
        }

        public async Task OnGetAsync()
        {
            //Slipway = new b_velop.Slipways.Data.Models.Slipway();
            var waters = await _dataStore.Waters.GetValuesAsync();
            Waters = new SelectList(waters.OrderBy(_ => _.Longname).Select(_ => new { _.Id, Name = _.Longname.FirstUpper() }), "Id", "Name");
            var extras = await _dataStore.Extras.GetValuesAsync();
            foreach (var extra in extras)
                Extras[extra.Name] = new ExtraSelection { Id = extra.Id, Selected = false };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            if (WaterId == Guid.Empty)
            {
                Message = "Bitte wähle ein Gewässer aus";
                Waters = new SelectList(waters, "Id", "Longname");
                return Page();
            }
            var water = waters.First(_ => _.Id == WaterId);
            Slipway.Water = water;
            Slipway.WaterFk = WaterId;

            if (ModelState.IsValid)
            {
                foreach (var extra in Extras)
                {
                    if (extra.Value.Selected)
                        Slipway.Extras.Add(new Extra { Id = extra.Value.Id });
                }

                var slipways = await _dataStore.Slipways.AddAsync(Slipway);

                if (slipways != null)
                    return RedirectToPage("./Index");

                Message = "Fehler beim erstellen der Slipanlage";
            }
            else
            {
                Message = "Eingabe ungültig";
            }

            Waters = new SelectList(waters, "Id", "Longname");

            return Page();
        }
    }
}