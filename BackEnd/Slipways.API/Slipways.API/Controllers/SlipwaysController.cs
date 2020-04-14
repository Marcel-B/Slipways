using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace com.b_velop.Slipways.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlipwaysController : ControllerBase
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<SlipwaysController> _logger;

        public SlipwaysController(
            IRepositoryWrapper repository,
            IMapper mapper,
            ILogger<SlipwaysController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/slipways
        [HttpGet]
        public async Task<IActionResult> GetAsync(
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_GET_api_slipway_seconds", "Histogram").NewTimer())
            {
                var slipways = await _repository.Slipway.SelectAllAsync(cancellationToken);
                //foreach (var slipway in slipways)
                //{
                //    var slExtras = await _repository.SlipwayExtra.SelectByConditionAsync(_ => _.SlipwayFk == slipway.Id);
                //    foreach (var slExtra in slExtras)
                //    {
                //        var extra = await _repository.Extra.SelectByIdAsync(slExtra.ExtraFk);
                //        slipway.Extras.Add(extra);
                //    }
                //}
                var result = _mapper.Map<IEnumerable<SlipwayForListDto>>(slipways.OrderBy(_ => _.Name));
                return Ok(result);
            }
        }

        // GET api/slipway/8177a148-5674-4b8f-8ded-050907f640f3
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_GET_api_slipway_id_seconds", "Histogram").NewTimer())
            {
                var slipway = await _repository.Slipway.SelectByIdAsync(id, cancellationToken);
                var slipwayDto = _mapper.Map<SlipwayDto>(slipway);
                return Ok(slipwayDto);
            }
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(
            SlipwayDto slipwayDto,
            CancellationToken cancellationToken)
        {
            if (slipwayDto == null || string.IsNullOrWhiteSpace(slipwayDto.Name))
                return BadRequest("SlipwayDto is null or incorrect format");

            using (Metrics.CreateHistogram($"slipways_api_duration_POST_api_slipway_seconds", "Histogram").NewTimer())
            {
                var slipway = _mapper.Map<Slipway>(slipwayDto);
                var result = await _repository.Slipway.InsertAsync(slipway, cancellationToken);
                if (result == null)
                {
                    return StatusCode(500);
                }

                if (slipwayDto.Extras != null)
                {
                    var extras = new HashSet<SlipwayExtra>();
                    foreach (var extra in slipwayDto?.Extras)
                    {
                        var slipwayExtra = new SlipwayExtra
                        {
                            Id = Guid.NewGuid(),
                            Created = DateTime.Now,
                            ExtraFk = extra.Id,
                            SlipwayFk = result.Id,
                        };
                        extras.Add(slipwayExtra);
                    }
                    _ = await _repository.SlipwayExtra.InsertRangeAsync(extras, cancellationToken);
                    slipwayDto.Id = slipway.Id;
                    slipwayDto.Created = result.Created;
                }
                return Ok(slipwayDto);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(
            Guid id,
            SlipwayDto slipwayDto,
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_PUT_api_slipway_seconds", "Histogram").NewTimer())
            {
                var slipway = _mapper.Map<Slipway>(slipwayDto);

                if (slipway.Id != id)
                    return BadRequest("IDs are not the same");

                var result = await _repository.Slipway.UpdateAsync(slipway, cancellationToken);
                if (result == null)
                {
                    _logger.LogError(6660, $"Error occurred while updating Slipway '{slipwayDto.Name} : {id}'");
                    return new StatusCodeResult(500);
                }

                // TODO - update not really works atm
                var extras = new HashSet<SlipwayExtra>();
                foreach (var extra in slipwayDto.Extras)
                {
                    var slipwayExtra = new SlipwayExtra
                    {
                        Created = DateTime.Now,
                        ExtraFk = extra.Id,
                        SlipwayFk = result.Id,
                    };
                    extras.Add(slipwayExtra);
                }
                _ = await _repository.SlipwayExtra.UpdateRangeAsync(extras, cancellationToken);
                // ************

                slipwayDto.Id = slipway.Id;
                slipwayDto.Created = result.Created;
                slipwayDto.Updated = result.Updated;

                return Ok(slipwayDto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSlipwayAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
                return BadRequest("Slipway ID is not valid");

            using (Metrics.CreateHistogram($"slipways_api_duration_DELETE_api_slipway_seconds", "Histogram").NewTimer())
            {
                _logger.LogInformation(5555, $"Try to delete Slipway '{id}'");
                var slipway = await _repository.Slipway.DeleteAsync(id, cancellationToken);
                var result = _mapper.Map<SlipwayDto>(slipway);
                return Ok(result);
            }
        }
    }
}
