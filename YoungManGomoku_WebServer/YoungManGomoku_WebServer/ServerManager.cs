using System;
using System.Collections.Concurrent;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.Source.TypeEnum;
using YoungManGomoku_WebServer.Data.DatabaseContext;

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

        public static PlayerData GetPlayerData(ulong UID)
		{
			PlayerData resultData = new PlayerData();

            PlayerProfile playerProfile = PlayerProfiles[UID];
			PlayerBattleRecord playeRecord = PlayerRecords[UID];

            // 클라로 UID, AuthToken, AuthLevel을 보낼 필요는 없다.

            resultData.Nickname = playerProfile.Nickname;
            resultData.Rating = playerProfile.Rating;

			resultData.Level = playerProfile.Level;
            resultData.ExperiencePoint = playerProfile.ExperiencePoint;
            resultData.MaxExperiencePoint = playerProfile.MaxExperiencePoint;

            resultData.GameMoney = playerProfile.GameMoney;
            resultData.CashMoney = playerProfile.CashMoney;

            resultData.EquipProfile = playerProfile.EquipProfile;
			resultData.EquipBoardSkin = playerProfile.EquipBoardSkin;
			resultData.EquipStoneSkin = playerProfile.EquipStoneSkin;
            
            resultData.RegisterDate = playerProfile.RegisterDate;
            resultData.LastLoginDate = playerProfile.LastLoginDate;
            resultData.LastPlayDate = playerProfile.LastPlayDate;

            playeRecord.WinCount = 0;
            playeRecord.DrawCount = 0;
            playeRecord.LoseCount = 0;
            playeRecord.DisconnectCount = 0;

            return resultData;
		}
    }
}
