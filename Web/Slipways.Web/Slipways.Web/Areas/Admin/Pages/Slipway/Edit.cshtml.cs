using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Slipway
{
    public class EditModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<EditModel> _logger;

        public class ExtraSelection
        {
            public Guid Id { get; set; }
            public bool Selected { get; set; }
        }

        [BindProperty]
        public Dictionary<string, ExtraSelection> Extras { get; set; }

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public b_velop.Slipways.Data.Models.Slipway Slipway { get; set; }

        [BindProperty]
        public string SelectedWaterId { get; set; }

        [BindProperty]
        public string WaterId { get; set; }

        public SelectList Waters { get; set; }

        public int Idx { get; set; }

        public EditModel(
            IStoreWrapper dataStore,
            ILogger<EditModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
            Extras = new Dictionary<string, ExtraSelection>();
        }

        public async Task OnGetAsync(
            Guid id)
        {
            var slipways = await _dataStore.Slipways.GetValuesAsync();
            Slipway = slipways.FirstOrDefault(_ => _.Id == id);
            SelectedWaterId = Slipway.Water.Id.ToString();
            var extras = await _dataStore.Extras.GetValuesAsync();
            Waters = await GetSelectListAsync();

            foreach (var extra in extras)
                Extras[extra.Name] = new ExtraSelection { Id = extra.Id, Selected = Slipway.Extras.FirstOrDefault(_ => _.Id == extra.Id) != null  };
        }

        private async Task<SelectList> GetSelectListAsync()
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            return new SelectList(waters.OrderBy(_ => _.Longname), "Id", "Longname");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            var water = waters.First(_ => _.Id == Guid.Parse(WaterId));
            Slipway.Water = water;
            foreach (var key in Extras.Keys)
            {
                if (Extras[key].Selected)
                {
                    var id = Extras[key].Id;
                    Slipway.Extras.Add(new b_velop.Slipways.Data.Models.Extra { Id = id, Name = key });
                }
            }
            var slipways = await _dataStore.Slipways.UpdateAsync(Slipway, Slipway.Id);
            if (slipways == null)
            {
                Message = "Ändern der Slipanlage fehlgeschlagen";
                Waters = await GetSelectListAsync();
                return Page();
            }
            Message = $"Slipanlage '{Slipway.Name}' erfolgreich geändert.";
            return new RedirectToPageResult("./Index");
        }
    }
}
