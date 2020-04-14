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
    public class StationController : ControllerBase
    {
        private readonly IRepositoryWrapper _rep;
        private readonly IMapper _mapper;
        private readonly ILogger<StationController> _logger;

        public StationController(
            IRepositoryWrapper rep,
            IMapper mapper,
            ILogger<StationController> logger)
        {
            _rep = rep;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> GetAsync(
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_GET_api_station_seconds", "Histogram").NewTimer())
            {
                var stations = await _rep.Station.SelectAllAsync(cancellationToken);
                var result = _mapper.Map<IEnumerable<StationDto>>(stations.OrderBy(_ => _.Longname));
                return Ok(result);
            }
        }

        // GET api/values/8177a148-5674-4b8f-8ded-050907f640f3
        [HttpGet("{id}")]
        public async Task<ActionResult<Station>> GetAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            using (Metrics.CreateHistogram($"slipways_api_duration_GET_api_station_id_seconds", "Histogram").NewTimer())
            {
                var station = await _rep.Station.SelectByIdAsync(id, cancellationToken);
                var result = _mapper.Map<StationDto>(station);
                return Ok(result);
            }
        }
    }
}
