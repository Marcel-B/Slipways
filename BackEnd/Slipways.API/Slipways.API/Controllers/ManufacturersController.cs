using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Application.Manufacturer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class ManufacturersController : BaseController
    {
        //private readonly IRepositoryWrapper _repository;
        //private readonly IMapper _mapper;
        //private readonly ILogger<ManufacturersController> _logger;

        //public ManufacturersController(
        //    IRepositoryWrapper repository,
        //    IMapper mapper,
        //    ILogger<ManufacturersController> logger)
        //{
        //    _repository = repository;
        //    _mapper = mapper;
        //    _logger = logger;
        //}
        //
        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<ManufacturerDto>> List(
            CancellationToken cancellationToken)
            => await Mediator.Send(new List.Query(), cancellationToken);

        //// GET api/values/5
        //[HttpGet("{id}", Name = "GetManufacturer")]
        //public async Task<IActionResult> Get(
        //    Guid id,
        //    CancellationToken cancellationToken)
        //{
        //    var manufacturer = await _repository.Manufacturer.SelectByIdAsync(id, cancellationToken);
        //    var result = _mapper.Map<ManufacturerDto>(manufacturer);
        //    return Ok(result);
        //}

        //// POST api/manufacturers
        //[HttpPost]
        //public async Task<IActionResult> PostAsync(
        //    ManufacturerDto manufacturerDto,
        //    CancellationToken cancellationToken)
        //{
        //    if (manufacturerDto == null || string.IsNullOrWhiteSpace(manufacturerDto.Name))
        //    {
        //        _logger.LogWarning(5000, $"Error occurred while POST Manufacturer - Value null or incorrect format");
        //        return BadRequest("Value null or incorrect format");
        //    }

        //    var manufacturer = _mapper.Map<Manufacturer>(manufacturerDto);
        //    var result = await _repository.Manufacturer.InsertAsync(manufacturer, cancellationToken);
        //    manufacturerDto.Id = result.Id;
        //    return CreatedAtRoute("GetManufacturer", new { manufacturer.Id }, manufacturerDto);
        //}

        //// PUT api/manufacturers/5
        //[HttpPut("{id}")]
        //public void Put(
        //    int id,
        //    [FromBody]string value)
        //{
        //}

        //// DELETE api/manufacturers/5
        //[HttpDelete("{id}")]
        //public void Delete(
        //    int id)
        //{
        //}
    }
}
