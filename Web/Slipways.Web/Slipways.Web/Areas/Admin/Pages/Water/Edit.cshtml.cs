using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using com.b_velop.Slipways.Web.Contracts;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Water
{
    public class EditModel : PageModel
    {
        private IStoreWrapper _dataStore;
        private ILogger<EditModel> _logger;

        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public b_velop.Slipways.Data.Models.Water Water { get; set; }

        public EditModel(
            IStoreWrapper dataStore,
            ILogger<EditModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetAsync(
            Guid id)
        {
            var waters = await _dataStore.Waters.GetValuesAsync();
            Water = waters.FirstOrDefault(_ => _.Id == id);
        }

        public async Task<ActionResult> OnPostAsync()
        {
            var water = await _dataStore.Waters.UpdateAsync(Water, Water.Id);
            
            if(water == null)
            {
                Message = "Fehler beim ?ndern des Gew?ssers";
                return Page();
            }

            Message = $"Gew?ser '{Water.Longname}' ge?dert.";
            return new RedirectToPageResult("./Index");
        }
    }
}
