using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Application.Marina;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class MarinasController : BaseController
    {

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<MarinaDto>>> List(CancellationToken cancellationToken)
            => await Mediator.Send(new List.Query(), cancellationToken);
    }
}
