using System.Collections.Generic;
using System.Threading.Tasks;
using com.b_velop.Slipways.Application.Water;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class WatersController : BaseController
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<WaterDto>>> List()
            => await Mediator.Send(new List.Query());
    }
}