using YoungManGomoku_WebServer.Data.DatabaseContext;
using System.Collections.Concurrent;

namespace YoungManGomoku_WebServer
{
	public static class ServerManager
	{
		private static UIDGenerator uidGenerator;

		internal static ConcurrentDictionary<ulong, PlayerProfile> PlayerProfiles { get; set; }

        internal static ConcurrentDictionary<ulong, PlayerBattleRecord> PlayerRecords { get; set; }

        static ServerManager()
		{
			uidGenerator = new UIDGenerator();
            PlayerProfiles = new ConcurrentDictionary<ulong, PlayerProfile>();
            PlayerRecords = new ConcurrentDictionary<ulong, PlayerBattleRecord>();
        }

		public static uint GenerateUID32() => uidGenerator.GenerateUID32();
		
		public static ulong GenerateUID64() => uidGenerator.GenerateUID64();



	}
}
