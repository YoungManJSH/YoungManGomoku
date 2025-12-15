using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_WebServer.SingletoneManager;


/*
[ApiController]
[Route("api/[controller]")]
public class MatchingController : ControllerBase
{
    private static readonly MatchManager _mgr = new MatchManager();

    [HttpPost("Join")]
    public IActionResult Join([FromBody] JoinDto dto)
    {
        var req = new MatchRequest
        {
            PlayerId = dto.PlayerId,
            Rating = dto.Rating,
            RequestTime = DateTime.UtcNow
        };
        _mgr.Enqueue(req);

        // 즉시 매칭 시도
        var match = _mgr.TryMatch();
        if (match != null)
        {
            var (a, b) = match.Value;
            return Ok(new { matched = true, playerA = a.PlayerId, playerB = b.PlayerId });
        }
        else
        {
            return Ok(new { matched = false });
        }
    }
}
*/

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
            return Ok(await _matchingManager.EnqueueAsync(idToken, ct));
        }

        [HttpPost("Cancel")]
        public IActionResult CancelMatching([FromBody] string idToken)
        {
            _matchingManager.Cancel(idToken);
            return Ok();
        }
    }
}
