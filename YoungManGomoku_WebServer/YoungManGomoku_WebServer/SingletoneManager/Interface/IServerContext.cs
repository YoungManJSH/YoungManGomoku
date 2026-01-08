using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;

namespace YoungManGomoku_WebServer.SingletoneManager.Interface
{
	public interface IServerContext : IUIDProvider
	{
		public SC_TimerSettingDTO DefaultTimerSetting { get; }

		public ulong GetPlayerUID(string id_Token);

		public PlayerData? ComposePlayerData(ulong UID);
	}
}
