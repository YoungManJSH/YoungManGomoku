using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
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

        internal PlayerSession GetPlayerSession(ulong UID)
			=> PlayerDatas[UID];
        public ulong GetPlayerUID(string id_Token)
           => UIDByIDToken[id_Token];

        /// <summary>
        /// Player Data는 Client에서 사용하는 class
        /// 보안 문제로 서버에서는 공개된 클라의 구조체를 쓰지 않는다</summary>
        /// 서버가 가진 데이터를 조립해 클라가 알아보기 쉬운 PlayerData로 바꿔주는 함수
        /// <param name="UID"> 이 UID로 서버의 플레이어 세션에 접근해서 클라용 플레이어 데이터로 조립</param>
        /// <returns> 클라용 플레이어 데이터 </returns>
        public PlayerData ComposePlayerData(ulong UID)
		{
            PlayerData resultData = new PlayerData()
            {
                Nickname = PlayerDatas[UID].Account.Nickname,
                Rating = PlayerDatas[UID].Account.Status.Rating,

                Level = PlayerDatas[UID].Account.Status.Level,
                ExperiencePoint = PlayerDatas[UID].Account.Status.ExperiencePoint,
                MaxExperiencePoint = PlayerDatas[UID].Account.Status.MaxExperiencePoint,

                GameMoney = PlayerDatas[UID].Account.Money.GameMoney,
                CashMoney = PlayerDatas[UID].Account.Money.CashMoney,

                EquipProfile = PlayerDatas[UID].Account.Equip.EquipProfile,
                EquipBoardSkin = PlayerDatas[UID].Account.Equip.EquipBoardSkin,
                EquipStoneSkin = PlayerDatas[UID].Account.Equip.EquipStoneSkin,

                RegisterDate = PlayerDatas[UID].Account.RegisterDate,
                LastLoginDate = PlayerDatas[UID].Account.LastLoginDate,
                LastPlayDate = PlayerDatas[UID].Account.LastPlayDate,

                WinCount = PlayerDatas[UID].Account.GomokuBattleRecord.WinCount,
                DrawCount = PlayerDatas[UID].Account.GomokuBattleRecord.LoseCount,
                LoseCount = PlayerDatas[UID].Account.GomokuBattleRecord.LoseCount,
                DisconnectCount = PlayerDatas[UID].Account.GomokuBattleRecord.DisconnectCount
            };

            // 클라로 UID, AuthToken, AuthLevel을 보낼 필요는 없다.
            return resultData;
		}

		// 매칭 성공 시 상대방 데이터
        public OpponentPlayerData ComposeOpponentPlayerData(ulong otherPlayerUID)
		{
            OpponentPlayerData resultData = new OpponentPlayerData()
            {
                Nickname = PlayerDatas[otherPlayerUID].Account.Nickname,
                EquipProfile = PlayerDatas[otherPlayerUID].Account.Equip.EquipProfile,
                EquipStoneSkin = PlayerDatas[otherPlayerUID].Account.Equip.EquipStoneSkin,
                EquipBoardSkin = PlayerDatas[otherPlayerUID].Account.Equip.EquipBoardSkin,
                Rating = PlayerDatas[otherPlayerUID].Account.Status.Rating,
                Level = PlayerDatas[otherPlayerUID].Account.Status.Level,
                WinCount = PlayerDatas[otherPlayerUID].Account.GomokuBattleRecord.WinCount,
                DrawCount = PlayerDatas[otherPlayerUID].Account.GomokuBattleRecord.DrawCount,
                LoseCount = PlayerDatas[otherPlayerUID].Account.GomokuBattleRecord.LoseCount,
                DisconnectCount = PlayerDatas[otherPlayerUID].Account.GomokuBattleRecord.DisconnectCount
            };
            return resultData;
        }
    }
}