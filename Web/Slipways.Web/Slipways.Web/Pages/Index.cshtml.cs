using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Pages
{
    public class SlipwaysModel
    {
        public HashSet<Slipway> Slipways { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IStoreWrapper _dataStore;
        private readonly ILogger<IndexModel> _logger;

        public SlipwaysModel Slipways { get; set; }

        public IndexModel(
            IStoreWrapper dataStore,
            ILogger<IndexModel> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            try
            {
                Slipways = new SlipwaysModel();
                var slipways = await _dataStore.Slipways.GetValuesAsync();
                foreach (var slipway in slipways)
                {
                    slipway.Water.Longname = slipway.Water.Longname.FirstUpper();
                }
                if (slipways != null)
                    Slipways.Slipways = slipways.OrderBy(_ => _.Name).ToHashSet();
                else
                    Slipways.Slipways = new HashSet<Slipway>();
            }
            catch (Exception e)
            {
                _logger.LogError(6666, e, $"Error occurred while GET index page");
                Redirect("/Error");
            }
        }

        public async Task<IActionResult> OnGetFilter(
            [FromQuery] string search)
        {
            try
            {
                Slipways = new SlipwaysModel();
                var slipways = await _dataStore.Slipways.GetValuesAsync();

                if (slipways == null)
                    return Page();


                IEnumerable<Slipway> searchResult;
                searchResult = slipways;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    searchResult = searchResult.Where(_ => _.Name.ToLower().Contains(search) || _.City.ToLower().Contains(search) || _.Water.Longname.ToLower().Contains(search)).Distinct();
                }
                searchResult = searchResult.OrderBy(_ => _.Name);
                foreach (var slipway in searchResult)
                {
                    slipway.Water.Longname = slipway.Water.Longname.FirstUpper();
                }
                Slipways.Slipways = new HashSet<Slipway>(searchResult);
                var partial = Partial("_SlipwayTable", Slipways);
                return partial;
            }
            catch (Exception e)
            {
                _logger.LogError(6666, e, $"Error occurred while GET filter index page");
                return Redirect("/Error");
            }
        }
    }
}
