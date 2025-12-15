using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.Source.TypeEnum;
using YoungManGomoku_WebServer.Data.DatabaseContext;

namespace YoungManGomoku_WebServer
{
	class PlayerSession
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
	}

	public class ServerManager
	{
		private UIDGenerator uidGenerator;
        // DB에 사용되는 테이블이 포함된 PlayerSession Class를 Concurrent Dictionary 구현해 접속중인 유저 관리
        // PlayerData는 클라에서도 사용되기 때문에 보안 상 노출 위험이 있다고 판단
		internal ConcurrentDictionary<ulong, PlayerSession> PlayerDatas { get; set; }

        public ServerManager()
		{
			uidGenerator = new UIDGenerator();
            PlayerDatas = new ConcurrentDictionary<ulong, PlayerSession>();
        }

		public uint GenerateUID32() => uidGenerator.GenerateUID32();
		
		public ulong GenerateUID64() => uidGenerator.GenerateUID64();

		/// <summary>
		/// Player Data는 Client에서 사용하는 class
		/// 보안 문제로 서버에서는 공개된 클라의 구조체를 쓰지 않는다</summary>
		/// 서버가 가진 데이터를 조립해 클라가 알아보기 쉬운 PlayerData로 바꿔주는 함수
		/// <param name="UID"> 이 UID로 서버의 플레이어 세션에 접근해서 클라용 플레이어 데이터로 조립</param>
		/// <returns> 클라용 플레이어 데이터 </returns>
		public PlayerData GetPlayerData(ulong UID)
		{
			PlayerData resultData = new PlayerData();
            // 클라로 UID, AuthToken, AuthLevel을 보낼 필요는 없다.
            resultData.Nickname = PlayerDatas[UID].Account.Nickname;
            resultData.Rating = PlayerDatas[UID].Account.Status.Rating;

			resultData.Level = PlayerDatas[UID].Account.Status.Level;
            resultData.ExperiencePoint = PlayerDatas[UID].Account.Status.ExperiencePoint;
            resultData.MaxExperiencePoint = PlayerDatas[UID].Account.Status.MaxExperiencePoint;

            resultData.GameMoney = PlayerDatas[UID].Account.Money.GameMoney;
            resultData.CashMoney = PlayerDatas[UID].Account.Money.CashMoney;

            resultData.EquipProfile = PlayerDatas[UID].Account.Equip.EquipProfile;
			resultData.EquipBoardSkin = PlayerDatas[UID].Account.Equip.EquipBoardSkin;
			resultData.EquipStoneSkin = PlayerDatas[UID].Account.Equip.EquipStoneSkin;
            
            resultData.RegisterDate = PlayerDatas[UID].Account.RegisterDate;
            resultData.LastLoginDate = PlayerDatas[UID].Account.LastLoginDate;
            resultData.LastPlayDate = PlayerDatas[UID].Account.LastPlayDate;

            resultData.WinCount = PlayerDatas[UID].Account.GomokuBattleRecord.WinCount;
            resultData.DrawCount = PlayerDatas[UID].Account.GomokuBattleRecord.LoseCount;
            resultData.LoseCount = PlayerDatas[UID].Account.GomokuBattleRecord.LoseCount;
            resultData.DisconnectCount = PlayerDatas[UID].Account.GomokuBattleRecord.DisconnectCount;

            return resultData;
		}
    }
}