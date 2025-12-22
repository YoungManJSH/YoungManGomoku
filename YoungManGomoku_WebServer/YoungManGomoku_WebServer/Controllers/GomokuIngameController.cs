using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.TypeEnum.InGame;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GomokuIngameController : ControllerBase
    {
        // 게임 결과 DB 기록용
        private ApplicationDBContext _context;

        private readonly ILogger<GomokuIngameController> _logger;
        private readonly ServerManager _serverManager;
        private readonly GameRoomManager _gameRoomManager;

        public GomokuIngameController(ILogger<GomokuIngameController> logger, ApplicationDBContext context, ServerManager serverManager, GameRoomManager gameRoomManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
            _gameRoomManager = gameRoomManager;
        }

		[HttpPost("GameStart")]
		public IActionResult GameStart([FromBody] string idToken)
		{
			// 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
			ulong uid = _serverManager.GetPlayerUID(idToken);

			if (uid == 0)
				return BadRequest("미접속 유저임");

			// 네 이놈 게임 룸에 있지도 않은 주제에 게임 시작이라고 뻥카를 쳐?
			if (_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom room) == false)
				return BadRequest("Not in game");


			// 아직 이 방 게임 대기 중이 아닌데??? 미쳐버린거냐
			if (room.State != GameRoomState.Waiting)
			{
				return BadRequest("Not Game Wait");
			}

			// 이제 await 걸고
			// 상대방도 게임시작 요청을 해서 둘 다 게임 시작이 되면 게임 룸 쪽에서 await task를 반환?

			// await 어쩌고


			// 보낼 정보 : 상대방 착수했음 { 상대방의 착수 좌표, 결정된 상대방의 타이머, 게임 종료 여부 (DTO) } [착수 유저의 상대편]

			return Ok("Game Start Success"); // 임시 result
		}


		[HttpPost("PlaceStone")]
		public IActionResult PlaceStone([FromBody] CS_PlaceStoneDTO userPlaceStoneDTO)
		{
            // 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
			ulong uid = _serverManager.GetPlayerUID(userPlaceStoneDTO.IDToken);

            if (uid == 0)
                return BadRequest("미접속 유저");

            // 네이놈 게임 룸에 있지도 않은 주제에 착수 요청을 해?
			if (_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom room) == false)
				return BadRequest("Not in game");

			PlaceStoneResult result = room.PlaceStone(uid, userPlaceStoneDTO.Row, userPlaceStoneDTO.Col);
			return Ok(result);
		}
	}
}