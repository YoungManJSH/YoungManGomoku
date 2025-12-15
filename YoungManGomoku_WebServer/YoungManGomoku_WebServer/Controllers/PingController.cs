using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        private readonly ServerManager _serverManager;
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("pong");
        }
    }
}
