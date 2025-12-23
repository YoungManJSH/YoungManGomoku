using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Sessions;
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
            PlayerSession player = _serverManager.GetPlayerSession(idToken);

			if (player == null)
			{
				// 인증 정보는 왔지만 유효한 세션이 아니다
				return Unauthorized($"[{idToken}] Player Session Not Found.");
			}

            player.LastRequestTime = player.LastHeartbeatTime = DateTime.UtcNow;

            return Ok("HeartBeat Success");
		}
	}
}