using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.GrQl.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get() => Ok();
    }
}