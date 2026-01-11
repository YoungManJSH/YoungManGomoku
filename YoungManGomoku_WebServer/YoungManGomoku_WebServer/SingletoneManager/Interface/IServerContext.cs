using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;

namespace YoungManGomoku_WebServer.SingletoneManager.Interface
{
	public interface IServerContext : IUIDProvider
	{
		public TimerSettingData DefaultTimerSetting { get; }

        public ulong GetPlayerUID(string id_Token);

        public string UserInfo(string id_Token);

        public string UserInfo(ulong UID);       

		public PlayerData? ComposePlayerData(ulong UID);

		public bool CloseSession(ulong UID);
	}
}
