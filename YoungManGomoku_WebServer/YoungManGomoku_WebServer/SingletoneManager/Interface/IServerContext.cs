using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_WebServer.Sessions;

namespace YoungManGomoku_WebServer.SingletoneManager.Interface
{
	public interface IServerContext : IUIDProvider
	{
		public TimerSettingData DefaultTimerSetting { get; }

        public bool TryDBUpdateGameResult(ulong UID, ApplicationDBContext context);



        public ulong GetPlayerUID(string id_Token);

        public string UserInfo(string id_Token);

        public string UserInfo(ulong UID);       

		public PlayerData? ComposePlayerData(ulong UID);

        public PlayerStatus? GetPlayerStatus(ulong UID);

        public PlayerMoney? GetPlayerMoney(ulong UID);

        public PlayerBattleRecord? GetPlayerBattleRecord(ulong UID);

        public bool CloseSession(ulong UID);
    }
}
