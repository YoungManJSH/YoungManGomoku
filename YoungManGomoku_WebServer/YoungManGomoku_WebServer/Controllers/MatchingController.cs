using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager;


namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MatchingController : ControllerBase
    {
        private readonly ILogger<MatchingController> _logger;
        private readonly ServerManager _serverManager;
        private readonly MatchingManager _matchingManager;

        public MatchingController(ILogger<MatchingController> logger, ServerManager serverManager, MatchingManager matchingManager)
        {
            _logger = logger;
            _serverManager = serverManager;
            _matchingManager = matchingManager;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterMatching([FromBody] string idToken, CancellationToken ct)
        {
            // _logger.LogTrace($"[{DateTime.Now}] [Matching Controller] {_serverManager.UserInfo(idToken)}Match Register");

            // Long Polling
            MatchResult mr = await _matchingManager.EnqueueAsync(idToken, ct);

            PlayerSession? player = _serverManager.GetPlayerSession(idToken);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                _logger.LogTrace($"Player Session Not Found. ID Token : [{idToken}]");

                return Unauthorized($"[{idToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;
                
            _logger.LogTrace($"[{DateTime.Now}] Match Result : {mr.OpponentUID} / {mr.Message} / {mr.Success} / {mr.StoneColor} /  {mr.GameRoomUID}");

            SC_MatchResultDTO scDTO = new SC_MatchResultDTO(
                   null,
                   mr.Message,
                   mr.Success,
                   mr.StoneColor,
                   // 게임 룸 UID 정보는 서버에서만 쓰고 클라로 넘기지 않는다
                   // 초기값에 대한 정의에 대한 기획이 따로 없으므로 우선 magic number로 처리.
                   _serverManager.DefaultTimerSetting
                   );

            if (mr.OpponentUID == 0 || mr.GameRoomUID == 0 || mr.StoneColor == StoneColorType.Empty || mr.Success == false)
                _logger.LogDebug($"[{DateTime.Now}] [Matching Controller] Match Register Fail : {mr.Message} / {mr.Success}");
            else
            {
                // _logger.LogTrace($"[{DateTime.Now}] [Matching Controller] Match Register Response : {_serverManager.UserInfo(idToken)}\nVerSus\n{_serverManager.UserInfo(mr.OpponentUID)}\n{mr.Message} / {mr.Success}");

                scDTO.OpponentPlayer =_serverManager.ComposeOpponentPlayerData(mr.OpponentUID);                
            }
            return Ok(scDTO);
        }

        [HttpPost("Cancel")]
        public IActionResult CancelMatching([FromBody] string idToken)
        {
            PlayerSession? player = _serverManager.GetPlayerSession(idToken);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                _logger.LogDebug($"Player Session Not Found. ID Token : [{idToken}]");

                return Unauthorized($"[{idToken}] Player Session Not Found.");
            }

            //_logger.LogTrace($"[{DateTime.Now}] [Matching Controller] {_serverManager.UserInfo(idToken)}Match Cancel Request");

            player.LastRequestTime = DateTime.UtcNow;

            _matchingManager.Cancel(idToken);

            return Ok(new SC_ResponseStringDTO("Match Register Cancel", true));
        }
    }
}