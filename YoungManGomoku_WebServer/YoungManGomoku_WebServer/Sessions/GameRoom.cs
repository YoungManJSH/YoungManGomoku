using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Sessions
{
	// 서버 검증용, 정상적인 클라이언트라면 Success만이 돌아와야 한다
	public enum PlaceStoneResultType
	{
		Success,        // 착수 성공
		NotYourTurn,    // 니 턴 아님
		Occupied,       // 그 위치 이미 뒀어
		Invalid,        // ㅈ버그에요
        NowWin          // 지금 즉시 승리함
	}
	
	public enum GameRoomState
	{
		Waiting, // 대기 중, 아직 게임 시작 안 됐음
		Playing, // 게임 중
		Finished // 게임 끝났음, 리벤지 각?
	}

    public class GomokuIngamePlaceStoneWaitingPlayer
    {
        public ulong UID { get; set; }
        public TaskCompletionSource<SC_OpponentPlaceStoneDTO>? TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GomokuGameEventWaitingPlayer
    {
        public ulong UID { get; set; }
        public TaskCompletionSource<SC_WaitEventDTO>? TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    class GomokuGameResultWaitingPlayer
    {
        public ulong UID { get; set; }
        public TaskCompletionSource<GameRecord>? TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GomokuRematchWaitingPlayer
    {
        public ulong UID { get; set; }
        public TaskCompletionSource<SC_RematchResultDTO>? TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GameRoom
	{
        // Elo Rating 전용 가중치
        public const int K_Factor = 20;

        // 방 식별자, 게임룸매니저에서 게임룸 Dictionary를 관리할 때 사용
		public ulong RoomID { get; }

        // 재도전 시 색 변경이 일어날 수 있다면 private set, 없다면 setter 제거
		public ulong BlackPlayerUID { get; private set; }
		public ulong WhitePlayerUID { get; private set; }

		// 방 밖에서 방이 게임 중인지 아닌지를 결정하면 안 된다
		public GameRoomState State { get; private set; }

		// 날 건드릴 수 있는 놈이 플레이어 둘이잖냐, 락 걸어야지
        // 현재 게임 룸 착수 전체 락
        private readonly object _gameroomLock = new object();

        // 나 자신의 방을 닫을 때 필요함, 로거 꺼내올 때도 씀
		private readonly GameRoomManager _gameRoomManager;

		// 오목판
		private readonly Board _board;

        // Long Polling 착수 대기자 명단
        private readonly Dictionary<ulong, GomokuIngamePlaceStoneWaitingPlayer> _waitingPlaceStoneMap;
        private readonly Dictionary<ulong, GomokuGameEventWaitingPlayer> _waitingEventMap;
        private readonly Dictionary<int, TaskCompletionSource<bool>> _synchronizeTimerTurnWaiters;
        private readonly Dictionary<ulong, GomokuGameResultWaitingPlayer> _waitingResultMap;
        private readonly Dictionary<ulong, GomokuRematchWaitingPlayer> _waitingRematchMap;

        // 재대결 대기자 Queue
        private readonly Queue<ulong> _waitingRematchQueue;

        // 하트비트, n 초 이상 미 요청 시 접속 끊김으로 간주
        private readonly Dictionary<ulong, DateTime> _lastRequestTime;

		// 시간패 함수 콜백
		private readonly Dictionary<ulong, Timer> _timeOutTimer;

		// 유저별 타이머 정보
		private readonly Dictionary<ulong, UserTimer> _userTimers;

        // 유저별 게임 결과
        private readonly Dictionary<ulong, GameEndCode> _userEndCodes;

        

        // 서버에 캐싱된 마지막 타이머 갱신 시간
        private long _gameProgressMilliseconds;

		// 서버에 캐싱된 무르기 요청이 들어온 시간
		private long _lastTakebackRequestTime;

		// 현재 턴인 사람의 UID
		private ulong _currentTurnUID;

        // 승패 결정 시 승리자의 UID, 승자가 없거나 게임 도중이면 0
        private ulong _winnerUID;

        // _board의 NowTurn 값이 홀수면 백, 짝수면 흑 차례라는 뜻
        private bool IsNowTurnBlack => (_board.NowTurn & 1) == 0;

        public bool IsFinished => _userEndCodes[BlackPlayerUID] != GameEndCode.None && _userEndCodes[WhitePlayerUID] != GameEndCode.None;

		public GameRoom(ulong roomID, ulong blackPlayerUID, ulong whitePlayerUID, GameRoomManager roomManager)
		{
            RoomID = roomID;
            BlackPlayerUID = blackPlayerUID;
            WhitePlayerUID = whitePlayerUID;
            State = GameRoomState.Waiting;

            _gameRoomManager = roomManager;

            _board = new Board();
			// 흑돌 첫 수는 무조건 중앙 고정, 그런데 첫수는 착수 요청으로 들어올 것임
            //_board.TryMoveStone(Board.BoardSize / 2, Board.BoardSize / 2);

            _board.OnBlackGomoku += OnBlackWin;
            _board.OnWhiteGomoku += OnWhiteWin;
            _board.OverMaxTurn += OnDraw;
            _board.OnBlackUnmovable += OnBlackUnmovable;

			// 첫 수는 흑돌
			_currentTurnUID = BlackPlayerUID;

            // 상대방 착수 대기
			_waitingPlaceStoneMap = new Dictionary<ulong, GomokuIngamePlaceStoneWaitingPlayer>();

            // 특수 게임 종료 대기
            _waitingEventMap = new Dictionary<ulong, GomokuGameEventWaitingPlayer>();

            // 타이머 동기화 대기
            _synchronizeTimerTurnWaiters = new Dictionary<int, TaskCompletionSource<bool>>();

            // 게임 종료 대기
            _waitingResultMap = new Dictionary<ulong, GomokuGameResultWaitingPlayer>();

            // 재대결 대기
            _waitingRematchMap = new Dictionary<ulong, GomokuRematchWaitingPlayer>();
            _waitingRematchQueue = new Queue<ulong>();

            // 유저별 타이머 정보 등록
            _userTimers = new Dictionary<ulong, UserTimer>()
            {
                // default : 180f, 3, 30f
                [blackPlayerUID] = new UserTimer(initMainTime: roomManager.ServerContext.DefaultTimerSetting.MainTime, initByoyomiCount: roomManager.ServerContext.DefaultTimerSetting.ByoyomiCount, byoyomiSeconds: roomManager.ServerContext.DefaultTimerSetting.ByoyomiSeconds),
                [whitePlayerUID] = new UserTimer(initMainTime: roomManager.ServerContext.DefaultTimerSetting.MainTime, initByoyomiCount: roomManager.ServerContext.DefaultTimerSetting.ByoyomiCount, byoyomiSeconds: roomManager.ServerContext.DefaultTimerSetting.ByoyomiSeconds)
            };

            // 스레드 타이머 콜백 등록
            _timeOutTimer = new Dictionary<ulong, Timer>()
            {
                [blackPlayerUID] = new Timer(OnBlackTimeOut, null, Timeout.Infinite, Timeout.Infinite),
                [whitePlayerUID] = new Timer(OnWhiteTimeOut, null, Timeout.Infinite, Timeout.Infinite)
            };

            // _endReason = GameEndCode.None; // 이 방에서 게임이 끝난 이유. None은 지금 게임중이라는 뜻
            _userEndCodes = new Dictionary<ulong, GameEndCode>
            {
                [blackPlayerUID] = GameEndCode.None,
                [whitePlayerUID] = GameEndCode.None
            };

            // 방 유저별 마지막 리퀘스트 타임 등록
			_lastRequestTime = new Dictionary<ulong, DateTime>
			{
				[blackPlayerUID] = DateTime.UtcNow,
				[whitePlayerUID] = DateTime.UtcNow
			};
        }

        // UID 대기 취소 (착수)
        public void CancelWaitPlaceStone(ulong UID)
        {
            lock (_gameroomLock)
            {
				_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [PlaceStone Cancel] {RoomID} 방 : [{UID}] 착수 대기 취소\n");
				if (_waitingPlaceStoneMap.TryGetValue(UID, out GomokuIngamePlaceStoneWaitingPlayer? waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc?.TrySetCanceled();
                    _waitingPlaceStoneMap.Remove(UID);
                }
            }
        }

        // UID 대기 취소 (인게임 이벤트)
        public void CancelWaitEvent(ulong UID)
        {
            lock (_gameroomLock)
            {
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Event Cancel] {RoomID} 방 : [{UID}] 이벤트 대기 취소\n");
                if (_waitingEventMap.TryGetValue(UID, out GomokuGameEventWaitingPlayer? waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc?.TrySetCanceled();
                    _waitingEventMap.Remove(UID);
                }
            }
        }

        // UID 대기 취소 (게임 종료)
        public void CancelWaitGameResult(ulong UID)
        {
            lock (_gameroomLock)
            {
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Game Result Cancel] {RoomID} 방 : [{UID}] 게임 결과 대기 취소\n");
                if (_waitingResultMap.TryGetValue(UID, out GomokuGameResultWaitingPlayer? waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc?.TrySetCanceled();
                    _waitingEventMap.Remove(UID);
                }
            }
        }

        // UID 대기 취소 (재대결 대기 종료)
        public void CancelWaitRematchResult(ulong UID)
        {
            lock (_gameroomLock)
            {
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Rematch Cancel] {RoomID} 방 : [{UID}] 재대결 대기 취소\n");
                if (_waitingRematchMap.TryGetValue(UID, out GomokuRematchWaitingPlayer? waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc?.TrySetCanceled();
                    _waitingEventMap.Remove(UID);
                }
            }
        }

        // 상대방 착수 대기 이벤트 등록 (Long - Polling)
        public Task<SC_OpponentPlaceStoneDTO> WaitNextPlaceStoneAsync(ulong UID, CancellationToken ct)
        {
            lock (_gameroomLock)
            {
                if (State == GameRoomState.Finished)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitEventAsync] Room {RoomID} : 이미 종료된 게임인데 {UID}에게서 착수 요청이 날아옴\n");
                    return Task.FromResult(new SC_OpponentPlaceStoneDTO(new TimerSyncData(0f, 0), 255, 255));
                }

                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Wait PlaceStone Async] Room {RoomID} : {UID} 상대 착수 대기\n");

				// 대기자용 TCS 조립 (대기 결과 반환용)
				TaskCompletionSource<SC_OpponentPlaceStoneDTO> tcs
                    = new TaskCompletionSource<SC_OpponentPlaceStoneDTO>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                // 대기자 데이터 조립 및 대기 등록 (Long Poll)
                _waitingPlaceStoneMap[UID] = new GomokuIngamePlaceStoneWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWaitPlaceStone(UID))
                };

                return tcs.Task;
            }
        }

        // 특수 게임 이벤트 등록 (Long - Polling)
        public Task<SC_WaitEventDTO> WaitGameEventAsync(ulong UID, CancellationToken ct)
        {
            lock (_gameroomLock)
            {
                if (State == GameRoomState.Finished)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitEventAsync] Room {RoomID} : 이미 종료된 게임인데 {UID}에게서 이벤트 요청이 날아옴\n");
                    return Task.FromResult(new SC_WaitEventDTO(IngameRequestType.None));
                }
                
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitEventAsync] Room {RoomID} : {UID} 특수 게임 발생 이벤트 대기\n");

                

                // 대기자용 TCS 조립 (대기 결과 반환용)
                TaskCompletionSource<SC_WaitEventDTO> tcs
                    = new TaskCompletionSource<SC_WaitEventDTO>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                // 대기자 데이터 조립 및 대기 등록 (Long Poll)
                _waitingEventMap[UID] = new GomokuGameEventWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWaitEvent(UID))
                };

                return tcs.Task;
            }
        }

        private Task<bool> WaitForSynchronizeTimerTurnAsync(int turn)
        {
            lock (_gameroomLock)
            {
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitTimerAsync] Room {RoomID} : {turn} 턴 - 타이머 싱크 대기\n");
                
                if (!_synchronizeTimerTurnWaiters.TryGetValue(turn, out TaskCompletionSource<bool>? tcs))
                {
                    tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _synchronizeTimerTurnWaiters[turn] = tcs;
                }

                // 이미 턴이 같으면 바로 완료
                if (_board.NowTurn == turn)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitTimerAsync] Room {RoomID} : {turn}턴과 서버보드 턴 {_board.NowTurn} 타이머 싱크 일치\n");
                    tcs.TrySetResult(true);
                }

                return tcs.Task;
            }
        }

        // 재대결 이벤트 등록 (Long - Polling)
        public Task<GameRecord> WaitGameResultAsync(ulong UID, CancellationToken ct)
        {
            lock (_gameroomLock)
            {
                
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitGameResultAsync] Room {RoomID} : {UID} 게임 종료 이벤트 대기\n");

                // 대기자용 TCS 조립 (대기 결과 반환용)
                TaskCompletionSource<GameRecord> tcs = new TaskCompletionSource<GameRecord>(TaskCreationOptions.RunContinuationsAsynchronously);

                // 대기자 데이터 조립 및 대기 등록 (Long Poll)
                _waitingResultMap[UID] = new GomokuGameResultWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWaitGameResult(UID))
                };

                return tcs.Task;
            }
        }

        // 재대결 이벤트 등록 (Long - Polling)
        public Task<SC_RematchResultDTO> WaitRematchAsync(ulong UID, CancellationToken ct)
        {
            lock (_gameroomLock)
            {
                if (State != GameRoomState.Finished)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitEventAsync] Room {RoomID} : 게임이 진행중인데 {UID}에게서 재대결 요청이 날아옴\n");
                    return Task.FromResult(new SC_RematchResultDTO(false, GetColor(UID)));
                }

                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [WaitEventAsync] Room {RoomID} : {UID} 특수 게임 발생 이벤트 대기\n");

                // 대기자용 TCS 조립 (대기 결과 반환용)
                TaskCompletionSource<SC_RematchResultDTO> tcs = new TaskCompletionSource<SC_RematchResultDTO>(TaskCreationOptions.RunContinuationsAsynchronously);

                // 대기자 데이터 조립 및 대기 등록 (Long Poll)
                _waitingRematchMap[UID] = new GomokuRematchWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWaitEvent(UID))
                };

                return tcs.Task;
            }
        }

        // 게임 종료 작업, Dispose는 필수 (Unmanaged Heap)
        private void DispatchGameEndToAll()
        {
			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Game End] {RoomID} 방에서 게임 종료 작업 수행!\n");
			// 대기자 명단에서 대기 플레이어를 제거 및 종료 이벤트를 조립해서 던져줌
			foreach (GomokuIngamePlaceStoneWaitingPlayer waitingPlayer in _waitingPlaceStoneMap.Values)
            {
                if (_userTimers.TryGetValue(waitingPlayer.UID, out UserTimer? waitingUserTimer) == false)
                {
                    // 큰일나는 예외 상황, Assert 상황이지만 있을 수 있으니 방심할 수 없다.
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] 착수 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]의 타이머를 찾을 수 없었습니다!");
                    continue;
                }
                
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 착수 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]에게 게임 종료 착수 이벤트 응답");
                waitingPlayer.TaskCompSrc?.TrySetResult(new SC_OpponentPlaceStoneDTO(waitingUserTimer.SyncData, 255, 255));
                waitingPlayer.CancellationTokenRegist.Dispose();
            }

            _waitingPlaceStoneMap.Clear();

			foreach (GomokuGameEventWaitingPlayer waitingPlayer in _waitingEventMap.Values)
			{
				_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 이벤트 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]에게 게임 종료 시점 빈 이벤트 응답");
				waitingPlayer.TaskCompSrc?.TrySetResult(new SC_WaitEventDTO(IngameRequestType.None));
				waitingPlayer.CancellationTokenRegist.Dispose();
			}

            _waitingEventMap.Clear();




			// Unmanaged Heap
			foreach (Timer timeOutCallback in _timeOutTimer.Values)
            {
				timeOutCallback.Dispose();
			}

            _timeOutTimer.Clear();
		}

        public bool TryGameStart()
        {
            // 백돌 첫 턴 시작 시간 갱신용
            if (_gameProgressMilliseconds == 0) _gameProgressMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Game Start]] {RoomID} 방에서 게임 시작 작업 수행\n시작 ms : {_gameProgressMilliseconds}");
			return true;
			// return State == GameRoomState.Waiting; // 흑돌 착수 후 방의 상태가 플레잉으로 바뀐 다음 백돌의 시작 요청이 올 수 있다...
		}

        public PlaceStoneResultType PlaceStone(ulong UID, int x, int y)
        {
            lock (_gameroomLock)
            {
                _lastRequestTime[UID] = DateTime.UtcNow;
                // 클라 변조 유효성 체크, 클라에서 제대로 요청이 들어왔다면 여기 들어올 일이 없음
                {
                    // 게임 끝났어
                    if (IsFinished)
                        return PlaceStoneResultType.Invalid;

                    // 니 턴 아니야, 근데 첫턴은 네 턴 아닐 수도 있으니 제외
                    if (UID != _currentTurnUID && _board.NowTurn != 0)
                        return PlaceStoneResultType.NotYourTurn;
                }

                // 게임 시작, 첫 수 전
                if (_board.NowTurn == 0)
                {
                    // 첫 수인데 흑돌이 아니셔?
                    if (BlackPlayerUID != UID)
                    {
                        // 이건 그럴 수 있음. 백돌 착수 요청이 먼저 도착할 수 있지...
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Place Stone] 첫 수인데 흑돌이 아닌 {UID} 유저가 착수 요청을 했습니다!\n");
                        return PlaceStoneResultType.Invalid; // 다른걸 생각해봐야 할 듯
                    }

                    // 흑돌은 무조건 첫 수 정 중앙이기 때문에 이건 클라 뚜껑 딴게 맞음, 
                    // 룰 상 중앙은 무조건 7, API 담당자가 중앙 번호나 보드판의 최대 길이를 뱉어주는 프로퍼티를 만들지 않아 매직 넘버를 일단 박음
                    if (x != Board.BoardSize / 2 || y != Board.BoardSize / 2)
                    {
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Place Stone] 첫 수인데 {UID} 흑 유저가 (7,7) 위치에 두지 않았습니다...!\n");
                        return PlaceStoneResultType.Invalid;
                    }

                    // 게임 대기 상태가 아닌데 첫 수라고?
                    if (State != GameRoomState.Waiting)
                    {
                        // 리벤지 쪽 구현 이상하면 여기 들어올 수도 있음
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] [Place Stone] 첫 수인데 게임 룸의 상태가 대기중이 아닙니다!\n");
                        return PlaceStoneResultType.Invalid;
                    }

                    // 흑돌 첫 수 두는 순간 게임이 시작됨 (이 이후 백돌의 시작 요청이 올 수 있음)
                    State = GameRoomState.Playing;
                }


				if (_timeOutTimer.TryGetValue(UID, out Timer? myTimerCallback) == false)
                {
					_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] [{UID}] 타이머 콜백이 등록되어 있지 않아 시간승패 처리가 불가능합니다!!!");
				}
				myTimerCallback?.Change(Timeout.Infinite, Timeout.Infinite);



                // 타이머 진행
                // [흑].프로그레스(흑턴 시작 시간, 흑턴 착수 정보가 온 시간)
                if (_userTimers.TryGetValue(UID, out UserTimer? myTimer))
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] [{UID}] 타이머 진행 전 : 서버시간 {_gameProgressMilliseconds}ms - {myTimer.MainTime} / {myTimer.ByoyomiCount} / {myTimer.NowByoyomiSeconds}");
                    myTimer.ProgressExcludingTol(_gameProgressMilliseconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] [{UID}] 타이머 진행 후 : 서버시간 {_gameProgressMilliseconds}ms - {myTimer.MainTime} / {myTimer.ByoyomiCount} / {myTimer.NowByoyomiSeconds}");
                }

                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] {_board.NowTurn}턴 시작 : {(IsNowTurnBlack ? "Black" : "White")} {_gameRoomManager.ServerContext.UserInfo(_currentTurnUID)}유저의 차례");

                // TryMoveStone이 true일 시 착수 성공. 내부적으로 승패 처리까지 동작하며 _board에 등록한 event들이 실행됨
                if (_board.TryMoveStone(x, y) == false)
                {
                    // 빈 곳이 아닌데 두려고 시도했거나, 흑돌이 금수 위치에 두려고 시도함. 클라 변조 체크
                    return PlaceStoneResultType.Occupied;
                }
                // 착수 성공 및 게임 결과 처리
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] {_board.NowTurn}턴으로 진행");

                // 보드 턴이 업데이트 되었다면 타이머 싱크로나이즈로 전달
                if (_synchronizeTimerTurnWaiters.TryGetValue(_board.NowTurn, out TaskCompletionSource<bool>? tcs))
                {
                    while (tcs.TrySetResult(true) == false) ; // 대기 Task 완료
                    _synchronizeTimerTurnWaiters.Remove(_board.NowTurn);
                }

                ulong opponent = GetOpponent(UID);

                // 게임 룸 타이머 갱신
                _gameProgressMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (_userTimers.TryGetValue(opponent, out UserTimer? opponentTimer) == false)
                {
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Place Stone] 상대 타이머가 없습니다!!!");
                }
                else
                {
					// 데드라인값 : 착수 응답을 보내는 놈의 시간패 시각
					long opponentWaitingTime = opponentTimer.DeadLine(_gameProgressMilliseconds) - _gameProgressMilliseconds;

					// 상대방의 타임아웃 이벤트를 동작시킨다.
					if (_timeOutTimer.TryGetValue(opponent, out Timer? opponentTimerCallback) == false)
                        _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Place Stone] 상대 타이머 콜백이 등록되지 않았습니다!!!");
					
                    if (opponentWaitingTime < -1)
                    {
                        _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Place Stone] 데드라인 값 {opponentWaitingTime} < -1 입니다! => 데드라인 계산값 : {opponentTimer.DeadLine(_gameProgressMilliseconds)}, 게임 진행 시간 : {_gameProgressMilliseconds} ");
                    }

					opponentTimerCallback?.Change(opponentWaitingTime, -1L);
                }

                // 내 상대가 대기 중이면 내가 착수한 정보를 대기중인 상대 이벤트로 등록해서 응답시켜줌
                if (_waitingPlaceStoneMap.TryGetValue(opponent, out GomokuIngamePlaceStoneWaitingPlayer? waitingPlaceStonePlayer))
                {
                    waitingPlaceStonePlayer.TaskCompSrc?.TrySetResult(new SC_OpponentPlaceStoneDTO(myTimer!.SyncData, (byte)x, (byte)y));
                    // DTO 조립하고 결과를 넣어 줬으니 상대의 대기는 끝났고 응답을 보내줘야지
                    _waitingPlaceStoneMap.Remove(opponent);
                }


                // 이번 착수로 네가 지금 즉시 승리했음, 무승부를 제외한 모든 오목 승/패는 여기로 들어옴
                if (UID == _winnerUID)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] {_gameRoomManager.ServerContext.UserInfo(UID)}차례에서 승리");
                    FinishGame();
                    return PlaceStoneResultType.NowWin;
                }

                // 연결끊김 처리





                // 게임 안 끝났네, 상대 턴으로 넘김
                if (State != GameRoomState.Finished && _winnerUID == 0UL)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Place Stone] {_gameRoomManager.ServerContext.UserInfo(UID)}차례에서 {_gameRoomManager.ServerContext.UserInfo(GetOpponent(UID))}으로 턴 교체");
                    _currentTurnUID = opponent;
                }

                return PlaceStoneResultType.Success;
            }
        }

		public TimerSyncData SynchronizeTimerAsync(ulong UID, int turn, TimerSyncData clientTimerData)
		{
			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] Client Turn {turn} / Server Board Turn {_board.NowTurn}");

            // 개 등신 코드인데 일단은 이렇게라도 동작시켜
            /*
            int loopCount = 0;
            while (turn != _board.NowTurn) ++loopCount;       
            */

            // 이벤트 기반으로 안전하게 턴 대기, 기존 while busy waiting 으로 인한 무식한 CPU 점유 제거
            //await WaitForSynchronizeTimerTurnAsync(turn);

            const int MAX_TRYCOUNT = 1200;
            int tryCnt = 0;
            for (tryCnt = 0; tryCnt < MAX_TRYCOUNT; ++tryCnt)
            {
                if (turn == _board.NowTurn) break;
                Task.Delay(500); // 500ms
            }
            if (tryCnt == MAX_TRYCOUNT)
            {
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] try 1200회... 10분을 기다렸는데 턴 동기화가 안 되었다. {turn} / {_board.NowTurn}");
                new TimerSyncData(0f, 0);
            }

            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] 서버 턴과 클라이언트 턴 동기화 완료 : Turn {turn}");

            // 뭣이 타이머가 없다고?
            if (_userTimers.TryGetValue(UID, out UserTimer? timer) == false)
            {
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Timer Sync] {_gameRoomManager.ServerContext.UserInfo(UID)} 유저가 유저 타이머를 보유하고 있지 않다?!");
                return new TimerSyncData(0f, 0);
            }

			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] {_gameRoomManager.ServerContext.UserInfo(UID)} 서버 타이머 현황 : {timer.SyncData.MainTime}초 / 잔여 초읽기 {timer.SyncData.ByoyomiCount}회");

            // 서버 타이머 값보다 클라이언트 데이터값이 더 작으면 클라이언트 데이터 승인
            if (timer >= clientTimerData)
            {
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] {_gameRoomManager.ServerContext.UserInfo(UID)}유저의 타이머가 승인되었습니다.");
                timer.SynchroTimer(clientTimerData);
            }
            else
            {
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Timer Sync] {_gameRoomManager.ServerContext.UserInfo(UID)}유저의 타이머가 승인되지 않았습니다.");
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Timer Sync] Client Timer Data : 메인타임 {clientTimerData.MainTime} / 초읽기 {clientTimerData.ByoyomiCount}회");
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Timer Sync] Server Timer Data : 메인타임 {timer.MainTime} / 초읽기 {timer.ByoyomiCount}회");
            }

            // 승인 되지 않았다면 서버 타이머 데이터를 그대로 보냄
            return timer.SyncData;
		}

		public void Surrender(ulong UID)
        {
            _userEndCodes[UID] = GameEndCode.SurrenderLose;

            _winnerUID = GetOpponent(UID);
            _userEndCodes[_winnerUID] = GameEndCode.SurrenderWin;

            // 착수 중에 온 것이 아니므로 따로 호출
            FinishGame();
        }

        public void RequestTakeBack(ulong UID)
        {
            // 마지막 무르기 요청 시간을 캐싱해둔다
			_lastTakebackRequestTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			if (_timeOutTimer.TryGetValue(UID, out Timer? myTimerCallback) == false)
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [무르기 요청] 무르기를 요청한 유저 {_gameRoomManager.ServerContext.UserInfo(UID)}의 타이머 콜백이 등록되어 있지 않습니다!!!");
			
            // 일단 무르기 요청이 들어왔으니 내 시간패 타이머 함수를 정지
			myTimerCallback?.Change(Timeout.Infinite, Timeout.Infinite);

			ulong opponent = GetOpponent(UID);
            if (_waitingEventMap.TryGetValue(opponent, out GomokuGameEventWaitingPlayer? waitingGameEndPlayer))
            {
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_gameRoomManager.ServerContext.UserInfo(opponent)}유저에게 무르기 요청 전달!");
                waitingGameEndPlayer.TaskCompSrc?.TrySetResult(new SC_WaitEventDTO(IngameRequestType.TakeBack));

                // 대기 끝, 상대 플레이어에게 이벤트 응답을 보내라
                _waitingEventMap.Remove(opponent);
            }
            else
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_gameRoomManager.ServerContext.UserInfo(_currentTurnUID)}현재 턴 유저가 Turn Request를 보낸 적 없음");
        }

        public void TakeBackResult(ulong permitterUID, bool isTakebackable)
        {
            lock (_gameroomLock)
            {
                long takebackWaitTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastTakebackRequestTime;
                _gameProgressMilliseconds += takebackWaitTime;

                SC_WaitEventDTO endEvent = new SC_WaitEventDTO(IngameRequestType.TakeBackResult);
                endEvent.IsTakeBackSuccess = isTakebackable && takebackWaitTime < 11000; // 무르기 요청이 온 후 15000ms 이내로 온 무르기 승인에만 무르기 성공

                if (endEvent.IsTakeBackSuccess) _board.TryTakeBack();
                
                
                // 모든 대기중인 이벤트에 무르기 결과를 전송
				foreach (GomokuGameEventWaitingPlayer waitingPlayer in _waitingEventMap.Values)
				{
					_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Takeback Result] 이벤트 대기자({GetColor(waitingPlayer.UID)}){_gameRoomManager.ServerContext.UserInfo(waitingPlayer.UID)}에게 무르기 승인 결과 이벤트 응답");              
					waitingPlayer.TaskCompSrc?.TrySetResult(endEvent);                   
                }
                _waitingEventMap.Clear();


                ulong takebackRequester = GetOpponent(permitterUID);
                if (_userTimers.TryGetValue(takebackRequester, out UserTimer? opponentTimer) == false)
                {
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Takeback Result] {_gameRoomManager.ServerContext.UserInfo(takebackRequester)}의 타이머가 없습니다!!!");
                }
                else
                {
                    // 데드라인값 : 무르기 요청했던 녀석의 시간패 시각
                    long waitingTime = opponentTimer.DeadLine(_gameProgressMilliseconds) - _gameProgressMilliseconds;

                    // 무르기 요청했던 녀석의 타임아웃 이벤트를 동작시킨다.
                    if (_timeOutTimer.TryGetValue(takebackRequester, out Timer? opponentTimerCallback) == false)
                        _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Takeback Result] {_gameRoomManager.ServerContext.UserInfo(takebackRequester)}의 타이머 콜백이 등록되지 않았습니다!!!");

                    opponentTimerCallback?.Change(waitingTime, -1L);
                }
            }
		}

        public void PurchaseCountdownLife(ulong UID)
        {
            lock (_gameroomLock)
            {
                // 유저가 돈이 있을 경우 승인하고 상대방에게 이벤트로 전달
                if (true)
                {
                    if (_timeOutTimer.TryGetValue(UID, out Timer? myTimerCallback) == false)
                    {
                        _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Purchase Countdown] {_gameRoomManager.ServerContext.UserInfo(UID)}초읽기 구매를 요청한 유저의 타이머 콜백이 등록되어 있지 않습니다!!!");
                        return;
                    }

                    // 타이머 추가 처리 필요
                    if (_userTimers.TryGetValue(UID, out UserTimer? myTimer) == false)
                    {
                        _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Purchase Countdown] {_gameRoomManager.ServerContext.UserInfo(UID)}유저의 유저타이머를 찾지 못했습니다.");
                        return;
                    }

                    // 초읽기 구매 요청이 들어왔으니 내 시간패 타이머 함수를 정지
                    myTimerCallback?.Change(Timeout.Infinite, Timeout.Infinite);

                    myTimer?.ByoyomiPurchased(_gameRoomManager.ServerContext.DefaultTimerSetting.ByoyomiPurchaseAmount);

                    ulong opponent = GetOpponent(UID);
                    if (_waitingEventMap.TryGetValue(opponent, out GomokuGameEventWaitingPlayer? waitingGameEndPlayer))
                    {
                        _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_gameRoomManager.ServerContext.UserInfo(opponent)}상대방 유저에게 초읽기 구매했음을 알림!");
                        waitingGameEndPlayer.TaskCompSrc?.TrySetResult(new SC_WaitEventDTO(IngameRequestType.PurchaseByoyomi));

                        // 대기 끝, 현재 턴인 플레이어에게 응답을 보내라
                        _waitingEventMap.Remove(opponent);
                    }

                    _gameProgressMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    // 데드라인값 : 착수 응답을 보내는 놈의 시간패 시각
                    long waitingTime = myTimer!.DeadLine(_gameProgressMilliseconds) - _gameProgressMilliseconds;
                    myTimerCallback?.Change(waitingTime, -1L);
                }
            }
        }

        public bool RequestRematch(ulong UID, bool isRematch)
        {
            // 일단 게임이 끝났는지부터 확인, 클라 뚜따인 경우 재대결 요청이 겜중에도 들어올 수도 있다
            if (State != GameRoomState.Finished)
                return false;

            // 리매치 요청을 두 클라가 다 한경우 재대결 성립

            // 플레이어 하나라도 리매치를 거절한 경우
            if (isRematch == false)
            {
                // 죽은 클라일 수도 있으니 무한 대기 방지를 위한 한계 설정
                const int MAX_TRYCOUNT = 25;
                int tryCnt = 0;
                for (; tryCnt < MAX_TRYCOUNT; ++tryCnt)
                {
                    if (_waitingRematchMap.Count < 2)
                    {
                        Task.Delay(500);
                        continue;
                    }
                }
                if (tryCnt == MAX_TRYCOUNT)
                {
                    // 아무래도 클라가 뒤져버려서 이벤트 대기가 오랫동안 못 온 모양이다.
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Rematch Request] Room [{RoomID}] : 리매치 결과 응답을 하기 위한 클라이언트가 2명이 아닙니다. 해당 방의 일부 혹은 모든 클라이언트가 연결이 끊긴 것 같습니다.");
                }


                // 모든 대기중인 리매치 이벤트에 리매치 실패를 전송
                foreach (GomokuRematchWaitingPlayer waitingPlayer in _waitingRematchMap.Values)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Rematch Result] 이벤트 대기자({GetColor(waitingPlayer.UID)}){_gameRoomManager.ServerContext.UserInfo(waitingPlayer.UID)}에게 리매치 실패 응답");
                    waitingPlayer.TaskCompSrc?.TrySetResult(new SC_RematchResultDTO(false, GetColor(waitingPlayer.UID)));
                }
                
                _waitingEventMap.Clear();
                _gameRoomManager.CloseRoom(this);
                return false;
            }
            else
            {
                _waitingRematchQueue.Enqueue(UID);
            }


            // 무식한 복붙 코딩, 추후 리팩토링 예정
            // 두 플레이어가 모두 true인 경우
            if (_waitingRematchQueue.Count == 2)
            {
                // 죽은 클라일 수도 있으니 무한 대기 방지를 위한 한계 설정
                const int MAX_TRYCOUNT = 25;
                int tryCnt = 0;
                for (; tryCnt < MAX_TRYCOUNT; ++tryCnt)
                {
                    if (_waitingRematchMap.Count < 2)
                    {
                        Task.Delay(500);
                        continue;
                    }
                }
                if (tryCnt == MAX_TRYCOUNT)
                {
                    // 아무래도 클라가 뒤져버려서 이벤트 대기가 오랫동안 못 온 모양이다.
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Rematch Request] Room [{RoomID}] : 리매치 결과 응답을 하기 위한 클라이언트가 2명이 아닙니다. 해당 방의 일부 혹은 모든 클라이언트가 연결이 끊긴 것 같습니다.");
                }

                // 다음 판 색 변경 정책은 여기서, 색 변경 후에 재대결 결과로 돌 색을 전달해야 함
                int colorRandomValue = new Random().Next(0, 2);
                ulong beforeBlack = BlackPlayerUID;
                ulong beforeWhite = WhitePlayerUID;
                if (colorRandomValue == 1)
                {
                    BlackPlayerUID = beforeWhite;
                    WhitePlayerUID = beforeBlack;
                }

                // 모든 대기중인 리매치 이벤트에 리매치 성공을 전송
                foreach (GomokuRematchWaitingPlayer waitingPlayer in _waitingRematchMap.Values)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Rematch Result] 이벤트 대기자({GetColor(waitingPlayer.UID)}){_gameRoomManager.ServerContext.UserInfo(waitingPlayer.UID)}에게 리매치 성공 응답");

                    
                    SC_RematchResultDTO rematchResult = new SC_RematchResultDTO(true, GetColor(waitingPlayer.UID));


                    PlayerStatus? opponentStatus = _gameRoomManager.ServerContext.GetPlayerStatus(UID);
                    PlayerMoney? opponentMoney = _gameRoomManager.ServerContext.GetPlayerMoney(UID);
                    PlayerBattleRecord? opponentBattleRecord = _gameRoomManager.ServerContext.GetPlayerBattleRecord(UID);
                    if (opponentBattleRecord == null || opponentStatus == null || opponentMoney == null)
                    {
                        _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Rematch Request] Room [{RoomID}] : 게임 종료 결과 갱신을 위한 {UID} 플레이어의 정보 탐색 실패");
                        return false;
                    }

                    rematchResult.OpponentPlayer = new RematchOpponentData
                    (
                        level : opponentStatus.Level,
                        rating : opponentStatus.Rating,
                        winCount : opponentBattleRecord.WinCount,
                        loseCount : opponentBattleRecord.LoseCount,
                        drawCount : opponentBattleRecord.DrawCount
                    );

                    waitingPlayer.TaskCompSrc?.TrySetResult(rematchResult);
                }

                _waitingEventMap.Clear();
                _gameRoomManager.CloseRoom(this);
                _gameRoomManager.CreateRoom(BlackPlayerUID, WhitePlayerUID);
                
            }
            else if (_waitingRematchQueue.Count > 2)
            {
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Rematch Request] Room [{RoomID}] : Thread Unsafe Error! 재대결 대기 큐의 인원이 2명 초과입니다.");
                return false;
            }

            

            return true;
        }

		// 구버전 코드기는 한데 혹시 몰라서 일단 저장, 추후 제거할듯
		public void CheckHeartbeat(ulong UID)
		{
            // 인게임 락
			lock (_gameroomLock)
			{
				foreach (var pair in _lastRequestTime)
				{
					if ((DateTime.UtcNow - pair.Value).TotalSeconds > 10)
					{
						//FinishGame(GetOpponent(pair.Key), GameEndCode.Disconnect);
						return;
					}
				}
			}
		}

        // 착수, 항복에서 호출
        private void FinishGame()
		{
			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Finish Game] Game Finished!");
			if (State == GameRoomState.Finished)
                return;

            State = GameRoomState.Finished;
			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 승리한 유저 : {_winnerUID}");

            ProcessGameResult();

			DispatchGameEndToAll();

            // 레이팅, 경험치, 돈, 승패, DB갱신등등 싹다 여기서

			// 방 정리 정책은 여기서
			// 재도전 가능한지 물어보고 재도전 안 하면 방 닫아야 함
			// _gameRoomManager.CloseRoom(this);
		}

        private GameRecord ComposeGameRecord(ulong UID)
        {
            GameRecord result = new GameRecord();
            if (_userEndCodes.TryGetValue(UID, out GameEndCode endCode) == false)
            {
                // 큰일나는 예외 상황, Assert 상황이지만 있을 수 있으니 방심할 수 없다.
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Game Record] ({GetColor(UID)})[{UID}]의 엔드코드를 찾을 수 없었습니다!");
            }
            else
            {
                result.EndCode = endCode;

                PlayerStatus? playerStatus = _gameRoomManager.ServerContext.GetPlayerStatus(UID);
                PlayerStatus? opponentStauts = _gameRoomManager.ServerContext.GetPlayerStatus(GetOpponent(UID));
                PlayerMoney? playerMoney = _gameRoomManager.ServerContext.GetPlayerMoney(UID);
                PlayerBattleRecord? playerBattleRecord = _gameRoomManager.ServerContext.GetPlayerBattleRecord(UID);
                if (playerBattleRecord == null || playerStatus == null || playerMoney == null)
                {
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Game Record] ({GetColor(UID)})[{UID}]의 플레이어 스테이터스, 재화, 전적 검색에 실패했습니다!");
                    return result;
                }


                float gameResultValue = 0f;
               
                if (_winnerUID == UID)
                {
                    ++playerBattleRecord.WinCount;
                    playerStatus.AddExperience(75);
                    playerMoney.GameMoney += 125;
                    gameResultValue = 1f;
                    
                }
                else if (_winnerUID == 0)
                {
                    ++playerBattleRecord.DrawCount;
                    playerStatus.AddExperience(50);
                    playerMoney.GameMoney += 75;
                    gameResultValue = 0.5f;
                }
                else
                {
                    ++playerBattleRecord.LoseCount;
                    playerStatus.AddExperience(15);
                    playerMoney.GameMoney += 25;
                }

                Func<float, float, float> EloRating = (rating, opponentRating) =>
                {
                    double predictedWinrate = 1 / (1 + Math.Pow(10, (opponentRating - rating) / 400));
                    return rating + K_Factor * (gameResultValue - (float)predictedWinrate);
                };

                if (opponentStauts != null)
                    playerStatus.Rating = EloRating(playerStatus.Rating, opponentStauts.Rating);

                result.Rating = playerStatus.Rating;
                result.Level = playerStatus.Level;
                result.ExperiencePoint = playerStatus.ExperiencePoint;
                result.MaxExperiencePoint = playerStatus.MaxExperiencePoint;
                result.GameMoney = playerMoney.GameMoney;
                result.WinCount = playerBattleRecord.WinCount;
                result.DrawCount = playerBattleRecord.DrawCount;
                result.LoseCount = playerBattleRecord.LoseCount;
            }
            return result;
        }

        private void ProcessGameResult()
        {
            // 죽은 클라일 수도 있으니 무한 대기 방지를 위한 한계 설정
            // 5초 정도면 많이 기다려 줬다.
            const int MAX_TRYCOUNT = 10;
            int tryCnt = 0;
            for (; tryCnt < MAX_TRYCOUNT; ++tryCnt)
            {
                if (_waitingResultMap.Count < 2)
                {
                    Task.Delay(500);
                    continue;
                }
            }
            if (tryCnt == MAX_TRYCOUNT)
            {
                // 아무래도 클라가 뒤져버려서 게임 응답이 못 온 모양이다.
                _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [Game Result] Room [{RoomID}] : 게임 결과 응답을 하기 위한 클라이언트가 2명이 아닙니다. 해당 방의 일부 혹은 모든 클라이언트가 연결이 끊긴 것 같습니다.");
            }

            // 플레이어들에게 게임 종료 정보를 보낸다
            foreach (GomokuGameResultWaitingPlayer waitingPlayer in _waitingResultMap.Values)
            {
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 이벤트 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]에게 게임 종료 응답");
                waitingPlayer.TaskCompSrc?.TrySetResult(ComposeGameRecord(waitingPlayer.UID));
                
                waitingPlayer.CancellationTokenRegist.Dispose();
            }

           
            _waitingEventMap.Clear();
        }

        // 착수 도중 호출
        private void OnBlackWin()
		{
			_userEndCodes[BlackPlayerUID] = GameEndCode.GomokuWin;
			_userEndCodes[WhitePlayerUID] = GameEndCode.GomokuLose;
			_winnerUID = BlackPlayerUID;
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 흑돌의 오목 승리!");
		}

        // 착수 도중 호출
		private void OnWhiteWin()
		{
            _userEndCodes[BlackPlayerUID] = GameEndCode.GomokuLose;
            _userEndCodes[WhitePlayerUID] = GameEndCode.GomokuWin;			
			_winnerUID = WhitePlayerUID;
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 백돌의 오목 승리!");
		}

        // 225수 흑 착수 도중 호출
		private void OnDraw()
		{
			_userEndCodes[BlackPlayerUID] = _userEndCodes[WhitePlayerUID] = GameEndCode.Draw;
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 오목 무승부 발생!");
            FinishGame();
		}

        // 백 착수 도중 호출 (백이 여길 두면 흑이 더이상 둘 곳이 없어서 백이 승리 판정)
        private void OnBlackUnmovable()
        {
            _userEndCodes[BlackPlayerUID] = _userEndCodes[WhitePlayerUID] = GameEndCode.BlackUnmovable;
            _winnerUID = WhitePlayerUID;
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 흑돌의 남은 위치 모두 금수로 인한 자동 패배. 백돌의 오목 승리!");
		}

        // 항상 락 내부에서 실행되어야 하는 함수
        private void ProcessTimeOut()
        {
            // MyTurn 응답 대기중인 플레이어들에게 전부 게임 결과를 뿌린다?
            // 현재 턴인 유저에게만 뿌린다.
            if (_waitingEventMap.TryGetValue(_currentTurnUID, out GomokuGameEventWaitingPlayer? waitingGameEndPlayer))
            {
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [TimeOut] {_gameRoomManager.ServerContext.UserInfo(_currentTurnUID)}현재 턴 유저에게 타임아웃으로 인한 빈 이벤트 응답");
                waitingGameEndPlayer.TaskCompSrc?.TrySetResult(new SC_WaitEventDTO(IngameRequestType.None));

                // 대기 끝, 현재 턴인 플레이어에게 응답을 보내라
                _waitingEventMap.Remove(_currentTurnUID);
            }
            else
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [TimeOut] {_gameRoomManager.ServerContext.UserInfo(_currentTurnUID)}현재 턴 유저가 Turn Request를 보낸 적 없음");


            ulong opponent = GetOpponent(_currentTurnUID);
            // 현재 턴인 플레이어가 타임아웃이므로 타임아웃당한 유저의 착수를 대기중인 상대에게 에러 착수를 응답시켜줌
            if (_waitingPlaceStoneMap.TryGetValue(opponent, out GomokuIngamePlaceStoneWaitingPlayer? waitingPlaceStonePlayer))
            {
                waitingPlaceStonePlayer.TaskCompSrc?.TrySetResult(new SC_OpponentPlaceStoneDTO(new TimerSyncData(0f, 0), 255, 255));
                _waitingPlaceStoneMap.Remove(opponent);
            }

            FinishGame();
        }

        private void OnBlackTimeOut(object? parameter)
        {
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] Black TimeOut 발생! 백돌의 시간 승리.");
            lock (_gameroomLock)
            {
                _userEndCodes[BlackPlayerUID] = GameEndCode.TimeOutLose;
                _userEndCodes[WhitePlayerUID] = GameEndCode.TimeOutWin;
                _winnerUID = WhitePlayerUID;
                ProcessTimeOut();				
			}
        }

        private void OnWhiteTimeOut(object? parameter)
        {
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] White TimeOut 발생! 흑돌의 시간 승리.");
            lock (_gameroomLock)
            {
                _userEndCodes[BlackPlayerUID] = GameEndCode.TimeOutWin;
                _userEndCodes[WhitePlayerUID] = GameEndCode.TimeOutLose;
                _winnerUID = BlackPlayerUID;
                ProcessTimeOut();
			}
        }
	
		private ulong GetOpponent(ulong UID)
			=> UID == BlackPlayerUID ? WhitePlayerUID : BlackPlayerUID;

        public StoneColorType GetColor(ulong UID)
        {
            if (UID == BlackPlayerUID) return StoneColorType.Black;
            if (UID == WhitePlayerUID) return StoneColorType.White;
            // 있을 수 없는 일일까? Assert를 걸어야 할까? 하지만 서버는 Assert 걸면 안 된다.
            _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] {_gameRoomManager.ServerContext.UserInfo(UID)}유저의 돌 색이 이상하다.");
            return StoneColorType.Empty;
        }

        public GameEndCode GetEndCode(ulong UID)
        {
            if (_userEndCodes.TryGetValue(UID, out GameEndCode endCode))
                return endCode;
            return GameEndCode.Error;
        }
	}
}