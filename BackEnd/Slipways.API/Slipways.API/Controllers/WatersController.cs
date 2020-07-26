using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Application.Water;
using com.b_velop.Slipways.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class WatersController : BaseController
    {
        [HttpGet]
        [ActionName("GetAllWater"), Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<WaterDto>), 200)]
        [ProducesResponseType(204)]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WaterDto>>> List(CancellationToken cancellationToken)
            => await Mediator.Send(new List.Query(), cancellationToken);

        [HttpGet("details/{id}", Name = "GetWater")]
        [ActionName("GetWater"), Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<WaterDto>), 200)]
        [ProducesResponseType(204)]
        [AllowAnonymous]
        public async Task<ActionResult<WaterDto>> GetWater(Guid id, CancellationToken cancellationToken)
            => await Mediator.Send(new Details.Query { Id = id }, cancellationToken);

        [HttpPost]
        [ActionName("CreateWater"), Produces("application/json")]
        [ProducesResponseType(typeof(WaterDto), 201)]
        [ProducesResponseType(400)]
        [AllowAnonymous]
        public async Task<ActionResult<WaterDto>> CreateWater(Create.Command command, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtRoute("GetWater", new { id = result.Id, cancellationToken }, result);
        }
    }
}