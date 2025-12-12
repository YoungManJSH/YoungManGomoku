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
        // DB에 사용되는 테이블 형태로 Concurrent Dictionary 구현해 접속중인 유저 관리
        // PlayerData는 클라에서도 사용되기 때문에 보안 상 노출 위험이 있다고 판단
		internal static ConcurrentDictionary<ulong, PlayerSession> PlayerDatas { get; set; }

        static ServerManager()
		{
			uidGenerator = new UIDGenerator();
            PlayerDatas = new ConcurrentDictionary<ulong, PlayerSession>();
        }

		public static uint GenerateUID32() => uidGenerator.GenerateUID32();
		
		public static ulong GenerateUID64() => uidGenerator.GenerateUID64();

        public static PlayerData GetPlayerData(ulong UID)
		{
			PlayerData resultData = new PlayerData();

            PlayerAccount playerProfile;

            // 클라로 UID, AuthToken, AuthLevel을 보낼 필요는 없다.

            resultData.Nickname = PlayerDatas[UID].Account.Nickname;
            resultData.Rating = PlayerDatas[UID].Status.Rating;

			resultData.Level = PlayerDatas[UID].Status.Level;
            resultData.ExperiencePoint = PlayerDatas[UID].Status.ExperiencePoint;
            resultData.MaxExperiencePoint = PlayerDatas[UID].Status.MaxExperiencePoint;

            resultData.GameMoney = PlayerDatas[UID].Money.GameMoney;
            resultData.CashMoney = PlayerDatas[UID].Money.CashMoney;

            resultData.EquipProfile = PlayerDatas[UID].Equip.EquipProfile;
			resultData.EquipBoardSkin = PlayerDatas[UID].Equip.EquipBoardSkin;
			resultData.EquipStoneSkin = PlayerDatas[UID].Equip.EquipStoneSkin;
            
            resultData.RegisterDate = PlayerDatas[UID].Account.RegisterDate;
            resultData.LastLoginDate = PlayerDatas[UID].Account.LastLoginDate;
            resultData.LastPlayDate = PlayerDatas[UID].Account.LastPlayDate;

            resultData.WinCount = PlayerDatas[UID].GomokuRecord.WinCount;
            resultData.DrawCount = PlayerDatas[UID].GomokuRecord.LoseCount;
            resultData.LoseCount = PlayerDatas[UID].GomokuRecord.LoseCount;
            resultData.DisconnectCount = PlayerDatas[UID].GomokuRecord.DisconnectCount;

            return resultData;
		}
    }

    class PlayerSession
    {
        public PlayerAccount Account { get; set; }
        public PlayerStatus Status { get; set; }
        public PlayerMoney Money { get; set; }
        public PlayerInventoryItem Inventory { get; set; }
        public PlayerEquip Equip { get; set; }
        
        public PlayerBattleRecord GomokuRecord { get; set; }
    }
}
