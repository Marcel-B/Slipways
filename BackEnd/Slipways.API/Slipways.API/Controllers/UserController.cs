using System.Threading.Tasks;
using com.b_velop.Slipways.Application.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace com.b_velop.Slipways.API.Controllers
{
    public class UserController : BaseController
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Login(Login.Query query)
            => await Mediator.Send(query);
        
        [HttpGet]
        public async Task<ActionResult<UserDto>> CurrentUser()
            => await Mediator.Send(new CurrentUser.Query());
    }
}