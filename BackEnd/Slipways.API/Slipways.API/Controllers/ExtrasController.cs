using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExtrasController : ControllerBase
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ExtrasController> _logger;

        public ExtrasController(
            IRepositoryWrapper repository,
            IMapper mapper,
            ILogger<ExtrasController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var extras = await _repository.Extra.SelectAllAsync(cancellationToken);
            var result = _mapper.Map<IEnumerable<ExtraDto>>(extras);
            return Ok(result);
        }

        [HttpGet("{id}", Name = "GetExtra")]
        public async Task<IActionResult> GetAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var extra = await _repository.Extra.SelectByIdAsync(id, cancellationToken);
            var result = _mapper.Map<ExtraDto>(extra);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(
            ExtraDto extraDto,
            CancellationToken cancellationToken)
        {
            if (extraDto == null || string.IsNullOrWhiteSpace(extraDto.Name))
                return BadRequest("Extra has not the correct format or is null");

            using (Metrics.CreateHistogram($"slipways_api_duration_POST_api_extra_seconds", "Histogram").NewTimer())
            {
                var extra = _mapper.Map<Extra>(extraDto);
                var result = await _repository.Extra.InsertAsync(extra, cancellationToken);
                if (result == null)
                {
                    _logger.LogError(6600, $"Error occurred while inserting Extra '{extraDto.Name}'");
                    return new StatusCodeResult(500);
                }
                extraDto.Id = result.Id;
                return CreatedAtRoute("GetExtra", new { extra.Id }, extraDto);
            }
        }
    }
}