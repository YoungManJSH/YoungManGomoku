using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace YoungManGomoku_WebServer.SingletoneManager
{
	public enum GameEndReason
	{
		None, // 게임이 아직 안 끝났음
		Win,
		Draw,
		Surrender,
		Disconnect
	}

	public class GameTimer
	{
		// 일단 10분 때려봄, 당연히 변경 가능성 아주 높음, 임시로 때려본거임
		public float BlackMainTime { get; private set; } = 600;
		public float WhiteMainTime { get; private set; } = 600;

		public void OnMove(ulong uid)
		{
			// 나중에 클라 API 공유받아서 확장 예정
		}

		public GameTimerSnapshot Snapshot()
		{
			return new GameTimerSnapshot
			{
				BlackMainTime = BlackMainTime,
				WhiteMainTime = WhiteMainTime
			};
		}
	}
	
	// 두 플레이어의 기본 제공 시간 (초읽기 아님)
	public class GameTimerSnapshot
	{
		public float BlackMainTime { get; set; }
		public float WhiteMainTime { get; set; }
	}


	public enum PlaceStoneResultType
	{
		Success,		// 착수 성공
		NotYourTurn,	// 니 턴 아님
		Occupied,		// 그 위치 이미 뒀어
		Invalid			// ㅈ버그에요
	}

	public class PlaceStoneResult
	{
		public PlaceStoneResultType Result { get; set; }
		public bool GameFinished { get; set; }
		public GameEndReason EndReason { get; set; }
		public ulong WinnerUID { get; set; }
		public GameTimerSnapshot Timer { get; set; }
	}

	public enum GameRoomState
	{
		Waiting, // 대기 중
		Playing, // 게임 중
		Finished // 게임 끝났음, 리벤지 각?
	}


	public class GameRoom
	{
		public ulong RoomID { get; }
		public ulong BlackPlayerUID { get; }
		public ulong WhitePlayerUID { get; }

		// 방 밖에서 방이 게임 중인지 아닌지를 결정하면 안 된다
		public GameRoomState State { get; private set; }

		// 날 건드릴 수 있는 놈이 플레이어 둘이잖냐, 락걸어야지
		private readonly object _lock = new object();


		private readonly GameRoomManager _gameRoomManager;

		// 오목판, 일단 임시로 만듦
		private readonly int[,] _board = new int[15, 15];

		// 턴, 나중에 쓸 건데 임시로 만듦
		private ulong _currentTurnUID;

		// 하트비트, n 초 이상 미 요청 시 접속 끊김으로 간주
		private readonly Dictionary<ulong, DateTime> _lastRequestTime;

		// 타이머 정보, 아직 미구현
		private GameTimer _timer;

		private GameEndReason _endReason;
		private ulong _winner;

		public bool IsFinished => _endReason != GameEndReason.None;

		public GameRoom(ulong roomID,ulong blackPlayerUID, ulong whitePlayerUID, GameRoomManager roomManager)
		{
			RoomID = roomID;
			BlackPlayerUID = blackPlayerUID;
			WhitePlayerUID = whitePlayerUID;
			_gameRoomManager = roomManager;

			// 흑돌 선수
			_currentTurnUID = BlackPlayerUID;
			State = GameRoomState.Playing;
			_endReason = GameEndReason.None; // 게임 끝난 이유 None은 지금 게임중이라는 뜻

			_lastRequestTime = new Dictionary<ulong, DateTime>
			{
				[blackPlayerUID] = DateTime.UtcNow,
				[whitePlayerUID] = DateTime.UtcNow
			};

			//_timer = new GameTimer();
		}
		public PlaceStoneResult PlaceStone(ulong uid, int x, int y)
		{
			lock (_lock)
			{
				// 겜 끝났어
				if (IsFinished)
					return InvalidResult();

				// 니 턴 아니야
				if (uid != _currentTurnUID)
					return new PlaceStoneResult { Result = PlaceStoneResultType.NotYourTurn };

				// 빈 곳이 아닌데 두려고 시도함
				if (_board[x, y] != 0)
					return new PlaceStoneResult { Result = PlaceStoneResultType.Occupied };

				_board[x, y] = uid == BlackPlayerUID ? 1 : 2;
				_timer.OnMove(uid);

				// 누가 이겼음?
				if (CheckWin(x, y))
				{
					_endReason = GameEndReason.Win;
					_winner = uid;
				}
				// 판 꽉참?
				else if (IsBoardFull())
				{
					_endReason = GameEndReason.Draw;
				}
				else
				{
					// 게임 안 끝났네, 상대 턴으로 넘김
					_currentTurnUID = GetOpponent(uid);
				}

				return new PlaceStoneResult
				{
					Result = PlaceStoneResultType.Success,
					GameFinished = IsFinished,
					EndReason = _endReason,
					WinnerUID = _winner,
					Timer = _timer.Snapshot()
				};
			}

		}
		// 구버전 코드기는 한데 혹시 몰라서 일단 저장, 추후 제거할듯
		/*
		public PlaceStoneResult PlaceStone(ulong uid, int x, int y)
		{
			lock (_lock)
			{
				if (State != GameRoomState.Playing)
					return PlaceStoneResult.Invalid;

				if (uid != _currentTurnUID)
					return PlaceStoneResult.NotYourTurn;

				if (_board[x, y] != 0)
					return PlaceStoneResult.Occupied;

				_board[x, y] = uid == BlackPlayerUID ? 1 : 2;
				_timer.OnMove(uid);

				_lastRequestTime[uid] = DateTime.UtcNow;

				if (CheckWin(x, y))
				{
					FinishGame(uid, GameEndReason.Win);
				}
				else if (IsBoardFull())
				{
					FinishGame(0, GameEndReason.Draw);
				}
				else
				{
					_currentTurnUID = GetOpponent(uid);
				}

				return PlaceStoneResult.Success;
			}
		}*/
		/*
		public void CheckHeartbeat()
		{
			lock (_lock)
			{
				foreach (var pair in _lastRequestTime)
				{
					if ((DateTime.UtcNow - pair.Value).TotalSeconds > 10)
					{
						FinishGame(GetOpponent(pair.Key), GameEndReason.Disconnect);
						return;
					}
				}
			}
		}

		private void FinishGame(ulong winnerUid, GameEndReason reason)
		{
			State = GameRoomState.Finished;

			// 결과 전송
			SendResultToPlayers(winnerUid, reason);

			if (reason == GameEndReason.Win || reason == GameEndReason.Draw)
			{
				// 재도전 가능 상태 유지
			}
			else
			{
				_manager.CloseRoom(this);
			}
		}
		*/

		private bool IsBoardFull()
		{
			foreach (var cell in _board)
				if (cell == 0)
					return false;
			return true;
		}

		private ulong GetOpponent(ulong uid)
			=> uid == BlackPlayerUID ? WhitePlayerUID : BlackPlayerUID;

		private bool CheckWin(int x, int y)
		{
			// 지금은 false로 두고 나중에 구현
			return false;
		}

		// 니 턴 아닌데 착수 요청이 들어옴, 제정신이 아님
		private PlaceStoneResult InvalidResult()
			=> new PlaceStoneResult { Result = PlaceStoneResultType.Invalid };

	}
	public class GameRoomManager
	{
		// Key : Room UID
		private readonly ConcurrentDictionary<ulong, GameRoom> _rooms;

		// Key : Player UID / Value : Room UID
		private readonly ConcurrentDictionary<ulong, ulong> _roomByPlayer;

		private readonly IServerContext _serverContext;

		public GameRoomManager(IServerContext serverContext)
		{
			_rooms = new ConcurrentDictionary<ulong, GameRoom>();
			_roomByPlayer = new ConcurrentDictionary<ulong, ulong>();
			_serverContext = serverContext;
		}

		public GameRoom CreateRoom(ulong roomID, ulong playerA, ulong playerB)
		{
			var room = new GameRoom(roomID, playerA, playerB, this);
			_rooms[roomID] = room;

			_roomByPlayer[playerA] = roomID;
			_roomByPlayer[playerB] = roomID;

			return room;
		}

		public bool TryGetRoomByPlayer(ulong playerUid, out GameRoom room)
		{
			// 못 찾았으면 null
			room = null;

			// 플레이어가 소속된 room ID를 찾고, room ID로 room을 찾아옴 
			if (_roomByPlayer.TryGetValue(playerUid, out ulong roomID))
				return _rooms.TryGetValue(roomID, out room);

			return false;
		}

		internal void CloseRoom(GameRoom room)
		{
			// Remove 하고 나서 삭제한 값을 따로 쓸 일은 없으니 그냥 _ 때림
			_rooms.TryRemove(room.RoomID, out _);
			_roomByPlayer.TryRemove(room.BlackPlayerUID, out _);
			_roomByPlayer.TryRemove(room.WhitePlayerUID, out _);
		}
	}
}
