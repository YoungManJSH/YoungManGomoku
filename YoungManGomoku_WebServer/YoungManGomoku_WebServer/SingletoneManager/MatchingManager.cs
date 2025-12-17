using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YoungManGomoku_WebServer.SingletoneManager
{
    public class MatchResult
    {     
        public ulong OpponentID { get; set; }
        public ulong GameRoomID { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    // 매칭 큐용 DTO
    public class WaitingPlayer
    {
        public string PlayerIdToken { get; set; }

        // 언제 끝났다고 할 지를 내가 결정하기 위해 사용
        public TaskCompletionSource<MatchResult> TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    // 싱글톤 또는 서비스 레벨에서 관리되는 매칭 매니저
    public class MatchingManager
    {
        private const int MAX_WAITING = 100;

        private readonly object _lock;

        // 매칭 큐와 대기자 맵은 한 쌍(커플링)이라 Concurrent Collection으로 대체해선 안 된다
        private readonly Queue<WaitingPlayer> _matchingQueue;
        private readonly Dictionary<string, WaitingPlayer> _waitingMap;

        private readonly IServerContext _serverManagerContext;
		private readonly GameRoomManager _gameRoomManager;

		public MatchingManager(IServerContext serverManagerContext, GameRoomManager gameRoomManager)
        {
            _lock = new object();

            _matchingQueue = new Queue<WaitingPlayer>();
            _waitingMap = new Dictionary<string, WaitingPlayer>();
            _serverManagerContext = serverManagerContext;
            _gameRoomManager = gameRoomManager;
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
                {
                    //throw new InvalidOperationException("Already matching");
                    return Task.FromResult(new MatchResult
                    {
                        Success = false,
                        Message = "Already matching"
                    });
                }

                // RunContinuationsAsynchronously 옵션
                // await SetResult 이후 코드를 호출 스레드에서 바로 실행하지 않고 스레드 풀로 넘긴다
                // 데드락 방지 및 성능 향상
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

                if (!_waitingMap.ContainsKey(p1.PlayerIdToken))
                    continue; // p1 취소 → 버림

                WaitingPlayer p2 = null;
                while (_matchingQueue.Count > 0)
                {
                    var candidate = _matchingQueue.Dequeue();
                    if (_waitingMap.ContainsKey(candidate.PlayerIdToken))
                    {
                        p2 = candidate;
                        break;
                    }
                }

                if (p2 == null)
                {
                    _matchingQueue.Enqueue(p1);
                    break; // 매칭 가능한 상대 없음
                }


                // 매칭 성공, 방 배정
                ulong roomID = _serverManagerContext.GenerateUID64();

                p1.TaskCompSrc.TrySetResult(new MatchResult
                {
                    Success = true,
                    OpponentID = _serverManagerContext.GetPlayerUID(p2.PlayerIdToken),
                    GameRoomID = roomID
                });

                p2.TaskCompSrc.TrySetResult(new MatchResult
                {
                    Success = true,
                    OpponentID = _serverManagerContext.GetPlayerUID(p1.PlayerIdToken),
                    GameRoomID = roomID
                });

                _waitingMap.Remove(p1.PlayerIdToken);
                _waitingMap.Remove(p2.PlayerIdToken);


				_gameRoomManager.CreateRoom(
					_serverManagerContext.GetPlayerUID(p1.PlayerIdToken),
					_serverManagerContext.GetPlayerUID(p2.PlayerIdToken)
				);
			}
        }
    }
}