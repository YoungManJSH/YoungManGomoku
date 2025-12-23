using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol.ServerToClient;
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
            _logger.LogTrace($"[{DateTime.Now}] [Matching Controller]Match Register By [{_serverManager.GetPlayerUID(idToken)}]{_serverManager.GetPlayerSession(idToken).Account.Nickname}");

            // Long Polling
            MatchResult mr = await _matchingManager.EnqueueAsync(idToken, ct);
            SC_MatchResultDTO scDTO = new SC_MatchResultDTO(
                _serverManager.ComposeOpponentPlayerData(mr.OpponentUID),
                mr.Message,
                mr.Success,
                mr.StoneColor,
                // 초기값에 대한 정의에 대한 기획이 따로 없으므로 우선 magic number로 처리.
                new SC_TimerSettingDTO(mainTime: 180f, byoyomiCount: 3, byoyomiSeconds: 30f, byoyomiPurchaseAmount: 2)
                );

            PlayerSession player = _serverManager.GetPlayerSession(idToken);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                return Unauthorized($"[{idToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            _logger.LogTrace($"[{DateTime.Now}] [Matching Controller]Match Register Response : [{_serverManager.GetPlayerUID(idToken)}]{_serverManager.GetPlayerSession(idToken).Account.Nickname}\nVerSus\n[{mr.OpponentUID}]{_serverManager.GetPlayerSession(mr.OpponentUID).Account.Nickname}\n{mr.Message},{mr.Success}");

            return Ok(scDTO);
        }

        [HttpPost("Cancel")]
        public IActionResult CancelMatching([FromBody] string idToken)
        {
            _logger.LogTrace($"[{DateTime.Now}] [Matching Controller]Match Cancel Request By {_serverManager.UserInfo(idToken)}");

            PlayerSession player = _serverManager.GetPlayerSession(idToken);

            if (player == null)
            {
                // 인증 정보는 왔지만 유효한 세션이 아니다
                return Unauthorized($"[{idToken}] Player Session Not Found.");
            }

            player.LastRequestTime = DateTime.UtcNow;

            _matchingManager.Cancel(idToken);

            return Ok(new SC_ResponseStringDTO("Match Register Cancel", true));
        }
    }
}