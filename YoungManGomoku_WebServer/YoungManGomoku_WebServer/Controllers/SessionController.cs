using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class SessionController : ControllerBase
	{
		private ApplicationDBContext _context;
		private readonly ServerManager _serverManager;

		private readonly ILogger<SessionController> _logger;

		public SessionController(ILogger<SessionController> logger, ApplicationDBContext context, ServerManager serverManager)
		{
			_logger = logger;
			_context = context;
			_serverManager = serverManager;
		}


		[HttpPost("Close")]
		public IActionResult SessionClose([FromBody] string idToken)
		{
			PlayerSession? player = _serverManager.GetPlayerSession(idToken);

			if (player == null)
			{
				// 인증 정보는 왔지만 유효한 세션이 아니다
				return Unauthorized($"[{idToken}] Player Session Not Found.");
			}

			if (_serverManager.CloseSession(player.Account.UID) == false)
				return Unauthorized($"[{idToken}] Player Session Close Failed.");
			


			return Ok(new SC_ResponseStringDTO("Close SessionSuccess", true));
		}
	}
}
