using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace com.b_velop.Slipways.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortsController : ControllerBase
    {
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private ILogger<PortsController> _logger;

        public PortsController(
            IRepositoryWrapper repository,
            IMapper mapper,
            ILogger<PortsController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostPortAsync(
            PortDto portDto,
            CancellationToken cancellationToken)
        {
            if (portDto == null || string.IsNullOrWhiteSpace(portDto.Name))
            {
                _logger.LogWarning($"Error occurred while POST new Port. No name provided");
                return BadRequest("Value null or incorrect format");
            }

            var port = _mapper.Map<Port>(portDto);
            var slipways = port.Slipways;
            port.Slipways = null;
            port = await _repository.Port.InsertAsync(port, cancellationToken, false);
            if (port == null)
            {
                _logger.LogWarning(5555, $"Error occurred while inserting Port '{portDto.Name}'");
                return new StatusCodeResult(500);
            }
            _repository.Context.SaveChanges();
            portDto.Id = port.Id;
            if (slipways != null)
            {
                foreach (var slipway in slipways)
                {
                    var tmp = await _repository.Slipway.SelectByIdAsync(slipway.Id, cancellationToken);
                    tmp.PortFk = port.Id;
                }
            }
            _repository.SaveChanges();
            return new JsonResult(portDto);
        }
    }
}
