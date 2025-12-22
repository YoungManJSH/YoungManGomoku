using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
//using System.Security.Cryptography;
using YoungManGomoku_Protocol;
using YoungManGomoku_WebServer.Sessions;

namespace YoungManGomoku_WebServer.SingletoneManager
{
    public class ServerManager : IServerContext
    {
        private readonly ILogger<ServerManager> _logger;

        private UIDGenerator _uidGenerator;
        // DB에 사용되는 테이블이 포함된 PlayerSession Class를 Concurrent Dictionary 구현해 접속중인 유저 관리
        // PlayerData는 클라에서도 사용되기 때문에 보안 상 노출 위험이 있다고 판단
        // [] 인덱서를 사용해 검색하지 말 것. 마음 같아서는 TryGetValue 강제를 위해 private get을 하고 싶다
        internal ConcurrentDictionary<ulong, PlayerSession> PlayerDatas { get; set; }
        public ConcurrentDictionary<string, ulong> UIDByIDToken { get; set; }

        public ServerManager(ILogger<ServerManager> logger)
        {
            _logger = logger;
            _uidGenerator = new UIDGenerator();
            PlayerDatas = new ConcurrentDictionary<ulong, PlayerSession>();
			UIDByIDToken = new ConcurrentDictionary<string, ulong>();
		}

        public uint GenerateUID32() => _uidGenerator.GenerateUID32();
		
		public ulong GenerateUID64() => _uidGenerator.GenerateUID64();

        /// <summary>
        /// 함수 인자로 들어온 UID로 검색한 데이터가 존재하지 않을 시 null 반환
        /// 현재 멀티 스레드 시점에 찾을 때만 없었던 것이므로 찾고 나서 다른 스레드에서 추가해 생겨있을 수도 있음
        /// </summary>
        /// <param name="UID"></param>
        /// <returns></returns>
        internal PlayerSession GetPlayerSession(ulong UID)
        {
            if (PlayerDatas.TryGetValue(UID, out PlayerSession playerSession))
            {
                return playerSession;
            }
            return null;
        }

        public ulong GetPlayerUID(string id_Token)
        {
            if (UIDByIDToken.TryGetValue(id_Token, out ulong UID))
            {
                return UID;
            }
            return 0;
        }

        /// <summary>
        /// Player Data는 Client에서 사용하는 class
        /// 보안 문제로 서버에서는 공개된 클라의 구조체를 쓰지 않는다</summary>
        /// 서버가 가진 데이터를 조립해 클라가 알아보기 쉬운 PlayerData로 바꿔주는 함수
        /// <param name="UID"> 이 UID로 서버의 플레이어 세션에 접근해서 클라용 플레이어 데이터로 조립</param>
        /// <returns> 클라용 플레이어 데이터 </returns>
        public PlayerData ComposePlayerData(ulong UID)
		{
            if (PlayerDatas.TryGetValue(UID, out PlayerSession playerSession))
            {
                // 클라로 UID, AuthToken, AuthLevel을 보낼 필요는 없다.
                return new PlayerData()
                {
                    Nickname = playerSession.Account.Nickname,
                    Rating = playerSession.Account.Status.Rating,

                    Level = playerSession.Account.Status.Level,
                    ExperiencePoint = playerSession.Account.Status.ExperiencePoint,
                    MaxExperiencePoint = playerSession.Account.Status.MaxExperiencePoint,

                    GameMoney = playerSession.Account.Money.GameMoney,
                    CashMoney = playerSession.Account.Money.CashMoney,

                    EquipProfile = playerSession.Account.Equip.EquipProfile,
                    EquipBoardSkin = playerSession.Account.Equip.EquipBoardSkin,
                    EquipStoneSkin = playerSession.Account.Equip.EquipStoneSkin,

                    RegisterDate = playerSession.Account.RegisterDate,
                    LastLoginDate = playerSession.Account.LastLoginDate,
                    LastPlayDate = playerSession.Account.LastPlayDate,

                    WinCount = playerSession.Account.GomokuBattleRecord.WinCount,
                    DrawCount = playerSession.Account.GomokuBattleRecord.LoseCount,
                    LoseCount = playerSession.Account.GomokuBattleRecord.LoseCount,
                    DisconnectCount = playerSession.Account.GomokuBattleRecord.DisconnectCount
                };
            }
            _logger.LogWarning($"[{DateTime.UtcNow}] Failed : ComposePlayerData By UID ({UID}).\nPlayerSession has not UID Data.");
            return null;
		}

		// 매칭 성공 시 상대방 데이터
        public OpponentPlayerData ComposeOpponentPlayerData(ulong opponentPlayerUID)
		{
            if (PlayerDatas.TryGetValue(opponentPlayerUID, out PlayerSession playerData))
            {
                // key 존재 → value 안전하게 사용
                return new OpponentPlayerData()
                {
                    Nickname = playerData.Account.Nickname,
                    EquipProfile = playerData.Account.Equip.EquipProfile,
                    EquipStoneSkin = playerData.Account.Equip.EquipStoneSkin,
                    EquipBoardSkin = playerData.Account.Equip.EquipBoardSkin,
                    Rating = playerData.Account.Status.Rating,
                    Level = playerData.Account.Status.Level,
                    WinCount = playerData.Account.GomokuBattleRecord.WinCount,
                    DrawCount = playerData.Account.GomokuBattleRecord.DrawCount,
                    LoseCount = playerData.Account.GomokuBattleRecord.LoseCount,
                    DisconnectCount = playerData.Account.GomokuBattleRecord.DisconnectCount
                };       
            }
            _logger.LogWarning($"[{DateTime.UtcNow}] Failed : ComposeOpponentPlayerData By UID ({opponentPlayerUID}).\nPlayerSession has not UID Data.");
            return null;
        }
    }
}