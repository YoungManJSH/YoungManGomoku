using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YoungManGomoku_WebServer.SingletoneManager
{
    // 매칭 큐용 DTO
    public class WaitingPlayer
    {
        public string PlayerIdToken { get; set; }
        public TaskCompletionSource<MatchResult> TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class MatchResult
    {
        public bool Success { get; set; }
        public string OpponentIDToken { get; set; }
        public ulong GameRoomID { get; set; }
        public string Message { get; set; }
    }

    // 싱글톤 또는 서비스 레벨에서 관리되는 매칭 매니저
    public class MatchingManager
    {
        private const int MAX_WAITING = 100;

        private readonly object _lock;

        private readonly Queue<WaitingPlayer> _matchingQueue;
        private readonly Dictionary<string, WaitingPlayer> _waitingMap;

        private readonly IUIDProvider _uidGenerator;

        public MatchingManager(IUIDProvider uidGenerator)
        {
            _lock = new object();

            _matchingQueue = new Queue<WaitingPlayer>();
            _waitingMap = new Dictionary<string, WaitingPlayer>();
            _uidGenerator = uidGenerator;
        }

        // 매칭 큐에 Player ID 등록
        public Task<MatchResult> EnqueueAsync(string playerIDToken, CancellationToken ct)
        {
            lock (_lock)
            {
                if (_waitingMap.Count >= MAX_WAITING)
                {
                    return Task.FromResult(new MatchResult
                    {
                        Success = false,
                        Message = "Server busy"
                    });
                }

                if (_waitingMap.ContainsKey(playerIDToken))
                    throw new InvalidOperationException("Already matching");

                TaskCompletionSource<MatchResult> tcs 
                    = new TaskCompletionSource<MatchResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                WaitingPlayer wp = new WaitingPlayer
                {
                    PlayerIdToken = playerIDToken,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => Cancel(playerIDToken))
                };

                _matchingQueue.Enqueue(wp);
                _waitingMap[playerIDToken] = wp;

                TryMatch();

                return tcs.Task;
            }
        }

        // 매칭 큐에 등록된 Player ID를 매칭 큐에서 제거
        public void Cancel(string playerIDToken)
        {
            lock (_lock)
            {
                if (_waitingMap.TryGetValue(playerIDToken, out WaitingPlayer wp) == false)
                    return;

                wp.TaskCompSrc.TrySetResult(new MatchResult
                {
                    Success = false,
                    Message = "Cancelled"
                });

                _waitingMap.Remove(playerIDToken);
                // Queue에서 제거는 lazy (매칭 시 스킵)
            }
        }

        // 항상 lock 안에서 실행되어야 하는 함수
        private void TryMatch()
        {
            // 아직 레이팅이고 뭐고 신경쓰기 싫다는 코드
            // 동시다발적 접속이 있으면 3 이상일 수 있다
            while (_matchingQueue.Count >= 2)
            {
                WaitingPlayer p1 = _matchingQueue.Dequeue();
                WaitingPlayer p2 = _matchingQueue.Dequeue();

                if (!_waitingMap.ContainsKey(p1.PlayerIdToken) ||
                    !_waitingMap.ContainsKey(p2.PlayerIdToken))
                    continue;

                ulong roomID = _uidGenerator.GenerateUID64();

                p1.TaskCompSrc.TrySetResult(new MatchResult
                {
                    Success = true,
                    OpponentIDToken = p2.PlayerIdToken,
                    GameRoomID = roomID
                });

                p2.TaskCompSrc.TrySetResult(new MatchResult
                {
                    Success = true,
                    OpponentIDToken = p1.PlayerIdToken,
                    GameRoomID = roomID
                });

                _waitingMap.Remove(p1.PlayerIdToken);
                _waitingMap.Remove(p2.PlayerIdToken);
            }
        }
    }
}