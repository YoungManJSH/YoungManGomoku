using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager.Interface;

namespace YoungManGomoku_WebServer.SingletoneManager
{
	public class GameRoomManager
	{
        private readonly ILogger<GameRoomManager> _logger;
		public ILogger<GameRoomManager> Logger => _logger;

        // Key : Room UID
        private readonly ConcurrentDictionary<ulong, GameRoom> _rooms;

		// 이 플레이어가 어느 방에서 게임 중인지
		// Key : Player UID / Value : Room UID
		private readonly ConcurrentDictionary<ulong, ulong> _roomByPlayer;

		public IServerContext ServerContext { get; }

        public GameRoomManager(ILogger<GameRoomManager> logger, IServerContext serverContext)
		{
			_logger = logger;
			_rooms = new ConcurrentDictionary<ulong, GameRoom>();
			_roomByPlayer = new ConcurrentDictionary<ulong, ulong>();
            ServerContext = serverContext;
		}

        public GameRoom CreateRoom(ulong blackPlayer, ulong whitePlayer)
        {
            GameRoom room = new GameRoom(ServerContext.GenerateUID64(), blackPlayer, whitePlayer, this);
            _rooms[room.RoomID] = room;

            _roomByPlayer[blackPlayer] = room.RoomID;
            _roomByPlayer[whitePlayer] = room.RoomID;

            return room;
        }

        public GameRoom CreateRoom(ulong roomID, ulong blackPlayer, ulong whitePlayer)
		{
            GameRoom room = new GameRoom(roomID, blackPlayer, whitePlayer, this);
			_rooms[roomID] = room;

			_roomByPlayer[blackPlayer] = roomID;
			_roomByPlayer[whitePlayer] = roomID;

			return room;
		}

		public bool TryGetRoomByPlayer(ulong playerUID, out GameRoom? room)
		{
			// 못 찾았으면 null
			room = null;

			// 플레이어가 소속된 room ID를 찾고, room ID로 room을 찾아옴 
			if (_roomByPlayer.TryGetValue(playerUID, out ulong roomID))
				return _rooms.TryGetValue(roomID, out room);

			return false;
		}

        internal void CloseRoom(ulong roomID)
        {
			// Remove 하고 나서 삭제한 값을 따로 쓸 일은 없으니 그냥 _ 때림
			if (_rooms.TryGetValue(roomID, out GameRoom? room))
			{
				_roomByPlayer.TryRemove(room.BlackPlayerUID, out _);
				_roomByPlayer.TryRemove(room.WhitePlayerUID, out _);
			}
            _rooms.TryRemove(roomID, out _);
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
