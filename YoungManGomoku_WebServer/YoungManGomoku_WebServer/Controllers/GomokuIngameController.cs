using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;
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
            PlayerSession? player = _serverManager.GetPlayerSession(idToken);

            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 게임 시작 실패. 플레이어 세션 탐색 실패 {idToken}");
                return Unauthorized($"[{idToken}] Player Session Not Found. Please Re Login.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            // 네 이놈 게임 룸에 있지도 않은 주제에 게임 시작이라고 뻥카를 쳐?
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 게임 시작 실패. Not In Game : {player.Account.UID}");
                return BadRequest("Not in game");
            }

            // 아직 이 방 게임 대기 중이 아닌데??? 미쳐버린거냐
            // 흑돌 착수 후 방의 상태가 플레잉으로 바뀐 다음 백돌의 시작 요청이 올 수 있다...
            // if (room.State != GameRoomState.Waiting)return BadRequest("Not Game Wait");

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
            PlayerSession? player = _serverManager.GetPlayerSession(userPlaceStoneDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 착수 요청 실패. 플레이어 세션 탐색 실패 : {player.Account.UID}");
                return Unauthorized($"[{userPlaceStoneDTO.IDToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            // 네 이놈 게임 룸에 소속해 있지도 않은 주제에 착수 요청을 해?
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 착수 요청 실패. Not In Game : {player.Account.UID}");
                return BadRequest("Not in game");
            }

            // 내 돌은 두었고, 그 결과가 return됨
            PlaceStoneResultType result = room.PlaceStone(player.Account.UID, userPlaceStoneDTO.Row, userPlaceStoneDTO.Col);

            // 이번 착수로 내가 승리했기 때문에 상대방 착수를 대기할 필요가 없으니 즉시 return
            if (result == PlaceStoneResultType.NowWin)
                return Ok(new SC_OpponentPlaceStoneDTO(new TimerSyncData(0f, 0), userPlaceStoneDTO.Row, userPlaceStoneDTO.Col));

            // 내가 승리한 상황이 아닌데 내 행동 결과가 즉시 끝나는 경우 (클라 변조임)
            if (result != PlaceStoneResultType.Success)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 착수 요청 실패. 내 승리가 아닌데 착수가 즉시 끝날 리 없음 : {player.Account.UID}");
                return Unauthorized($"Place Stone Result : {result}");
            }

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
            PlayerSession? player = _serverManager.GetPlayerSession(reqTimerSyncDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 타이머 동기화 실패 : 플레이어 세션 검색에 실패 {player.Account.UID}");
                return Unauthorized($"[{reqTimerSyncDTO.IDToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            // 네 이놈 게임 룸에 소속해 있지도 않은 주제에 이하생략
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 타이머 동기화 실패 : Not In Game {player.Account.UID}");
                return BadRequest("Not in game");
            }

            return Ok(room.SynchronizeTimerAsync(player.Account.UID, reqTimerSyncDTO.NowTurn, reqTimerSyncDTO.MyTimer));
        }


        [HttpPost("Request")]
        public IActionResult InGameRequest([FromBody] CS_InGameRequestDTO userInGameRequestDTO, CancellationToken ct)
        {
            // 클라가 데이터를 JOAT같이 줬어요
            if (userInGameRequestDTO == null)
                return BadRequest("뭘 두겠다는 건데???");

            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            ulong uid = _serverManager.GetPlayerUID(userInGameRequestDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            if (uid == 0)
                return BadRequest("미접속 유저");

            PlayerSession? player = _serverManager.GetPlayerSession(uid);

            if (player == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 타이머 동기화 실패 : 플레이어 세션 탐색 실패 {player.Account.UID}");
                // 인증 정보는 왔지만 유효한 세션이 아니다
                return Unauthorized($"Player Session Not Found. Please Re-Login.");
            }

            player.LastRequestTime = DateTime.UtcNow;


            // 네 이놈 게임 룸에 소속해 있지도 않은 주제에 인게임 요청을 했다고?
            if (_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 인게임 리퀘스트 실패 : Not In Game {player.Account.UID}");
                return BadRequest("IngameRequest Failed : Not in game");
            }

            // 여기서 인게임 리퀘스트 처리 
            // 무르기, 항복 등등
            switch (userInGameRequestDTO.IngameRequest)
            {
                case IngameRequestType.Surrender:
                    room.Surrender(uid);
                    break;
                case IngameRequestType.PurchaseByoyomi:
                    room.PurchaseCountdownLife(uid);
                    break;
                case IngameRequestType.TakeBack:
                    room.RequestTakeBack(uid);
                    break;
            }

            return Ok(new SC_ResponseStringDTO("InGame Request Success", true));
        }


        /*
        * TODO
           WaitForEvent : 롱 폴링
           항상 요청을 걸어놓고 대기하고 있음. 이건 언제 응답해야 하는가?
           1. 중간에 게임 끝났을 때 (시간승, 시간패, 기권승, 기권패, 접속끊김)
           2. 상대방이 항복, 무르기 요청, 초읽기 구매 등을 했을 때
        */
        [HttpPost("WaitForEvent")]
        public async Task<IActionResult> WaitForEvent([FromBody] string idToken, CancellationToken ct)
        {
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession? player = _serverManager.GetPlayerSession(idToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
			{
				_logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 플레이어 세션 탐색에 실패 :  {idToken}");
				return Unauthorized($"[{idToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            // 게임 룸에 있지도 않으면서 무슨 게임 종료 결과를 달라는거야
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 이벤트 대기 요청 실패 : Not In Game {player.Account.UID}");
                return BadRequest("WaitEvent Failed : Not in game");
            }

            // await
            return Ok(await room.WaitGameEventAsync(player.Account.UID, ct));
        }

        // 상대방의 무르기 요청에 대한 승인/거부 결과를 서버로 전송
        [HttpPost("TakeBackPermit")]
        public IActionResult TakeBackPermit([FromBody] CS_PermitDTO takebackPermitDTO, CancellationToken ct)
        {
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession? player = _serverManager.GetPlayerSession(takebackPermitDTO.IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{takebackPermitDTO.IDToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

            // 게임 룸에 있지도 않으면서 무슨 무르기를 허용하네 마네..
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}] [Gomoku Controller] 무르기 허가 여부 요청 실패 : Not In Game {player.Account.UID}");
                return BadRequest("Takeback Permit Failed : Not in game");
            }

            room.TakeBackResult(player.Account.UID, takebackPermitDTO.IsPermit);

            return Ok(new SC_ResponseStringDTO("Take Back Permit Response Success", true));
        }

        [HttpPost("GameResult")]
        public async Task<IActionResult> GameResult([FromBody] string IDToken, CancellationToken ct)
        {
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession? player = _serverManager.GetPlayerSession(IDToken);
          
            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{IDToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

            // 게임 룸에 있지도 않으면서 무슨 게임 종료 결과를 달라는거야
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                _logger.LogWarning($"[{DateTime.Now}][Gomoku Controller] [Game Result] {player.Account.UID} 유저가 게임 방 탐색에 실패했습니다...");
                return BadRequest("Game Result Request Failed : Not in game");
            }

            GameRecord gameResult = await room.WaitGameResultAsync(player.Account.UID, ct);

            _gameRoomManager.ServerContext.TryDBUpdateGameResult(player.Account.UID, _context);

            return Ok(gameResult);
        }

        [HttpPost("RematchRequest")]
        public IActionResult RematchRequest([FromBody] CS_PermitDTO requestRematchDTO, CancellationToken ct)
        {
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession? player = _serverManager.GetPlayerSession(requestRematchDTO.IDToken);
            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{requestRematchDTO.IDToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

            // 게임 룸에 있지도 않으면서 무슨 게임 종료 결과를 달라는거야
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                if (room != null)
                    _logger.LogDebug($"[{DateTime.Now}] [Gomoku Controller] 리매치 요청 실패 : 이 UID가 속한 방을 찾을 수 없습니다. {player.Account.UID}");
                else
                {
                    _logger.LogTrace($"[{DateTime.Now}] [Gomoku Controller] 리매치 요청 : 상대가 이미 방을 닫아서 방이 없습니다. {player.Account.UID}");
                    return Ok(new SC_ResponseStringDTO("Rematch Request Response - game room is null", false));
                }
                return BadRequest("Rematch Request Failed : Not in game");
            }

            return Ok(new SC_ResponseStringDTO("Rematch Request Response", room.RequestRematch(player.Account.UID, requestRematchDTO.IsPermit)));
        }

        [HttpPost("RematchResult")]
        public async Task<IActionResult> RematchResult([FromBody] string IDToken, CancellationToken ct)
        {
            // 일단 보내져온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
            PlayerSession? player = _serverManager.GetPlayerSession(IDToken);

            // 클라를 못 찾았음. 비인가 클라이언트거나 게임 도중 서버가 뒤졌다가 살아남
            // 인증 정보는 왔지만 유효한 세션이 아니다
            if (player == null)
                return Unauthorized($"[{IDToken}] Player Session Not Found.");

            player.LastRequestTime = DateTime.UtcNow;

            

            // 게임 룸에 있지도 않으면서 무슨 게임 종료 결과를 달라는거야
            if (_gameRoomManager.TryGetRoomByPlayer(player.Account.UID, out GameRoom? room) == false || room == null)
            {
                if (room != null)
                    _logger.LogDebug($"[{DateTime.Now}] [Gomoku Controller] 리매치 결과 요청 실패 : 이 UID가 속한 방을 찾을 수 없습니다. {player.Account.UID}");
                else
                    _logger.LogDebug($"[{DateTime.Now}] [Gomoku Controller] 리매치 결과 요청 실패 : 방이 없습니다. {player.Account.UID}");
                return BadRequest("Rematch Result Request Failed : Not in game");
            }

            return Ok(await room.WaitRematchAsync(player.Account.UID, ct));
        }
    }
}