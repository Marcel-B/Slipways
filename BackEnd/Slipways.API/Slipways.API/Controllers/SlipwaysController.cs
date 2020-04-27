using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Application.Slipway;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class SlipwaysController : BaseController
    {
        // GET: api/slipways
        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<SlipwayDto>> GetItAll(CancellationToken cancellationToken)
            => await Mediator.Send(new List.Query(), cancellationToken);

        // GET api/slipways/details/8177a148-5674-4b8f-8ded-050907f640f3
        [HttpGet("details/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<SlipwayDetailsDto>> GetAsync(
            Guid id,
            CancellationToken cancellationToken)
            => await Mediator.Send(new Details.Query{Id = id}, cancellationToken);

        //[HttpPost]
        //public async Task<ActionResult> PostAsync(
        //    SlipwayDto slipwayDto,
        //    CancellationToken cancellationToken)
        //{
        //    if (slipwayDto == null || string.IsNullOrWhiteSpace(slipwayDto.Name))
        //        return BadRequest("SlipwayDto is null or incorrect format");

        //    using (Metrics.CreateHistogram($"slipways_api_duration_POST_api_slipway_seconds", "Histogram").NewTimer())
        //    {
        //        var slipway = _mapper.Map<Slipway>(slipwayDto);
        //        var result = await _repository.Slipway.InsertAsync(slipway, cancellationToken);
        //        if (result == null)
        //        {
        //            return StatusCode(500);
        //        }

        //        if (slipwayDto.Extras != null)
        //        {
        //            var extras = new HashSet<SlipwayExtra>();
        //            foreach (var extra in slipwayDto?.Extras)
        //            {
        //                var slipwayExtra = new SlipwayExtra
        //                {
        //                    Id = Guid.NewGuid(),
        //                    Created = DateTime.Now,
        //                    ExtraFk = extra.Id,
        //                    SlipwayFk = result.Id,
        //                };
        //                extras.Add(slipwayExtra);
        //            }
        //            _ = await _repository.SlipwayExtra.InsertRangeAsync(extras, cancellationToken);
        //            slipwayDto.Id = slipway.Id;
        //            slipwayDto.Created = result.Created;
        //        }
        //        return Ok(slipwayDto);
        //    }
        //}

        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutAsync(
        //    Guid id,
        //    SlipwayDto slipwayDto,
        //    CancellationToken cancellationToken)
        //{
        //    using (Metrics.CreateHistogram($"slipways_api_duration_PUT_api_slipway_seconds", "Histogram").NewTimer())
        //    {
        //        var slipway = _mapper.Map<Slipway>(slipwayDto);

        //        if (slipway.Id != id)
        //            return BadRequest("IDs are not the same");

        //        var result = await _repository.Slipway.UpdateAsync(slipway, cancellationToken);
        //        if (result == null)
        //        {
        //            _logger.LogError(6660, $"Error occurred while updating Slipway '{slipwayDto.Name} : {id}'");
        //            return new StatusCodeResult(500);
        //        }

        //        // TODO - update not really works atm
        //        var extras = new HashSet<SlipwayExtra>();
        //        foreach (var extra in slipwayDto.Extras)
        //        {
        //            var slipwayExtra = new SlipwayExtra
        //            {
        //                Created = DateTime.Now,
        //                ExtraFk = extra.Id,
        //                SlipwayFk = result.Id,
        //            };
        //            extras.Add(slipwayExtra);
        //        }
        //        _ = await _repository.SlipwayExtra.UpdateRangeAsync(extras, cancellationToken);
        //        // ************

        //        slipwayDto.Id = slipway.Id;
        //        slipwayDto.Created = result.Created;
        //        slipwayDto.Updated = result.Updated;

        //        return Ok(slipwayDto);
        //    }
        //}

        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteSlipwayAsync(
        //    Guid id,
        //    CancellationToken cancellationToken)
        //{
        //    if (id == Guid.Empty)
        //        return BadRequest("Slipway ID is not valid");

        //    using (Metrics.CreateHistogram($"slipways_api_duration_DELETE_api_slipway_seconds", "Histogram").NewTimer())
        //    {
        //        _logger.LogInformation(5555, $"Try to delete Slipway '{id}'");
        //        var slipway = await _repository.Slipway.DeleteAsync(id, cancellationToken);
        //        var result = _mapper.Map<SlipwayDto>(slipway);
        //        return Ok(result);
        //    }
        //}
    }
}
