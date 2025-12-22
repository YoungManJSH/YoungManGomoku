using YoungManGomoku_Protocol;

namespace YoungManGomoku_WebServer.SingletoneManager
{
	public interface IServerContext : IUIDProvider
	{
		public ulong GetPlayerUID(string id_Token);

		public PlayerData ComposePlayerData(ulong UID);
	}
}
