using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YoungManGomoku_Protocol.TypeEnum.InGame;
using YoungManGomoku_WebServer.SingletoneManager.Interface;

namespace YoungManGomoku_WebServer.SingletoneManager
{
    public class MatchResult
    {
        public ulong OpponentUID { get; private set; }
        public ulong GameRoomUID { get; private set; }
        public string Message { get; private set; }
        public StoneColorType StoneColor { get; private set; }
        public bool Success { get; private set; }

        // default parameter는 message string을 제외하면 매치 실패 기준
        public MatchResult(string message = "", bool success = false, ulong opponentUID = 0, ulong gameRoomUID = 0, StoneColorType stoneColor = StoneColorType.Empty)
        {
            if (opponentUID != 0 && gameRoomUID != 0)
            {
                OpponentUID = opponentUID;
                GameRoomUID = gameRoomUID;
            }

            Message = message;
            StoneColor = stoneColor;
            Success = success;
        }
    }

    // 매칭 큐용 DTO
    public class WaitingPlayer
    {
        public string PlayerIdToken { get; set; }

        // 언제 끝났다고 할 지를 내가 결정하기 위해 사용
        public TaskCompletionSource<MatchResult>? TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
        /* TODO: GPT가 Cancel이나 매칭 성공 때 이거 Dispose 해줘야 한다는데?
         * IDisposable이라 메모리 누수 이슈 있다는데 제가 막 해도 되는 건지
         * 잘 몰라서 일단 내버려 뒀습니다. 나중에 확인하시고 맞으면 ㄱㄱ */
        
        public WaitingPlayer(string playerIdToken, TaskCompletionSource<MatchResult>? taskCompSrc, CancellationTokenRegistration cancellationTokenRegist)
		{
			PlayerIdToken = playerIdToken;
			TaskCompSrc = taskCompSrc;
			CancellationTokenRegist = cancellationTokenRegist;
		}
	}

    // 싱글톤 또는 서비스 레벨에서 관리되는 매칭 매니저
    public class MatchingManager
    {
        private const int MAX_WAITING = 100;
        private static readonly Random Random = new Random();

        private readonly object _lock;

        // 매칭 큐와 대기자 맵은 한 쌍(커플링)이라 Concurrent Collection으로 대체해선 안 된다
        private readonly Queue<string> _matchingQueue;
        private readonly Dictionary<string, WaitingPlayer> _waitingMap;

        private readonly ILogger<ServerManager> _logger;
        private readonly IServerContext _serverManagerContext;
        private readonly GameRoomManager _gameRoomManager;

        public MatchingManager(ILogger<ServerManager> logger, IServerContext serverManagerContext, GameRoomManager gameRoomManager)
        {
            _logger = logger;
            _lock = new object();

            _matchingQueue = new Queue<string>();
            _waitingMap = new Dictionary<string, WaitingPlayer>();
            _serverManagerContext = serverManagerContext;
            _gameRoomManager = gameRoomManager;
        }

        // 매칭 큐에 Player ID 등록
        public Task<MatchResult> EnqueueAsync(string playerIDToken, CancellationToken ct)
        {
            lock (_lock)
            {
                //_logger.LogTrace($"[{DateTime.Now}] Thread {Thread.CurrentThread.ManagedThreadId} enqueue {playerIDToken}");

                if (_waitingMap.Count >= MAX_WAITING)
                {
                    _logger.LogDebug($"[{DateTime.Now}] Server Busy : {_waitingMap.Count} >= {MAX_WAITING}");
                    return Task.FromResult(
                        new MatchResult
                        (
                            message: "Server busy",
                            success: false
                        )
                    );
                }

                if (_waitingMap.ContainsKey(playerIDToken))
                {
                    _logger.LogDebug($"[{DateTime.Now}] Already in Matching Queue : {_waitingMap[playerIDToken]}");
                    //throw new InvalidOperationException("Already matching");
                    return Task.FromResult(
                        new MatchResult
                        (
                            message: "Already matching",
                            success: false    
                        )
                    );
                }

                // RunContinuationsAsynchronously 옵션
                // await SetResult 이후 코드를 호출 스레드에서 바로 실행하지 않고 스레드 풀로 넘긴다
                // 데드락 방지 및 성능 향상
                TaskCompletionSource<MatchResult> tcs
                    = new TaskCompletionSource<MatchResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                _waitingMap[playerIDToken] = new WaitingPlayer(playerIDToken, tcs,
                    cancellationTokenRegist: ct.Register(() => Cancel(playerIDToken)));
                
                _matchingQueue.Enqueue(playerIDToken);

                TryMatch();

                return tcs.Task;
            }
        }

        // 매칭 큐에 등록된 Player ID를 매칭 큐에서 제거
        public void Cancel(string playerIDToken)
        {
            lock (_lock)
            {
                _logger.LogTrace($"[{DateTime.Now}] [Matching Cancel] {_serverManagerContext.GetPlayerUID(playerIDToken)}");

                if (_waitingMap.TryGetValue(playerIDToken, out WaitingPlayer? wp) == false)
                {
                    _logger.LogDebug($"[{DateTime.Now}] 매칭 대기 맵에 유저 [ {_serverManagerContext.GetPlayerUID(playerIDToken)}]가 없습니다.");
                    return;
                }

                

                wp.TaskCompSrc?.TrySetResult(
                    new MatchResult
                    (
                        message: "Matching Register Cancelled",
                        success: false
                    )
                );

                // Queue에서 제거는 lazy (매칭 시 스킵)
                _waitingMap.Remove(playerIDToken);
            }
        }

        // 항상 lock 안에서 실행되어야 하는 함수
        private void TryMatch()
        {
            // 아직 레이팅이고 뭐고 신경쓰기 싫다는 코드
            // 동시다발적 매칭 신청이 있으면 3 이상일 수 있다
            while (_matchingQueue.Count >= 2)
            {
                //_logger.LogTrace($"[{DateTime.Now}] Matching Queue Count : {_matchingQueue.Count}");
                string p1Token = _matchingQueue.Dequeue();

                if (_waitingMap.TryGetValue(p1Token, out WaitingPlayer? p1) == false)
                {
                    _logger.LogWarning($"[{DateTime.Now}] Matching Queue에는 있는데 대기자 맵에 없습니다. [{_serverManagerContext.GetPlayerUID(p1Token)}]");
                    continue; // p1이 매칭을 취소해서 맵에 없으니 큐에서 버림
                }

                WaitingPlayer? p2 = null;
                while (_matchingQueue.Count > 0)
                {
                    string p2Token = _matchingQueue.Dequeue();
                    
                    if (p1Token == p2Token)
                    {
                        _logger.LogWarning($"[{DateTime.Now}] 중복 등록된 Matching queue 값이므로 스킵\n{_serverManagerContext.GetPlayerUID(p2Token)}");
                        continue;
                    }

					// Queue에 있어도 _waitingMap에 존재해야만 매칭
					if (_waitingMap.TryGetValue(p2Token, out p2))
                    {
                        _logger.LogTrace($"[{DateTime.Now}] Matching 상대 플레이어 발견\n{_serverManagerContext.GetPlayerUID(p2Token)}");
                        break;
                    }
                }

                if (p2 == null) // 큐를 끝까지 다 뽑았는데 p2를 찾지 못함
                {
                    _logger.LogDebug($"[{DateTime.Now}] Matching Fail - Matching Opponent is not Exist");
                    _matchingQueue.Enqueue(p1Token); // p1Token은 유효했던 Queue이므로 되돌려놓기
                    break; // 매칭 상대 찾기 종료
                }

                // 매칭 성공, 방 배정
                ulong roomID = _serverManagerContext.GenerateUID64();
                _logger.LogDebug($"[{DateTime.Now}] [Matching Success] Room ID {roomID}");

                
                int colorRandomValue = Random.Next(0, 2);

                // 매칭에 성공한 두 플레이어에게 각각의 게임을 위한 정보 (색, 방번호) 전달 및 클라이언트로 응답
                p1.TaskCompSrc?.TrySetResult(
                    new MatchResult
                    (
                        success: true,
                        opponentUID: _serverManagerContext.GetPlayerUID(p2.PlayerIdToken),
                        gameRoomUID: roomID,
                        stoneColor: StoneColorType.Black + colorRandomValue
                    )
                );

                p2.TaskCompSrc?.TrySetResult(
                    new MatchResult
                    (
                        success: true,
                        opponentUID: _serverManagerContext.GetPlayerUID(p1.PlayerIdToken),
                        gameRoomUID: roomID,
                        stoneColor: StoneColorType.White - colorRandomValue
                    )
                );

                _waitingMap.Remove(p1.PlayerIdToken);
                _waitingMap.Remove(p2.PlayerIdToken);

                if (colorRandomValue == 0)
                {
                    _gameRoomManager.CreateRoom(roomID,
                        _serverManagerContext.GetPlayerUID(p1.PlayerIdToken),
                        _serverManagerContext.GetPlayerUID(p2.PlayerIdToken));
                }
                else
                {
                    _gameRoomManager.CreateRoom(roomID,
                        _serverManagerContext.GetPlayerUID(p2.PlayerIdToken),
                        _serverManagerContext.GetPlayerUID(p1.PlayerIdToken));
                }
            }
        }
    }
}