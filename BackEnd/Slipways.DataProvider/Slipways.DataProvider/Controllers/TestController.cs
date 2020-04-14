using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.DataProvider.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : Controller
    {
        // GET: api/values
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
