using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            // 서버 살아있는지 테스트용이라 그 어떤 유저 관련 처리도 없음
            // 유저 생존 처리는 Heart Beat에서
            return Ok("pong");
        }
    }
}
