using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;


namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MatchingController : ControllerBase
    {
        ApplicationDBContext _context;

        private readonly ILogger<MatchingController> _logger;
        private readonly ServerManager _serverManager;
        private readonly MatchingManager _matchingManager;

        // ConcurrentQueue<PlayerSession> _MatchingQueue;
        public MatchingController(ILogger<MatchingController> logger, ApplicationDBContext context, ServerManager serverManager, MatchingManager matchingManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
            _matchingManager = matchingManager;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterMatching([FromBody] string idToken, CancellationToken ct)
        {
            _logger.LogTrace($"[{DateTime.UtcNow}] [Matching Controller]Match Register By [{_serverManager.GetPlayerUID(idToken)}]{_serverManager.GetPlayerSession(_serverManager.GetPlayerUID(idToken)).Account.Nickname}");

            MatchResult mr = await _matchingManager.EnqueueAsync(idToken, ct);
            SC_MatchResultDTO scDTO = new SC_MatchResultDTO(
                _serverManager.ComposeOpponentPlayerData(mr.OpponentID),
                mr.Message,
                mr.Success,
                new SC_TimerSettingDTO(mainTime: 180f, byoyomiCount:3, byoyomiSeconds:30f, byoyomiPurchaseAmount: 2)
                );
            _logger.LogTrace($"[{DateTime.UtcNow}] [Matching Controller]Match Register Response : [{_serverManager.GetPlayerUID(idToken)}]{_serverManager.GetPlayerSession(_serverManager.GetPlayerUID(idToken)).Account.Nickname}\nVerSus\n[{mr.OpponentID}]{_serverManager.GetPlayerSession(mr.OpponentID).Account.Nickname}\n{mr.Message},{mr.Success}");

            return Ok(scDTO);
        }

        [HttpPost("Cancel")]
        public IActionResult CancelMatching([FromBody] string idToken)
        {
            _logger.LogTrace($"[{DateTime.UtcNow}] [Matching Controller]Match Cancel Request By {_serverManager.GetPlayerUID(idToken)}");
            _matchingManager.Cancel(idToken);
            return Ok(new SC_ResponseStringDTO("Match Register Cancel", true));
        }
    }
}