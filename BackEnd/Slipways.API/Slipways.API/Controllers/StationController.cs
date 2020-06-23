using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Application.Station;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class StationController : BaseController
    {
        // GET: api/station
        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<StationDto>> GetItAll(CancellationToken cancellationToken)
            => await Mediator.Send(new List.Query(), cancellationToken);
        
        // GET api/station/details/8177a148-5674-4b8f-8ded-050907f640f3
        [HttpGet("details/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<StationDetailsDto>> GetAsync(
            Guid id,
            CancellationToken cancellationToken)
            => await Mediator.Send(new Details.Query{Id = id}, cancellationToken);
    }
}
