using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("pong");
        }
    }
}
