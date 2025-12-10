using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using YoungManGomoku_Protocol;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;

// 우선 더미 코드, 살리거나 죽일 수 있음
/*
// 매칭 큐용 DTO
public class MatchRequest
{
    public string PlayerId { get; set; }       // 예: UID
    public int Rating { get; set; }
    public DateTime RequestTime { get; set; }  // 요청 받은 시간
}

// 싱글톤 또는 서비스 레벨에서 관리되는 매칭 매니저
public class MatchManager
{
    private readonly object _lock = new object();
    private readonly List<MatchRequest> _waiting = new List<MatchRequest>();

    // 매칭 신청 등록
    public void Enqueue(MatchRequest req)
    {
        lock (_lock)
        {
            _waiting.Add(req);
        }
    }

    // 주기적으로(혹은 요청 시) 매칭 시도
    public (MatchRequest? a, MatchRequest? b)? TryMatch()
    {
        lock (_lock)
        {
            if (_waiting.Count < 2) return null;

            // 현재 시간
            DateTime now = DateTime.UtcNow;

            // 가능한 모든 쌍을 돌면서 최소 rating 차이를 찾되,
            // 만약 차이가 30 이상이면 1분 이상 기다린 요청이어야 함
            MatchRequest? bestA = null;
            MatchRequest? bestB = null;
            int bestDiff = int.MaxValue;

            for (int i = 0; i < _waiting.Count; i++)
            {
                for (int j = i + 1; j < _waiting.Count; j++)
                {
                    var a = _waiting[i];
                    var b = _waiting[j];
                    int diff = Math.Abs(a.Rating - b.Rating);

                    bool canMatchNow = diff < 30 
                        || (now - a.RequestTime).TotalSeconds >= 60 
                        || (now - b.RequestTime).TotalSeconds >= 60;

                    if (!canMatchNow) continue;

                    // 우선순위 1: diff 작을수록 좋음
                    // 우선순위 2: 요청 시간 (먼저 요청한 순서)
                    if (diff < bestDiff ||
                        (diff == bestDiff && a.RequestTime < bestA?.RequestTime))
                    {
                        bestDiff = diff;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            if (bestA != null && bestB != null)
            {
                // 큐에서 제거
                _waiting.Remove(bestA);
                _waiting.Remove(bestB);
                return (bestA, bestB);
            }

            return null;
        }
    }
}
*/
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

        ConcurrentQueue<PlayerProfile> pp;

        public MatchingController(ILogger<MatchingController> logger, ApplicationDBContext context)
        {
            _logger = logger;
            _context = context;
        }


        [HttpPost("Register")]
        public async void RegisterMatchingQueue([FromBody] string IdToken)
        {
            
        }
    }
}
