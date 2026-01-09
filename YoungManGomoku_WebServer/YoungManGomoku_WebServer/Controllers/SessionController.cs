using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System;

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
		private readonly GameRoomManager _gameroomManager;

        private readonly ILogger<SessionController> _logger;

		public SessionController(ILogger<SessionController> logger, ApplicationDBContext context, ServerManager serverManager, GameRoomManager gameroomManager)
		{
			_logger = logger;
			_context = context;
			_serverManager = serverManager;
			_gameroomManager = gameroomManager;
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

            _logger.LogTrace($"[{DateTime.Now}] [Session Controller] Response Game End By : {idToken}");

            // 게임 룸에 있지도 않으면서 무슨 게임 종료 결과를 달라는거야
            if (_gameroomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom room) == false)
                return BadRequest("Not in game");

            return Ok(new SC_ResponseStringDTO("Close SessionSuccess", true));
		}
	}
}
