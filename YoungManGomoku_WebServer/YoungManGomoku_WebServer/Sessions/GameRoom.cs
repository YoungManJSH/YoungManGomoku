using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
		Invalid         // ㅈ버그에요
	}
	
	
	public class PlaceStoneResult
    {
        public ulong WinnerUID { get; set; }
        public PlaceStoneResultType Result { get; set; }
		public GameEndCode EndReason { get; set; }
        public bool GameFinished { get; set; }
        // public GameTimerSnapshot Timer { get; set; }
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
        public TaskCompletionSource<SC_OpponentMoveDTO> TaskCompSrc { get; set; }
        public CancellationTokenRegistration CancellationTokenRegist { get; set; }
    }

    public class GameRoom
	{
		public ulong RoomID { get; }
		public ulong BlackPlayerUID { get; }
		public ulong WhitePlayerUID { get; }

		// 방 밖에서 방이 게임 중인지 아닌지를 결정하면 안 된다
		public GameRoomState State { get; private set; }

		// 날 건드릴 수 있는 놈이 플레이어 둘이잖냐, 락 걸어야지
		private readonly object _lock = new object();

        // 나 자신의 방을 닫을 때 필요함
		private readonly GameRoomManager _gameRoomManager;

		// 오목판
		private readonly Board _board;

		// 하트비트, n 초 이상 미 요청 시 접속 끊김으로 간주
		private readonly Dictionary<ulong, DateTime> _lastRequestTime;

        private readonly Dictionary<ulong, InGameWaitingPlayer> _waitingMap;

        // 유저별 타이머 정보
        private readonly Dictionary<ulong, UserTimer> _timers;

        // 현재 이 게임 룸의 게임 종료 사유
        private GameEndCode _endReason;

        // 턴, 나중에 쓸 건데 임시로 만듦
        private ulong _currentTurnUID;

        private ulong _winnerUID;

        public bool IsFinished => _endReason != GameEndCode.None;

		public GameRoom(ulong roomID, ulong blackPlayerUID, ulong whitePlayerUID, GameRoomManager roomManager)
		{
            RoomID = roomID;
            BlackPlayerUID = blackPlayerUID;
            WhitePlayerUID = whitePlayerUID;
            State = GameRoomState.Waiting;

            _gameRoomManager = roomManager;

            _board = new Board();
			// 흑돌 첫 수는 무조건 중앙 고정
            _board.TryMoveStone(7, 7);

            _board.OnBlackGomoku += OnBlackWin;
            _board.OnWhiteGomoku += OnWhiteWin;
            _board.OverMaxTurn += OnDraw;
            _board.OnBlackUnmovable += OnWhiteWin;

            _board.OnBlackGomoku += FinishGame;
            _board.OnWhiteGomoku += FinishGame;
            _board.OverMaxTurn += FinishGame;
            _board.OnBlackUnmovable += FinishGame;

            _timers = new Dictionary<ulong, UserTimer>()
            {
                [blackPlayerUID] = new UserTimer(initMainTime: 180f, initByoyomiCount: 3, byoyomiSeconds: 30f),
                [whitePlayerUID] = new UserTimer(initMainTime: 180f, initByoyomiCount: 3, byoyomiSeconds: 30f)
            };                  
            

            _waitingMap = new Dictionary<ulong, InGameWaitingPlayer>();


            // 흑돌 선수
            _currentTurnUID = BlackPlayerUID;
			
			_endReason = GameEndCode.None; // 이 방에서 게임이 끝난 이유. None은 지금 게임중이라는 뜻

			_lastRequestTime = new Dictionary<ulong, DateTime>
			{
				[blackPlayerUID] = DateTime.UtcNow,
				[whitePlayerUID] = DateTime.UtcNow
			};
        }

        public void RegisterWaiter(InGameWaitingPlayer waitingPlayer)
        {
            lock (_lock)
            {
                _waitingMap[waitingPlayer.UID] = waitingPlayer;
            }
        }

        public void CancelWait(ulong UID)
        {
            lock (_lock)
            {
                if (_waitingMap.TryGetValue(UID, out InGameWaitingPlayer waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc.TrySetCanceled();
                    _waitingMap.Remove(UID);
                }
            }
        }
        private void DispatchTo(ulong UID, SC_OpponentMoveDTO OpponentMoveDTO)
        {
            if (_waitingMap.TryGetValue(UID, out InGameWaitingPlayer waitingPlayer))
            {
                waitingPlayer.TaskCompSrc.TrySetResult(OpponentMoveDTO);
                _waitingMap.Remove(UID);
            }
        }


        public Task<SC_OpponentMoveDTO> WaitNextEventAsync(ulong UID, CancellationToken ct)
        {
            lock (_lock)
            {
                TaskCompletionSource<SC_OpponentMoveDTO> tcs
                    = new TaskCompletionSource<SC_OpponentMoveDTO>(
                    TaskCreationOptions.RunContinuationsAsynchronously);


                _waitingMap[UID] = new InGameWaitingPlayer
                {
                    UID = UID,
                    TaskCompSrc = tcs,
                    CancellationTokenRegist = ct.Register(() => CancelWait(UID))
                };

                return tcs.Task;
            }
        }

        // 보드와 타이머 쪽에서 재대결 리벤지 정의가 안 됨
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

        // 게임 종료 작업, Dispose는 필수
        private void DispatchGameEndToAll()
        {
            foreach (var waitingPlayer in _waitingMap.Values)
            {
                SC_OpponentMoveDTO endEvent = new SC_OpponentMoveDTO(
                            (byte)255, (byte)255, _timers[(GetColor(waitingPlayer.UID) == StoneColorType.Black) ? BlackPlayerUID : WhitePlayerUID], _endReason);
                waitingPlayer.TaskCompSrc.TrySetResult(endEvent);
                waitingPlayer.CancellationTokenRegist.Dispose();
            }

            _waitingMap.Clear();
        }

        public PlaceStoneResult PlaceStone(ulong uid, int x, int y)
		{
			lock (_lock)
			{
				// 클라 변조 유효성 체크, 클라에서 제대로 요청이 들어왔다면 여기 들어올 일이 없음
				{
					// 게임 끝났어
					if (IsFinished)
						return InvalidResult();

					// 니 턴 아니야
					if (uid != _currentTurnUID)
						return new PlaceStoneResult { Result = PlaceStoneResultType.NotYourTurn };
				}

				// 타이머 체크
				// _timer. 어쩌고

				// _board의 NowTurn 값이 홀수면 백, 짝수면 흑 차례라는 뜻
				//bool isBlack = (_board.NowTurn & 1) == 0;

				// TryMoveStone이 true일 시 착수 성공. 내부적으로 승패 처리까지 동작하며 _board에 등록한 event들이 실행됨
                if (_board.TryMoveStone(x, y) == false)
				{
                    // 빈 곳이 아닌데 두려고 시도했거나, 흑돌이 금수 위치에 두려고 시도함. 클라 변조 체크
                    return new PlaceStoneResult { Result = PlaceStoneResultType.Occupied };
                }

				// 착수 성공 및 게임 결과 처리
                _lastRequestTime[uid] = DateTime.UtcNow;
                ulong opponent = GetOpponent(uid);


                // 상대가 대기 중이면 이벤트 전달
                if (_waitingMap.TryGetValue(opponent, out InGameWaitingPlayer waitingPlayer))
                {
                    waitingPlayer.TaskCompSrc.TrySetResult(
                        new SC_OpponentMoveDTO(
                            (byte)x, (byte)y, _timers[(GetColor(uid) == StoneColorType.Black) ? BlackPlayerUID : WhitePlayerUID]));
                    
                    _waitingMap.Remove(opponent);
                }

                // 게임 시작 처리
                if (State == GameRoomState.Waiting)
                    State = GameRoomState.Playing;

                // 게임 안 끝났네, 상대 턴으로 넘김
                if (_winnerUID == 0UL)
                    _currentTurnUID = GetOpponent(uid);


                return new PlaceStoneResult
				{
					Result = PlaceStoneResultType.Success,
					GameFinished = IsFinished,
					EndReason = _endReason,
					WinnerUID = _winnerUID, // 승자가 없으면 0
					//Timer = _timer.Snapshot()
				};
			}
		}

		public StoneColorType GetColor(ulong uid)
		{
			if (uid == BlackPlayerUID)
				return StoneColorType.Black;
			if (uid == WhitePlayerUID) 
				return StoneColorType.White;
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


            DispatchGameEndToAll();

            // 방 정리 정책은 여기서
            // _gameRoomManager.CloseRoom(this);

            // 재도전 가능?
            // 재도전 안 하면 방 닫아야 함
            // _gameRoomManager.CloseRoom(this);
        }

        private void OnBlackWin()
		{
			_endReason = GameEndCode.GomokuWin;
			_winnerUID = BlackPlayerUID;
        }


		private void OnWhiteWin()
		{
			_endReason = GameEndCode.GomokuWin;
			_winnerUID = WhitePlayerUID;
        }

		private void OnDraw()
		{
            _endReason = GameEndCode.Draw;
        }

		private ulong GetOpponent(ulong uid)
			=> uid == BlackPlayerUID ? WhitePlayerUID : BlackPlayerUID;

		// 니 턴 아닌데 착수 요청이 들어옴, 제정신이 아님
		private PlaceStoneResult InvalidResult()
			=> new PlaceStoneResult { Result = PlaceStoneResultType.Invalid };
	}
}