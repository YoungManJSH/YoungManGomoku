using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.Source.TypeEnum;
using YoungManGomoku_WebServer.Data.DatabaseContext;

namespace YoungManGomoku_WebServer.SingletoneManager
{
	/*
	서버에서 관리하는 접속된 Player Object
	PlayerAccount에서 Composition, 관계형 DB를 통해 외래 Key로 연결해 DB 데이터 관리
	나머지는 비 DB 데이터, 메모리에만 올라감
	(DB에 저장할 필요가 없고, 서버가 터져서 메모리가 날아가도 괜찮은 데이터)
	(DB Transaction 최소화를 위한 최적화)
	 */
	internal class PlayerSession
	{
		// 운영 시 서버 로그나 디버깅 전용
		public ulong SessionID { get; set; }  // 8바이트 정수

		// DB Data, Account 이외의 Player 관련 Table들은 Account의 멤버로 갖고 있음
		public PlayerAccount Account { get; set; }

		// 로그인 관련
		// 매번 토큰을 검사하는 DB 작업은 굉장히 구리기 때문에 Session에 캐싱
		public string AuthToken { get; set; }
		public DateTime LoginTime { get; set; }

		// 마지막으로 요청해온 시간
		// 이게 너무 빨리 자주 오면 DDOS 의심으로 차단할 것
		// Rate Limit 및 악성 요청 체크
		public DateTime LastRequestTime { get; set; }

		// 이게 너무 오래 안 와서 HeartBeat가 죽었는지 판단할 것
		// 연결 끊김 감지용
		public DateTime LastHeartbeatTime { get; set; }

		public bool IsConnected { get; set; }

		// 게임 상태
		public bool IsMatching { get; set; }
		public bool IsInGame { get; set; }
		//public GameRoom CurrentRoom { get; set; }

		// 요청 제한 / 보안
		public int RecentRequestCount { get; set; }
		public DateTime LastActionTime { get; set; }
		public string IPAddress { get; set; }

		// 클라이언트 정보
		public string ClientVersion { get; set; }
		public string Platform { get; set; }
		public string DeviceModel { get; set; }

		// 운영 및 임시 데이터
		public string ConnectionId { get; set; }

		// 요청 간 임시 데이터 저장 (예: 매치 리퀘스트에 필요한 변수)
		// public Dictionary<string, object> TempData { get; } = new Dictionary<string, object>();

		public PlayerSession(ulong sessionUID, PlayerAccount account, bool isConnect = true)
		{
			SessionID = sessionUID;
            Account = account;
			AuthToken = Account.AuthToken;	// caching
            IsConnected = isConnect;
			IsMatching = false;
			IsInGame = false;
            LoginTime = DateTime.UtcNow;
			LastRequestTime = DateTime.UtcNow;
			LastHeartbeatTime = DateTime.UtcNow;
            LastActionTime = DateTime.UtcNow;
        }
	}

    public interface IUIDProvider
    {
		uint GenerateUID32();
        ulong GenerateUID64();
    }

	public interface IServerContext : IUIDProvider
    {
        public ulong GetPlayerUID(string id_Token);

        public PlayerData ComposePlayerData(ulong UID);
    }

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