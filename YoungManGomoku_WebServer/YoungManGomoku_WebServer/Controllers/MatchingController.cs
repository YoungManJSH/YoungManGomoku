using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            // _serverManager.PlayerDatas
            MatchResult mr = await _matchingManager.EnqueueAsync(idToken, ct);
            SC_MatchResultDTO scDTO = new SC_MatchResultDTO(
                _serverManager.ComposeOpponentPlayerData(mr.OpponentID),
                mr.Message,
                mr.Success
                );
            return Ok(scDTO);
        }

        [HttpPost("Cancel")]
        public IActionResult CancelMatching([FromBody] string idToken)
        {
            _matchingManager.Cancel(idToken);
            return Ok();
        }
    }
}
