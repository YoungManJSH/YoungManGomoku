using System;
using YoungManGomoku_WebServer.Data.DatabaseContext;

namespace YoungManGomoku_WebServer.Sessions
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
			AuthToken = Account.AuthToken;  // caching
			IsConnected = isConnect;
			IsMatching = false;
			IsInGame = false;
			LoginTime = DateTime.UtcNow;
			LastRequestTime = DateTime.UtcNow;
			LastHeartbeatTime = DateTime.UtcNow;
			LastActionTime = DateTime.UtcNow;
		}
	}

}
