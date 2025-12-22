using System.Collections.Concurrent;
using YoungManGomoku_WebServer.Sessions;

namespace YoungManGomoku_WebServer.SingletoneManager
{
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
