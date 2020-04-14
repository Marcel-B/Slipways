using com.b_velop.Slipways.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Areas.Admin.Pages.Water
{
    public class CreateModel : PageModel
    {
        [TempData]
        public string Message { get; set; }

        [BindProperty]
        public b_velop.Slipways.Data.Models.Water Water { get; set; }

        private IStoreWrapper _dataStore;

        public CreateModel(
            IStoreWrapper dataStore)
        {
            _dataStore = dataStore;
        }

        public void OnGet()
        {
        }

        public async Task<ActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Message = "Eingabe ung¨¹ltig";
                return Page();
            }
            var reuslt = await _dataStore.Waters.AddAsync(Water);

            if (reuslt != null)
                return RedirectToPage("./Index");

            Message = "Fehler beim erstellen des Gew?ssers";
            return Page();
        }
    }
}
