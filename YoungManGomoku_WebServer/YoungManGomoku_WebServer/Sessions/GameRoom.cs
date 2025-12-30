using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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

    public class InGameWaitingPlayer
    {
        public ulong UID { get; set; }
        public TaskCompletionSource<SC_OpponentPlaceStoneDTO> TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GameRoom
	{
		public ulong RoomID { get; }

        // 재도전 시 색 변경이 일어날 수 있다면 private set, 없다면 setter 제거
		public ulong BlackPlayerUID { get; private set; }
		public ulong WhitePlayerUID { get; private set; }

		// 방 밖에서 방이 게임 중인지 아닌지를 결정하면 안 된다
		public GameRoomState State { get; private set; }

		// 날 건드릴 수 있는 놈이 플레이어 둘이잖냐, 락 걸어야지
        // 현재 게임 룸 전체 락
        private readonly object _lock = new object();

        // 나 자신의 방을 닫을 때 필요함, 로거 꺼내올 때도 씀
		private readonly GameRoomManager _gameRoomManager;

		// 오목판
		private readonly Board _board;

		// 하트비트, n 초 이상 미 요청 시 접속 끊김으로 간주
		private readonly Dictionary<ulong, DateTime> _lastRequestTime;

        // Long Polling 대기자 명단

        private readonly Dictionary<ulong, InGameWaitingPlayer> _waitingMap;

        // 유저별 타이머 정보
        private readonly Dictionary<ulong, UserTimer> _timers;

		private readonly Dictionary<ulong, GameEndCode> _userEndCodes;



		private long _gameProgressMilliseconds;


        // 현재 이 게임 룸의 게임 종료 사유
        // private GameEndCode _endReason;

        // 현재 턴인 사람의 UID
        private ulong _currentTurnUID;

        // 승패 결정 시 승리자의 UID, 승자가 없거나 게임 도중이면 0
        private ulong _winnerUID;

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
            _board.OnBlackUnmovable += OnWhiteWin;

			// 첫 수는 흑돌
			_currentTurnUID = BlackPlayerUID;

			_waitingMap = new Dictionary<ulong, InGameWaitingPlayer>();

			_timers = new Dictionary<ulong, UserTimer>()
            {
                [blackPlayerUID] = new UserTimer(initMainTime: 180f, initByoyomiCount: 3, byoyomiSeconds: 30f),
                [whitePlayerUID] = new UserTimer(initMainTime: 180f, initByoyomiCount: 3, byoyomiSeconds: 30f)
            };

            // _timers[BlackPlayerUID].OnTimeOut += 흑시간승함수

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

        // UID 대기 취소
        public void CancelWait(ulong UID)
        {
            lock (_lock)
            {
				_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] {RoomID} 방 : [{UID}] 대기 취소\n");
				if (_waitingMap.TryGetValue(UID, out InGameWaitingPlayer waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc.TrySetCanceled();
                    _waitingMap.Remove(UID);
                }
            }
        }

        // 대기 이벤트 등록 (Long - Polling)
        public Task<SC_OpponentPlaceStoneDTO> WaitNextPlaceStoneAsync(ulong UID, CancellationToken ct)
        {
            lock (_lock)
            {
				_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] Room {RoomID} : {UID} 상대 착수 대기\n");

				// 대기자용 TCS 조립 (대기 결과 반환용)
				TaskCompletionSource<SC_OpponentPlaceStoneDTO> tcs
                    = new TaskCompletionSource<SC_OpponentPlaceStoneDTO>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                // 대기자 데이터 조립 및 대기 등록 (Long Poll)
                _waitingMap[UID] = new InGameWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWait(UID))
                };

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
			_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] {RoomID} 방에서 게임 종료 작업 수행!\n");
			// 대기자 명단에서 대기 플레이어를 제거 및 종료 이벤트를 조립해서 던져줌
			foreach (InGameWaitingPlayer waitingPlayer in _waitingMap.Values)
            {
                if (_timers.TryGetValue(waitingPlayer.UID, out UserTimer waitingUserTimer) == false)
                {
                    // 큰일나는 예외 상황, Assert 상황이지만 있을 수 있음
                    _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]의 타이머를 찾을 수 없었습니다!");
                }
                
                _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 대기자({GetColor(waitingPlayer.UID)})[{waitingPlayer.UID}]에게 종료 이벤트 넘김");
                SC_OpponentPlaceStoneDTO endEvent = new SC_OpponentPlaceStoneDTO(waitingUserTimer.SyncData, 255, 255, _userEndCodes[waitingPlayer.UID]);
                waitingPlayer.TaskCompSrc.TrySetResult(endEvent);
                waitingPlayer.CancellationTokenRegist.Dispose();
            }

            _waitingMap.Clear();
        }

        public bool TryGameStart()
        {
            if (_gameProgressMilliseconds == 0) // 백돌 첫 턴 시작 시간 갱신용
			    _gameProgressMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			_gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] {RoomID} 방에서 게임 시작 작업 수행\n시작 ms : {_gameProgressMilliseconds}");
			return true;
			// return State == GameRoomState.Waiting; // 흑돌 착수 후 방의 상태가 플레잉으로 바뀐 다음 백돌의 시작 요청이 올 수 있다...
		}

		public PlaceStoneResultType PlaceStone(ulong uid, int x, int y)
		{
			lock (_lock)
			{
                _lastRequestTime[uid] = DateTime.UtcNow;
				// 클라 변조 유효성 체크, 클라에서 제대로 요청이 들어왔다면 여기 들어올 일이 없음
				{
					// 게임 끝났어
					if (IsFinished)
                        return PlaceStoneResultType.Invalid;

                    // 니 턴 아니야, 근데 첫턴은 네 턴 아닐 수도 있으니 제외
                    if (uid != _currentTurnUID && _board.NowTurn != 0)
						return PlaceStoneResultType.NotYourTurn;
				}

                // 게임 시작, 첫 수 전
                if (_board.NowTurn == 0)
                {
                    // 첫 수인데 흑돌이 아니셔?
                    if (BlackPlayerUID != uid)
                    {
                        // 이건 그럴 수 있음. 백돌 착수 요청이 먼저 도착할 수 있지...
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 첫 수인데 흑돌이 아닌 {uid} 유저가 착수 요청을 했습니다!\n");
                        return PlaceStoneResultType.Invalid; // 다른걸 생각해봐야 할 듯
                    }

                    // 흑돌은 무조건 첫 수 정 중앙이기 때문에 이건 클라 뚜껑 딴게 맞음, 
                    if (x != 7 || y != 7)
                    {
                        _gameRoomManager.Logger.LogInformation($"[{DateTime.Now}] 첫 수인데 {uid} 흑 유저가 (7,7) 위치에 두지 않았습니다...!\n");
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


				// 타이머 진행
				long nowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // [흑].프로그레스(흑턴 시작 시간, 흑턴 착수 정보가 온 시간)
                if (_timers.TryGetValue(uid, out UserTimer myTimer))
                {
                    myTimer.ProgressExcludingTol(_gameProgressMilliseconds, nowTime);
                    //myTimer.DeadLine(_gameProgressMilliseconds);
                }
                

                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [{uid}] 타이머 체크 : {myTimer.MainTime} / {myTimer.ByoyomiCount} / {myTimer.NowByoyomiSeconds}");

				// _board의 NowTurn 값이 홀수면 백, 짝수면 흑 차례라는 뜻
				bool isBlack = (_board.NowTurn & 1) == 0;
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_board.NowTurn}턴 시작 : {(isBlack ? "Black" : "White")} [{_currentTurnUID}] 차례");
				
                // TryMoveStone이 true일 시 착수 성공. 내부적으로 승패 처리까지 동작하며 _board에 등록한 event들이 실행됨
                if (_board.TryMoveStone(x, y) == false)
				{
                    // 빈 곳이 아닌데 두려고 시도했거나, 흑돌이 금수 위치에 두려고 시도함. 클라 변조 체크
                    return PlaceStoneResultType.Occupied;
                }
                // 착수 성공 및 게임 결과 처리
                _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_board.NowTurn}턴 종료");

                ulong opponent = GetOpponent(uid);
                
                // 게임 룸 타이머 갱신
                _gameProgressMilliseconds = nowTime;
                
                if(_timers.TryGetValue(opponent, out UserTimer opponentTimer) == false)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 상대 타이머가 없습니다!!! 크아악");
                }

                // 데드라인값 : 착수 응답을 보내는 놈의 시간패 시각
                long oppoWaitingTime = opponentTimer.DeadLine(_gameProgressMilliseconds) - _gameProgressMilliseconds; // 기다렸다가 시간패 하도록 예약을 해뒀다가
                // 지금 가장 큰 문제는 타이머를 착수를 받을 때마다 갱신하는데
                // 시간패라는건 이새기가 착수를 안 했어. 이때 시간패임.

                // oppoWaitingTime만큼 기다렸다가 양 유저에게 시간패/시간승 처리를 하는 함수 실행 (이거랑은 비동기)
                // 그걸 착수 정보가 들어올 때마다 시간패 예약은 취소

                // 내 상대가 대기 중이면 내가 착수한 정보를 대기중인 상대 이벤트로 등록해서 응답시켜줌
                if (_waitingMap.TryGetValue(opponent, out InGameWaitingPlayer waitingPlayer))
                {
                    if (_userEndCodes.TryGetValue(opponent, out GameEndCode endReason))
                    {
                        waitingPlayer.TaskCompSrc.TrySetResult(
                        new SC_OpponentPlaceStoneDTO(myTimer.SyncData, (byte)x, (byte)y, endReason));
                        // DTO 조립하고 결과를 넣어 줬으니 상대의 대기는 끝났고 응답을 보내줘야지
                        _waitingMap.Remove(opponent);
                    }
                }

				// 이번 착수로 네가 지금 즉시 승리했음
				if (uid == _winnerUID)
				{
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {uid} 차례에서 승리");
                    FinishGame();
					return PlaceStoneResultType.NowWin;
				}

                // 항복, 시간승패 처리도 해야함


                


				// 게임 안 끝났네, 상대 턴으로 넘김
				if (State != GameRoomState.Finished && _winnerUID == 0UL)
                {
                    _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] {_currentTurnUID} 차례에서 {GetOpponent(uid)}으로 턴 교체");
                    _currentTurnUID = GetOpponent(uid);
                }

				return PlaceStoneResultType.Success;
			}
		}

        public TimerSyncData SynchronizeTimer(ulong uid, int turn, TimerSyncData clientTimerData)
        {
            _gameRoomManager.Logger.LogDebug($"[{DateTime.Now}] [Timer Sync] Client Turn {turn} / Server Board Turn {_board.NowTurn}");

            int whileCount = 0;
            while (_board.NowTurn != turn)
            {
                //gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] while Loop... 서버 턴과 클라 턴이 같을 때 까지");
                // TODO : 클라가 보낸 턴이 현재 나의 턴보다 뒤의 턴이면, board 작업이 아직 마무리되지 않았다는 의미
                // 보드 작업이 마무리되고 Turn이 서로 맞춰지면 그때 아래 응답을 보내줘야 함.

                //1초 기다려
                ++whileCount;
            }
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] 서버 턴과 클라 턴 동기화를 위한 while 반복 횟수 : {whileCount}");

            if (_timers.TryGetValue(uid, out UserTimer timer) == false)
            {
                return new TimerSyncData(0f, 0);
            }
            _gameRoomManager.Logger.LogTrace($"[{DateTime.Now}] [Timer Sync] {uid} 서버 타이머 현황 : {timer.SyncData.MainTime}초 / 잔여 초읽기 {timer.SyncData.ByoyomiCount}회");

            if (timer >= clientTimerData)
            {
                // 클라 거 승인
                timer.SynchroTimer(clientTimerData);
            }

            return timer.SyncData;
        }


		public StoneColorType GetColor(ulong uid)
		{
			if (uid == BlackPlayerUID)
				return StoneColorType.Black;
			if (uid == WhitePlayerUID) 
				return StoneColorType.White;
            // 있을 수 없는 일일까? Assert를 걸어야 할까? 하지만 서버는 Assert 걸면 안 된다.
            _gameRoomManager.Logger.LogWarning($"[{DateTime.Now}] [{uid}]유저의 돌 색이 이상하다.");
            return StoneColorType.Empty;
		}

		// 구버전 코드기는 한데 혹시 몰라서 일단 저장, 추후 제거할듯
		public void CheckHeartbeat()
		{
			lock (_lock)
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
			//_endReason = GameEndCode.GomokuWin;
			_userEndCodes[BlackPlayerUID] = GameEndCode.GomokuWin;
			_userEndCodes[WhitePlayerUID] = GameEndCode.GomokuLose;
			_winnerUID = BlackPlayerUID;

        }


		private void OnWhiteWin()
		{
			//_endReason = GameEndCode.GomokuWin;
			_userEndCodes[WhitePlayerUID] = GameEndCode.GomokuWin;
			_userEndCodes[BlackPlayerUID] = GameEndCode.GomokuLose;
			_winnerUID = WhitePlayerUID;
        }

		private void OnDraw()
		{
			_userEndCodes[BlackPlayerUID] = _userEndCodes[WhitePlayerUID] = GameEndCode.Draw;
        }

		private ulong GetOpponent(ulong uid)
			=> uid == BlackPlayerUID ? WhitePlayerUID : BlackPlayerUID;

        public GameEndCode GetEndCode(ulong uid) => _userEndCodes[uid];
	}
}