using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WatersController : ControllerBase
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<WatersController> _logger;

        public WatersController(
            IRepositoryWrapper repository,
            IMapper mapper,
            ILogger<WatersController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/water
        [HttpGet]
        public async Task<IActionResult> GetAsync(
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_GET_api_water_seconds", "Histogram").NewTimer())
            {
                var result = await _repository.Water.SelectAllAsync(cancellationToken);
                var waterDto = _mapper.Map<IEnumerable<WaterDto>>(result.OrderBy(_ => _.Longname));
                return Ok(waterDto);
            }
        }

        // GET api/water/8177a148-5674-4b8f-8ded-050907f640f3
        [HttpGet("{id}", Name = "GetWater")]
        public async Task<IActionResult> GetWaterAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_GET_api_water_id_seconds", "Histogram").NewTimer())
            {
                var water = await _repository.Water.SelectByIdAsync(id, cancellationToken);
                var waterDto = _mapper.Map<WaterDto>(water);
                return Ok(waterDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(
            WaterDto waterDto,
            CancellationToken cancellationToken)
        {
            if (waterDto == null || string.IsNullOrWhiteSpace(waterDto.Name))
                return BadRequest("WaterDto is null or incorrect format");

            using (Metrics.CreateHistogram($"slipways_api_duration_POST_api_water_seconds", "Histogram").NewTimer())
            {
                var water = _mapper.Map<Water>(waterDto);
                var result = await _repository.Water.InsertAsync(water, cancellationToken);
                waterDto.Id = result.Id;
                return CreatedAtRoute("GetWater", new { waterDto.Id }, waterDto);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(
            Guid id,
            WaterDto waterDto,
            CancellationToken cancellationToken)
        {
            if (id != waterDto.Id)
            {
                _logger.LogWarning(5555, $"Unable to update Water, IDs are not the same '{id} : {waterDto.Id}'");
                return BadRequest("IDs are not the same");
            }

            using (Metrics.CreateHistogram($"slipways_api_duration_PUT_api_water_seconds", "Histogram").NewTimer())
            {
                var water = await _repository.Water.SelectByIdAsync(id, cancellationToken);
                water.Longname = waterDto.Name;
                water.Shortname = waterDto.Shortname;
                var result = await _repository.Water.UpdateAsync(water);
                return Ok(waterDto);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning(5000, $"Unable to delete Water - no ID");
                return BadRequest("Id is incorrect");
            }

            using (Metrics.CreateHistogram($"slipways_api_duration_DELETE_api_water_seconds", "Histogram").NewTimer())
            {
                var result = await _repository.Water.DeleteAsync(id, cancellationToken);
                var waterDto = _mapper.Map<WaterDto>(result);
                return Ok(waterDto);
            }
        }
    }
}