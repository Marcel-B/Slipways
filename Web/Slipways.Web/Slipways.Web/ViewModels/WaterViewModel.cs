using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Slipways.Web.Contracts;
using com.b_velop.Slipways.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.ViewModels
{
    public class WaterViewModel
    {
        //private ISlipwayService _service;
        //private ILogger<WaterViewModel> _logger;

        [BindProperty]
        public Water Water { get; set; }

        [BindProperty]
        public HashSet<Water> Waters { get; set; }

        public async Task<WaterDto> SaveWaterAsync(
            IWaterService service)
        {
            var waterDto = new WaterDto
            {
                Longname = Water.Longname,
                Shortname = Water.Shortname
            };
            var result = await service.InsertAsync(waterDto);
            if (result != null)
            {
                return result;
            }
            return null;
        }
    }
}
