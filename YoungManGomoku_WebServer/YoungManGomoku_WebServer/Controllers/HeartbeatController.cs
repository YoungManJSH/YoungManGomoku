using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class HeartbeatController : ControllerBase
	{
		private ApplicationDBContext _context;
		private readonly ServerManager _serverManager;

		private readonly ILogger<HeartbeatController> _logger;

		public HeartbeatController(ILogger<HeartbeatController> logger, ApplicationDBContext context, ServerManager serverManager)
		{
			_logger = logger;
			_context = context;
			_serverManager = serverManager;
		}
		[HttpPost]
		public IActionResult HeartBeat([FromBody] string idToken)
		{
			
			return Ok("HeartBeat Success");
		}
	}
}