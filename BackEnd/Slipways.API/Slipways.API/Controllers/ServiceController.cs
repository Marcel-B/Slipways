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
    public class ServiceController : ControllerBase
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceController> _logger;

        public ServiceController(
            IRepositoryWrapper repository,
            IMapper mapper,
            ILogger<ServiceController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> Get(
            CancellationToken cancellationToken)
        {
            var services = await _repository.Service.SelectAllAsync(cancellationToken);
            var servicesDto = _mapper.Map<IEnumerable<ServiceDto>>(services.OrderBy(_ => _.Name));
            return Ok(servicesDto);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(
           ServiceDto serviceDto,
           CancellationToken cancellationToken)
        {
            if (serviceDto == null || string.IsNullOrEmpty(serviceDto.Name))
                return BadRequest("ServiceDto is null or format error");

            using (Metrics.CreateHistogram($"slipways_api_duration_POST_api_service_seconds", "Histogram").NewTimer())
            {
                var service = _mapper.Map<Service>(serviceDto);
                var result = await _repository.Service.InsertAsync(service, cancellationToken);
                if (result != null)
                {
                    if (serviceDto.Manufacturers != null)
                    {
                        var manufacturers = new HashSet<ManufacturerService>();
                        foreach (var manufacturer in serviceDto.Manufacturers)
                        {
                            var manufacturerService = new ManufacturerService
                            {
                                Id = Guid.NewGuid(),
                                Created = DateTime.Now,
                                ServiceFk = service.Id,
                                ManufacturerFk = manufacturer.Id,
                            };
                            manufacturers.Add(manufacturerService);
                        }
                        _ = await _repository.ManufacturerServices.InsertRangeAsync(manufacturers, cancellationToken);
                    }
                    serviceDto.Id = service.Id;
                    return Ok(serviceDto);
                }
                return StatusCode(500);
            }
        }
    }
}
