using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol;
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
		public IActionResult GameStart([FromBody] string idToken, CancellationToken ct)
		{
            // 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession player = _serverManager.GetPlayerSession(idToken);

            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)           
                return Unauthorized($"[{idToken}] Player Session Not Found. Please Re Login.");

            player.LastRequestTime = DateTime.UtcNow;

			// 네 이놈 게임 룸에 있지도 않은 주제에 게임 시작이라고 뻥카를 쳐?
			if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom room) == false)
				return BadRequest("Not in game");

			// 아직 이 방 게임 대기 중이 아닌데??? 미쳐버린거냐
            // 흑돌 착수 후 방의 상태가 플레잉으로 바뀐 다음 백돌의 시작 요청이 올 수 있다...
			// if (room.State != GameRoomState.Waiting)return BadRequest("Not Game Wait");

            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Gomoku Controller] Game Start Request - ID Token {idToken}");

            // 상대방도 게임시작 요청을 해서 둘 다 게임 시작하면 돌아옴
            // SC_OpponentPlaceStoneDTO response = await room.WaitNextPlaceStoneAsync(player.Account.UID, ct);
            SC_ResponseStringDTO response = new SC_ResponseStringDTO("Game Start Process", room.TryGameStart());
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
            
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession player = _serverManager.GetPlayerSession(userPlaceStoneDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{userPlaceStoneDTO.IDToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

            // 네 이놈 게임 룸에 소속해 있지도 않은 주제에 착수 요청을 해?
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom room) == false)
				return BadRequest("Not in game");

            // 내 돌은 두었고, 그 결과가 return됨
            PlaceStoneResultType result = room.PlaceStone(player.Account.UID, userPlaceStoneDTO.Row, userPlaceStoneDTO.Col);

            // 이번 착수로 내가 승리했기 때문에 상대방 착수를 대기할 필요가 없으니 즉시 return
			if (result == PlaceStoneResultType.NowWin)
				return Ok(new SC_OpponentPlaceStoneDTO(new TimerSyncData(0f, 0), userPlaceStoneDTO.Row, userPlaceStoneDTO.Col, room.GetEndCode(player.Account.UID)));
			
			// 내가 승리한 상황이 아닌데 내 행동 결과가 즉시 끝나는 경우 (클라 변조임)
			if (result != PlaceStoneResultType.Success)
                return Unauthorized($"Place Stone Result : {result}");

            // 내 돌 착수에 성공했으면 다음 이벤트(상대방 착수)까지 대기 후 상대방이 착수하면 await해서 정보를 받아옴
            return Ok(await room.WaitNextPlaceStoneAsync(player.Account.UID, ct));
		}

        [HttpPost("TimerSynchronize")]
        public IActionResult ClientTimerSynchronize([FromBody] CS_RequestTimerSynchroDTO reqTimerSyncDTO)
        {
            // 클라가 데이터를 JOAT같이 줬어요
            if (reqTimerSyncDTO == null)
                return BadRequest("뭘 두겠다는 건데???");

            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession player = _serverManager.GetPlayerSession(reqTimerSyncDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{reqTimerSyncDTO.IDToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Gomoku Controller] Req Timer Sync By : {reqTimerSyncDTO.IDToken}");

			// 네 이놈 게임 룸에 소속해 있지도 않은 주제에 이하생략
			if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom room) == false)
                return BadRequest("Not in game");            

            return Ok(room.SynchronizeTimerAsync(player.Account.UID, reqTimerSyncDTO.NowTurn, reqTimerSyncDTO.MyTimer));
        }


        /*
         * TODO
            (내가 상대 착수 정보를 받음)(내 턴 시작)

            RequestMyTurn (내 턴이 진행하는 동안 응답 대기용 요청) : 롱 폴링
            이건 언제 응답해야 하는가?
            1. 중간에 게임 끝났을 때 (시간승, 시간패, 기권승, 기권패)
            2. 상대방 착수 정보 보내줄 때 이거 응답도 그냥 None으로 같이 보내줘야 함. Opponent DTO 조립할 때 None으로 이벤트 던져야 함
         */
        [HttpPost("ResponseGameEnd")]
        public async Task<IActionResult> ResponseGameEnd([FromBody] string idToken, CancellationToken ct)
        {
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession player = _serverManager.GetPlayerSession(idToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{idToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}][Gomoku Controller] Response Game End By : {idToken}");

			// 게임 룸에 있지도 않으면서 무슨 게임 종료 결과를 달라는거야
			if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom room) == false)
                return BadRequest("Not in game");


            // await
            return Ok(await room.WaitGameEndAsync(player.Account.UID, ct));
        }

        // Long Polling
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

            SC_OpponentPlaceStoneDTO response = await room.WaitNextPlaceStoneAsync(uid, ct);
            return Ok(response);
        }
    }
}