using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;
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
        public TaskCompletionSource<SC_OpponentPlaceStoneDTO> TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GomokuSpecialGameEndWaitingPlayer
    {
        public ulong UID { get; set; }
        public TaskCompletionSource<GameEndCode> TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GameRoom
	{
        // 방 식별자, 게임룸매니저에서 게임룸 Dictionary를 관리할 때 사용
		public ulong RoomID { get; }

        // 재도전 시 색 변경이 일어날 수 있다면 private set, 없다면 setter 제거
		public ulong BlackPlayerUID { get; private set; }
		public ulong WhitePlayerUID { get; private set; }

		// 방 밖에서 방이 게임 중인지 아닌지를 결정하면 안 된다
		public GameRoomState State { get; private set; }

		// 날 건드릴 수 있는 놈이 플레이어 둘이잖냐, 락 걸어야지
        // 현재 게임 룸 착수 전체 락
        private readonly object _placeStoneLock = new object();

        // 나 자신의 방을 닫을 때 필요함, 로거 꺼내올 때도 씀
		private readonly GameRoomManager _gameRoomManager;

		// 오목판
		private readonly Board _board;

		// 하트비트, n 초 이상 미 요청 시 접속 끊김으로 간주
		private readonly Dictionary<ulong, DateTime> _lastRequestTime;

        // Long Polling 착수 대기자 명단
        private readonly Dictionary<ulong, GomokuIngamePlaceStoneWaitingPlayer> _waitingPlaceStoneMap;
        private readonly Dictionary<ulong, GomokuSpecialGameEndWaitingPlayer> _waitingSpecialGameEndMap;
        private readonly Dictionary<int, TaskCompletionSource<bool>> _synchronizeTimerTurnWaiters;


        // 유저별 타이머 정보
        private readonly Dictionary<ulong, UserTimer> _timers;

        private Timer blackTimeOutTimer;
        private Timer whiteTimeOutTimer;

        // 유저별 게임 결과
        private readonly Dictionary<ulong, GameEndCode> _userEndCodes;


        // 서버에 캐싱된 마지막 타이머 갱신 시간
		private long _gameProgressMilliseconds;

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
            _waitingSpecialGameEndMap = new Dictionary<ulong, GomokuSpecialGameEndWaitingPlayer>();

            // 타이머 동기화 대기
            _synchronizeTimerTurnWaiters = new Dictionary<int, TaskCompletionSource<bool>>(); 

            _timers = new Dictionary<ulong, UserTimer>()
            {
                // default : 180f, 3, 30f
                [blackPlayerUID] = new UserTimer(initMainTime: 180f, initByoyomiCount: 3, byoyomiSeconds: 30f),
                [whitePlayerUID] = new UserTimer(initMainTime: 180f, initByoyomiCount: 3, byoyomiSeconds: 30f)
            };

            blackTimeOutTimer = new Timer(OnBlackTimeOut, null, Timeout.Infinite, Timeout.Infinite);
            whiteTimeOutTimer = new Timer(OnWhiteTimeOut, null, Timeout.Infinite, Timeout.Infinite);
 
            // _endReason = GameEndCode.None; // 이 방에서 게임이 끝난 이유. None은 지금 게임중이라는 뜻
            _userEndCodes = new Dictionary<ulong, GameEndCode>
            {
                [blackPlayerUID] = GameEndCode.None,
                [whitePlayerUID] = GameEndCode.None
            };

			_lastRequestTime = new Dictionary<ulong, DateTime>
			{
				[blackPlayerUID] = DateTime.UtcNow,
				[whitePlayerUID] = DateTime.UtcNow
			};
        }

        // UID 대기 취소 (착수)
        public void CancelWaitPlaceStone(ulong UID)
        {
            lock (_placeStoneLock)
            {
				_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] {RoomID} 방 : [{UID}] 대기 취소\n");
				if (_waitingPlaceStoneMap.TryGetValue(UID, out GomokuIngamePlaceStoneWaitingPlayer waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc.TrySetCanceled();
                    _waitingPlaceStoneMap.Remove(UID);
                }
            }
        }

        // UID 대기 취소 (게임종료)
        public void CancelWaitSpecialGameEnd(ulong UID)
        {
            lock (_placeStoneLock)
            {
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] {RoomID} 방 : [{UID}] 대기 취소\n");
                if (_waitingSpecialGameEndMap.TryGetValue(UID, out GomokuSpecialGameEndWaitingPlayer waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc.TrySetCanceled();
                    _waitingSpecialGameEndMap.Remove(UID);
                }
            }
        }



        // 상대방 착수 대기 이벤트 등록 (Long - Polling)
        public Task<SC_OpponentPlaceStoneDTO> WaitNextPlaceStoneAsync(ulong UID, CancellationToken ct)
        {
            lock (_placeStoneLock)
            {
				_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] Room {RoomID} : {UID} 상대 착수 대기\n");

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

        // 특수 게임 종료 대기 이벤트 등록 (Long - Polling)
        public Task<GameEndCode> WaitGameEndAsync(ulong UID, CancellationToken ct)
        {
            lock (_placeStoneLock)
            {
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] Room {RoomID} : {UID} 특수 게임 종료 대기\n");

                // 대기자용 TCS 조립 (대기 결과 반환용)
                TaskCompletionSource<GameEndCode> tcs
                    = new TaskCompletionSource<GameEndCode>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                // 대기자 데이터 조립 및 대기 등록 (Long Poll)
                _waitingSpecialGameEndMap[UID] = new GomokuSpecialGameEndWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWaitSpecialGameEnd(UID))
                };

                return tcs.Task;
            }
        }

        private Task WaitForSynchronizeTimerTurnAsync(int turn)
        {
            lock (_synchronizeTimerTurnWaiters)
            {
                // 이미 턴이 같으면 바로 완료
                if (_board.NowTurn == turn) return Task.CompletedTask;

                if (!_synchronizeTimerTurnWaiters.TryGetValue(turn, out TaskCompletionSource<bool> tcs))
                {
                    tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _synchronizeTimerTurnWaiters[turn] = tcs;
                }
                return tcs.Task;
            }
        }

       

        // 보드와 타이머 쪽에서 재대결 리벤지 정의가 안 되었기 때문에 일단 대충 구색만 맞춰둠
        /*
        private void RestartGame()
        {
            _board.Reset();
            _timer.Reset();

            _endReason = GameEndCode.None;
            _winnerUID = 0;
            State = GameRoomState.Playing;

            _rematchAccepted.Clear();

            _currentTurnUID = BlackPlayerUID;

            // 시작 이벤트 전파
            DispatchGameStart();
        }
        */

        // 게임 종료 작업, Dispose는 필수 (Unmanaged Heap)
        private void DispatchGameEndToAll()
        {
			_gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {RoomID} 방에서 게임 종료 작업 수행!\n");
			// 대기자 명단에서 대기 플레이어를 제거 및 종료 이벤트를 조립해서 던져줌
			foreach (GomokuIngamePlaceStoneWaitingPlayer waitingPlayer in _waitingPlaceStoneMap.Values)
            {
                if (_timers.TryGetValue(waitingPlayer.UID, out UserTimer waitingUserTimer) == false)
                {
                    // 큰일나는 예외 상황, Assert 상황이지만 있을 수 있으니 방심할 수 없다.
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]의 타이머를 찾을 수 없었습니다!");
                }
                
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]에게 종료 이벤트 넘김");
                SC_OpponentPlaceStoneDTO endEvent = new SC_OpponentPlaceStoneDTO(waitingUserTimer.SyncData, 255, 255, _userEndCodes[waitingPlayer.UID]);
                waitingPlayer.TaskCompSrc.TrySetResult(endEvent);
                waitingPlayer.CancellationTokenRegist.Dispose();
            }

            _waitingPlaceStoneMap.Clear();

            
            // Unmanaged Heap
            blackTimeOutTimer?.Dispose(); 
            whiteTimeOutTimer?.Dispose();
        }

        public bool TryGameStart()
        {
            // 백돌 첫 턴 시작 시간 갱신용
            if (_gameProgressMilliseconds == 0) _gameProgressMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] {RoomID} 방에서 게임 시작 작업 수행\n시작 ms : {_gameProgressMilliseconds}");
			return true;
			// return State == GameRoomState.Waiting; // 흑돌 착수 후 방의 상태가 플레잉으로 바뀐 다음 백돌의 시작 요청이 올 수 있다...
		}

		public PlaceStoneResultType PlaceStone(ulong UID, int x, int y)
		{
			lock (_placeStoneLock)
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
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 첫 수인데 흑돌이 아닌 {UID} 유저가 착수 요청을 했습니다!\n");
                        return PlaceStoneResultType.Invalid; // 다른걸 생각해봐야 할 듯
                    }

                    // 흑돌은 무조건 첫 수 정 중앙이기 때문에 이건 클라 뚜껑 딴게 맞음, 
                    if (x != 7 || y != 7)
                    {
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 첫 수인데 {UID} 흑 유저가 (7,7) 위치에 두지 않았습니다...!\n");
                        return PlaceStoneResultType.Invalid;
                    }
                    
                    // 게임 대기 상태가 아닌데 첫 수라고?
                    if (State != GameRoomState.Waiting)
                    {
                        // 리벤지 쪽 구현 이상하면 여기 들어올 수도 있음
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 첫 수인데 게임 룸의 상태가 대기중이 아닙니다!\n");
                        return PlaceStoneResultType.Invalid;
                    }

                    // 흑돌 첫 수 두는 순간 게임이 시작됨 (이 이후 백돌의 시작 요청이 올 수 있음)
                    State = GameRoomState.Playing;
                }

                if (IsNowTurnBlack)
                    blackTimeOutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                else
                    whiteTimeOutTimer.Change(Timeout.Infinite, Timeout.Infinite);


                // 타이머 진행
                // [흑].프로그레스(흑턴 시작 시간, 흑턴 착수 정보가 온 시간)
                if (_timers.TryGetValue(UID, out UserTimer myTimer))
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [{UID}] 타이머 진행 전 : 서버시간 {_gameProgressMilliseconds}ms - {myTimer.MainTime} / {myTimer.ByoyomiCount} / {myTimer.NowByoyomiSeconds}");
                    myTimer.ProgressExcludingTol(_gameProgressMilliseconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [{UID}] 타이머 진행 후 : 서버시간 {_gameProgressMilliseconds}ms - {myTimer.MainTime} / {myTimer.ByoyomiCount} / {myTimer.NowByoyomiSeconds}");
                }
				
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_board.NowTurn}턴 시작 : {(IsNowTurnBlack ? "Black" : "White")} [{_currentTurnUID}] 차례");
				
                // TryMoveStone이 true일 시 착수 성공. 내부적으로 승패 처리까지 동작하며 _board에 등록한 event들이 실행됨
                if (_board.TryMoveStone(x, y) == false)
				{
                    // 빈 곳이 아닌데 두려고 시도했거나, 흑돌이 금수 위치에 두려고 시도함. 클라 변조 체크
                    return PlaceStoneResultType.Occupied;
                }
                // 착수 성공 및 게임 결과 처리
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_board.NowTurn}턴 종료");
                OnBoardTurnUpdated();

                ulong opponent = GetOpponent(UID);
                
                // 게임 룸 타이머 갱신
                _gameProgressMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (_timers.TryGetValue(opponent, out UserTimer opponentTimer) == false)
                {
                    _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] 상대 타이머가 없습니다!!!");
                }
                else
                {
                    // 데드라인값 : 착수 응답을 보내는 놈의 시간패 시각
                    long oppoWaitingTime = opponentTimer.DeadLine(_gameProgressMilliseconds) - _gameProgressMilliseconds; // 기다렸다가 시간패 하도록 예약을 해뒀다가
                    // 지금 가장 큰 문제는 타이머를 착수를 받을 때마다 갱신하는데 착수를 안 했어. 이 때 시간패임.

                    // oppoWaitingTime만큼 기다렸다가 양 유저에게 시간패/시간승 처리를 하는 함수 실행 (PlaceStone()과는 비동기)
                    // 착수 정보가 들어올 때마다 시간패 예약은 취소

                    // 현재 흑돌 차례면 백돌의 타이머를 키는게 맞다. 
                    // 하지만 _board.TryMoveStone가 호출되어 Turn 값이 1 올라서 이미 내 턴은 끝나고 상대 턴 넘어간 취급
                    if (IsNowTurnBlack)
                        blackTimeOutTimer.Change(oppoWaitingTime, -1L);
                    else
                        whiteTimeOutTimer.Change(oppoWaitingTime, -1L);
                }

                // 내 상대가 대기 중이면 내가 착수한 정보를 대기중인 상대 이벤트로 등록해서 응답시켜줌
                if (_waitingPlaceStoneMap.TryGetValue(opponent, out GomokuIngamePlaceStoneWaitingPlayer waitingPlaceStonePlayer))
                {
                    if (_userEndCodes.TryGetValue(opponent, out GameEndCode endReason))
                    {
                        waitingPlaceStonePlayer.TaskCompSrc.TrySetResult(
                        new SC_OpponentPlaceStoneDTO(myTimer.SyncData, (byte)x, (byte)y, endReason));
                        // DTO 조립하고 결과를 넣어 줬으니 상대의 대기는 끝났고 응답을 보내줘야지
                        _waitingPlaceStoneMap.Remove(opponent);
                    }
                }

                // 착수 성공했으니 특수 게임 종료는 아니다. 착수자에게 Game End None을 보낸다
                if (_waitingSpecialGameEndMap.TryGetValue(UID, out GomokuSpecialGameEndWaitingPlayer waitingGameEndPlayer))
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {UID} 유저의 MyTurn 응답으로 None 전송");
                    waitingGameEndPlayer.TaskCompSrc.TrySetResult(GameEndCode.None);
                        
                    // 대기 끝, 응답을 보내줘야지
                    _waitingSpecialGameEndMap.Remove(UID);
                }

                // 이번 착수로 네가 지금 즉시 승리했음
                if (UID == _winnerUID)
				{
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {UID} 차례에서 승리");
                    FinishGame();
					return PlaceStoneResultType.NowWin;
				}

                // 항복, 연결끊김 처리도 해야함


                


				// 게임 안 끝났네, 상대 턴으로 넘김
				if (State != GameRoomState.Finished && _winnerUID == 0UL)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_currentTurnUID} 차례에서 {GetOpponent(UID)}으로 턴 교체");
                    _currentTurnUID = opponent;
                }

				return PlaceStoneResultType.Success;
			}
		}
        
        // 보드 턴이 업데이트 되었다면 타이머 싱크로나이즈로 전달
        private void OnBoardTurnUpdated()
        {
            lock (_placeStoneLock)
            {
                if (_synchronizeTimerTurnWaiters.TryGetValue(_board.NowTurn, out TaskCompletionSource<bool> tcs))
                {
                    tcs.SetResult(true); // 대기 Task 완료
                    _synchronizeTimerTurnWaiters.Remove(_board.NowTurn);
                }
            }
        }

        public TimerSyncData SynchronizeTimerAsync(ulong UID, int turn, TimerSyncData clientTimerData)
        {
            _gameRoomManager.Logger.LogDebug($"[{DateTime.Now}] [Timer Sync] Client Turn {turn} / Server Board Turn {_board.NowTurn}");

            // 개 등신 코드인데 일단은 이렇게라도 동작시켜
            int loopCount = 0;
            while (turn != _board.NowTurn)
            {
                ++loopCount;
            }

            // 이벤트 기반으로 안전하게 턴 대기, 기존 while busy waiting 으로 인한 무식한 CPU 점유 제거
            //await WaitForSynchronizeTimerTurnAsync(turn);

            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 서버 턴과 클라 턴 동기화 완료 : Turn {turn} / busy Waiting 루프 횟수 : {loopCount}");

            // 뭣이 타이머가 없다고?
            if (_timers.TryGetValue(UID, out UserTimer timer) == false) return new TimerSyncData(0f, 0);

            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] {UID} 서버 타이머 현황 : {timer.SyncData.MainTime}초 / 잔여 초읽기 {timer.SyncData.ByoyomiCount}회");
            
            // 서버 타이머 값보다 클라이언트 데이터값이 더 작으면 클라이언트 데이터 승인
            if (timer >= clientTimerData) timer.SynchroTimer(clientTimerData);
            
            // 승인 되지 않았다면 서버 타이머 데이터를 그대로 보냄
            return timer.SyncData;
        }


        public StoneColorType GetColor(ulong UID)
		{
			if (UID == BlackPlayerUID) return StoneColorType.Black;
			if (UID == WhitePlayerUID) return StoneColorType.White;
            // 있을 수 없는 일일까? Assert를 걸어야 할까? 하지만 서버는 Assert 걸면 안 된다.
            _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [{UID}]유저의 돌 색이 이상하다.");
            return StoneColorType.Empty;
		}

		// 구버전 코드기는 한데 혹시 몰라서 일단 저장, 추후 제거할듯
		public void CheckHeartbeat()
		{
            // 인게임 락
			lock (_placeStoneLock)
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

        private void FinishGame()
		{
            if (State == GameRoomState.Finished)
                return;

            State = GameRoomState.Finished;
			_gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] 승리한 유저 : {_winnerUID}");

			DispatchGameEndToAll();

			// 방 정리 정책은 여기서
			// 재도전 가능한지 물어보고 재도전 안 하면 방 닫아야 함
			// _gameRoomManager.CloseRoom(this);
		}

		private void OnBlackWin()
		{
			_userEndCodes[BlackPlayerUID] = GameEndCode.GomokuWin;
			_userEndCodes[WhitePlayerUID] = GameEndCode.GomokuLose;
			_winnerUID = BlackPlayerUID;
        }

		private void OnWhiteWin()
		{
            _userEndCodes[BlackPlayerUID] = GameEndCode.GomokuLose;
            _userEndCodes[WhitePlayerUID] = GameEndCode.GomokuWin;			
			_winnerUID = WhitePlayerUID;
        }

		private void OnDraw()
		{
			_userEndCodes[BlackPlayerUID] = _userEndCodes[WhitePlayerUID] = GameEndCode.Draw;
        }

        private void OnBlackUnmovable()
        {
            _userEndCodes[BlackPlayerUID] = _userEndCodes[WhitePlayerUID] = GameEndCode.BlackUnmovable;
            _winnerUID = WhitePlayerUID;
        }

        private void ProcessTimeOut()
        {
            lock (_placeStoneLock)
            {
                // MyTurn 응답 대기중인 플레이어들에게 전부 게임 결과를 뿌린다?
                // 현재 턴인 유저에게만 뿌린다.
                if (_waitingSpecialGameEndMap.TryGetValue(_currentTurnUID, out GomokuSpecialGameEndWaitingPlayer waitingGameEndPlayer))
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_currentTurnUID} 현재 턴 유저에게 타임아웃 정보 전달!");
                    waitingGameEndPlayer.TaskCompSrc.TrySetResult(GetEndCode(_currentTurnUID));

                    // 대기 끝, 현재 턴인 플레이어에게 응답을 보내
                    _waitingSpecialGameEndMap.Remove(_currentTurnUID);
                }
                else
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_currentTurnUID} 현재 턴 유저가 Turn Request를 보낸 적 없음");


                ulong opponent = GetOpponent(_currentTurnUID);
                // 현재 턴인 플레이어가 타임아웃이므로 내 착수를 대기중인 상대에게 응답시켜줌
                if (_waitingPlaceStoneMap.TryGetValue(opponent, out GomokuIngamePlaceStoneWaitingPlayer waitingPlaceStonePlayer))
                {
                    if (_userEndCodes.TryGetValue(opponent, out GameEndCode endReason))
                    {
                        waitingPlaceStonePlayer.TaskCompSrc.TrySetResult(
                        new SC_OpponentPlaceStoneDTO(new TimerSyncData(0f, 0), 255, 255, endReason));
                        _waitingPlaceStoneMap.Remove(opponent);
                    }
                }
            }
        }

        private void OnBlackTimeOut(object? parameter)
        {
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] Black TimeOut 발생!");
            _userEndCodes[BlackPlayerUID] = GameEndCode.TimeOutLose;
            _userEndCodes[WhitePlayerUID] = GameEndCode.TimeOutWin;
            _winnerUID = WhitePlayerUID;
            ProcessTimeOut();            
        }

        private void OnWhiteTimeOut(object? parameter)
        {
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] White TimeOut 발생!");
            _userEndCodes[BlackPlayerUID] = GameEndCode.TimeOutWin;
            _userEndCodes[WhitePlayerUID] = GameEndCode.TimeOutLose;
            _winnerUID = BlackPlayerUID;
            ProcessTimeOut();
        }



        private ulong GetOpponent(ulong uid)
			=> uid == BlackPlayerUID ? WhitePlayerUID : BlackPlayerUID;

        public GameEndCode GetEndCode(ulong uid)
        {
            if (_userEndCodes.TryGetValue(uid, out GameEndCode endCode))
                return endCode;
            return GameEndCode.Error;
        }
	}
}