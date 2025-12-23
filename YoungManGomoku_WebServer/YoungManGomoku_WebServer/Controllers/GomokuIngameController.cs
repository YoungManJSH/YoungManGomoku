using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager;

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
		public async Task<IActionResult> GameStart([FromBody] string idToken, CancellationToken ct)
		{
            // 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            ulong uid = _serverManager.GetPlayerUID(idToken);

			if (uid == 0)
				return BadRequest("Logout or UnConnected User");

            PlayerSession player = _serverManager.GetPlayerSession(idToken);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                return Unauthorized($"[{idToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

			// 네 이놈 게임 룸에 있지도 않은 주제에 게임 시작이라고 뻥카를 쳐?
			if (_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom room) == false)
				return BadRequest("Not in game");

			// 아직 이 방 게임 대기 중이 아닌데??? 미쳐버린거냐
			if (room.State != GameRoomState.Waiting)
			{
				return BadRequest("Not Game Wait");
			}

            // 상대방도 게임시작 요청을 해서 둘 다 게임 시작하면 돌아옴
            // 보낼 정보 : 상대방 착수했음 { 상대방의 착수 좌표, 결정된 상대방의 타이머, 게임 종료 여부 (DTO) } [착수 유저의 상대편]
            // 그냥 다음 이벤트 기다림
            SC_OpponentMoveDTO response = await room.WaitNextEventAsync(uid, ct);
            return Ok(response);
		}


        // Long Polling
        // 내 돌을 여기다 두겠다는 요청. 응답은 상대 돌이 두어졌을 때 해야 한다.
		[HttpPost("PlaceStone")]
        public async Task<IActionResult> PlaceStone([FromBody] CS_PlaceStoneDTO userPlaceStoneDTO, CancellationToken ct)
		{
            // 클라가 데이터를 JOAT같이 줬어요
            if (userPlaceStoneDTO == null)
				return BadRequest("뭘 두겠다는 건데???");

            // 클라가 데이터를 JOAT같이 줬어요 2
            if (userPlaceStoneDTO.MyTimer == null)
				return BadRequest("Timer is Null");            
			
			// 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
			ulong uid = _serverManager.GetPlayerUID(userPlaceStoneDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            if (0 == uid)
                return BadRequest("Logout or UnConnected User");

            PlayerSession player = _serverManager.GetPlayerSession(uid);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                return Unauthorized($"[{uid}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            // 네 이놈 게임 룸에 소속해 있지도 않은 주제에 착수 요청을 해?
            if (_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom room) == false)
				return BadRequest("Not in game");

            // 내 돌은 두었고, 그 결과가 return됨
            PlaceStoneResult result = room.PlaceStone(uid, userPlaceStoneDTO.Row, userPlaceStoneDTO.Col);

            // 내 행동 결과가 즉시 끝나는 경우 (클라 변조임)
            if (result.Result != PlaceStoneResultType.Success)
                return Unauthorized(result);

            // 성공했으면 다음 이벤트까지 대기
            SC_OpponentMoveDTO response = await room.WaitNextEventAsync(uid, ct);
            return Ok(response);
		}

        [HttpPost("Request")]
        public async Task<IActionResult> InGameRequest([FromBody] CS_InGameRequestDTO userInGameRequestDTO, CancellationToken ct)
        {
            // 클라가 데이터를 JOAT같이 줬어요
            if (userInGameRequestDTO == null)
                return BadRequest("뭘 두겠다는 건데???");

            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            ulong uid = _serverManager.GetPlayerUID(userInGameRequestDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            if (uid == 0)
                return BadRequest("미접속 유저");

            PlayerSession player = _serverManager.GetPlayerSession(uid);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                return Unauthorized($"[{uid}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;


            // 네 이놈 게임 룸에 소속해 있지도 않은 주제에 인게임 요청을 했다고?
            if (_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom room) == false)
                return BadRequest("Not in game");



            // 여기서 인게임 리퀘스트 처리 
            // 무르기, 항복 등등
            //room.ProcessRequest(uid, userInGameRequestDTO);

            SC_OpponentMoveDTO response = await room.WaitNextEventAsync(uid, ct);
            return Ok(response);
        }

    }
}