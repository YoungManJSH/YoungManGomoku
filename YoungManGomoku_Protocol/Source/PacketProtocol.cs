using System;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;
using YoungManGomoku_Protocol.TypeEnum.InGame;
using Microsoft.VisualBasic;
using YoungManGomoku_Protocol.ServerToClient;

/*
 Protocol의 PlayerData는 단순 서버와 통신용
 서버 DB 테이블과의 구조 역시 다르다
 클라이언트 빌드에 포함되기 때문에 보안상 중요한 코드는 Protocol에 올리지 말 것
*/
/*
 Unity의 JsonUtil은 public field만 직렬화 가능하고 Property는 모른다.
 [SerializeField] private도 되긴 하는데 서버 코드상에서 조립하기 불편하다.
 반대로 ASP .net core는 public Property만 읽는다.
 -> Converter 만드는건 코드 중복이 너무 심하니 JsonUtil 내다 버리고 Newtonsoft 씁시다.
*/

// 클라이언트 to 서버 / 서버 to 클라이언트 공용
namespace YoungManGomoku_Protocol
{
    public class PlayerData
    {
        public string Nickname { get; set; }

		// 인게임 재화, 상점 이용에 쓴다
		public int GameMoney { get; set; }

		// 캐쉬 재화, 과금 시 쓰기 위한 재화인데 이거 구현할 일 있을까? 
		// 세븐나이츠 루비같은 가챠겜 보석 느낌으로 만든 재화
		// 유료결제 직빵 거래로 퉁치면 2차 현금재화가 필요할 지 모르겠다.
		public int CashMoney { get; set; }


		// 프로필이미지 = 캐릭터 (상점에서 팜)
		public ProfileImageType EquipProfile { get; set; }

		// 현재 장착중인 돌 스킨
		public StoneSkinType EquipStoneSkin { get; set; }

		// 현재 장착중인 바둑판 스킨
		public BoardSkinType EquipBoardSkin { get; set; }

		// 실력 판단용 내부 지표 레이팅
		public float Rating { get; set; }

		// 게임을 얼마나 많이 했는지 판단하는 지표, Exp가 일정량 찰 때마다 레벨 업
		public int Level { get; set; }
		public int ExperiencePoint { get; set; }
		public int MaxExperiencePoint { get; set; }

		// Max Exp 초기값을 0으로 세팅해서 테스트할 수 있기 때문에 Assert 하지 않음
		// 아니 잠깐, 웹서버가 Assert걸면 그냥 터지잖아, 안되지그건
		// 만렙 개념이 있다면 Max Exp가 0일 수도 있는데 기획이 확정된 것이 없으므로 일단 예외처리
		public float ExperienceRate => MaxExperiencePoint != 0 ? (float)ExperiencePoint / MaxExperiencePoint : 0f;

        // 회원가입시간
        public DateTime RegisterDate { get; set; }

		// 마지막 로그인 시간
		public DateTime LastLoginDate { get; set; }

		// 마지막 오목 플레이 시간
		public DateTime LastPlayDate { get; set; }


		// 승리 횟수
		public uint WinCount { get; set; }

		// 오목판이 꽉 찰 때까지 결판이 나지 않았다면 무승부 카운트
		public uint DrawCount { get; set; }

		public uint LoseCount { get; set; }

		public uint DisconnectCount { get; set; }

		public uint BattleCount => (WinCount + DrawCount + LoseCount);

        // 전체 승률은 승리 횟수 / 전체 판수 형태로 계산한다.
        // 게임을 1판도 플레이하지 않으면 DIV 0 예외이기 때문에 승률 0% 처리
        public float WinRate => BattleCount > 0 ? (float)WinCount / BattleCount : 0;

        public void UpdateData(ref GameRecord record)
        {
	        Rating = record.Rating;
	        Level = record.Level;
	        ExperiencePoint = record.ExperiencePoint;
	        MaxExperiencePoint = record.MaxExperiencePoint;
	        GameMoney = record.GameMoney;
	        WinCount = record.WinCount;
	        DrawCount = record.DrawCount;
	        LoseCount = record.LoseCount;
        }
    }

    public class OpponentPlayerData
    {
        public string Nickname { get; set; }

		// 장착중인 프로필 이미지
		public ProfileImageType EquipProfile { get; set; }

		// 현재 장착중인 바둑돌 스킨
		public StoneSkinType EquipStoneSkin { get; set; }

		// 바둑판 스킨
		public BoardSkinType EquipBoardSkin { get; set; }

		public float Rating { get; set; }

		public int Level { get; set; }

		// 승리 횟수
		public uint WinCount { get; set; }

		// 오목판이 꽉 찰 때까지 결판이 나지 않았다면 무승부 카운트
		public uint DrawCount { get; set; }

		public uint LoseCount { get; set; }

		public uint DisconnectCount { get; set; }

		public uint BattleCount => (WinCount + DrawCount + LoseCount);

        // 전체 승률은 승리 횟수 / 전체 판수 형태로 계산한다.
        // 게임을 1판도 플레이하지 않으면 DIV 0 예외이기 때문에 승률 0% 처리
        public float WinRate => BattleCount > 0 ? (float)WinCount / BattleCount : 0;

        public void UpdateData(RematchOpponentData data)
        {
	        Level = data.Level;
	        Rating = data.Rating;
	        WinCount = data.WinCount;
	        DrawCount = data.DrawCount;
	        LoseCount = data.LoseCount;
        }
    }

    /// <summary> 서버-클라이언트 간 타이머 전송용 DTO </summary>
    public struct TimerSyncData
    {
	    public float MainTime { get; set; }     // 누적하여 소모되는 자유시간
		public int ByoyomiCount { get; set; }   // 현재 보유한 초읽기 개수

		public TimerSyncData(float mainTime, int byoyomiCount)
	    {
		    MainTime = mainTime;
		    ByoyomiCount = byoyomiCount;
	    }
	    
	    // 프로퍼티로 만들면 직렬화되어 날아가므로 주의
	    public bool IsDefault() => MainTime == 0 && ByoyomiCount == 0;
    }

	///<summary> 
	/// 게임이 시작될 때 클라가 받을 타이머 설정 정보 
	/// 서버도 매칭 잡고 방 생성 시 Default Setting을 위해 사용하므로 namespace 위치 이동
	/// 단, 송수신 자체는 여전히 매칭 성공 시 Server To Client 1번만 사용 (통상적으로는 TimerSyncData를 사용)
	/// </summary>
	public struct TimerSettingData
	{
		public float MainTime { get; set; }         // 처음에 누적하여 소모되는 자유시간
		public int ByoyomiCount { get; set; }       // 처음에 제공되는 초읽기 개수
		public float ByoyomiSeconds { get; set; }   // 초읽기 시간
		public int ByoyomiPurchaseAmount { get; set; } // 초읽기 구매 시 추가되는 개수

		public TimerSettingData(float mainTime, int byoyomiCount, float byoyomiSeconds, int byoyomiPurchaseAmount)
		{
			MainTime = mainTime;
			ByoyomiCount = byoyomiCount;
			ByoyomiSeconds = byoyomiSeconds;
			ByoyomiPurchaseAmount = byoyomiPurchaseAmount;
		}
	}
    

    public struct GameRecord
    {
        public GameEndCode EndCode { get; set; }
        public float Rating { get; set; }
        public int Level { get; set; }
        public int ExperiencePoint { get; set; }
        public int MaxExperiencePoint { get; set; }
        public int GameMoney { get; set; }
        public uint WinCount { get; set; }
        public uint DrawCount { get; set; }
        public uint LoseCount { get; set; }
    }
}

namespace YoungManGomoku_Protocol.ClientToServer
{
    public class CS_AccountRegisterDTO
    {
        public string UserNickname { get; set; }
        public string IdToken { get; set; }
		public bool IsGuest { get; set; }
	}

    public class CS_PlaceStoneDTO
    {
        public string IDToken { get; set; }
		public TimerSyncData MyTimer { get; set; }
		public byte Row { get; set; }
        public byte Col { get; set; }

        /// <summary> JSON 역직렬화용 기본 생성자 </summary>
        public CS_PlaceStoneDTO() { }

		public CS_PlaceStoneDTO(string idToken, TimerSyncData myTimer, int row, int col)
		{
			IDToken = idToken;
			MyTimer = myTimer;
			Row = (byte)row;
			Col = (byte)col;
		}
    }

    public class CS_RequestTimerSynchroDTO
    {
	    public string IDToken { get; set; }
	    public int NowTurn { get; set; }
        public TimerSyncData MyTimer { get; set; }

        /// <summary> JSON 역직렬화를 위한 기본 생성자</summary>
        public CS_RequestTimerSynchroDTO() { }

	    public CS_RequestTimerSynchroDTO(string idToken, int nowTurn, TimerSyncData myTimer)
	    {
		    IDToken = idToken;
		    NowTurn = nowTurn;
            MyTimer = myTimer;
	    }
    }
    
    public class CS_InGameRequestDTO
    {
        public string IDToken { get; set; }
        public IngameRequestType IngameRequest { get; set; }

        /// <summary> JSON 역직렬화를 위한 기본 생성자 </summary>
        public CS_InGameRequestDTO() { }

        public CS_InGameRequestDTO(string idToken, IngameRequestType request)
        {
	        IDToken = idToken;
	        IngameRequest = request;
        }
    }

    public class CS_PermitDTO
    {
        public string IDToken { get; set; }
        public bool IsPermit { get; set; }

        /// <summary> JSON 역직렬화를 위한 기본 생성자 </summary>
        public CS_PermitDTO() { }

        public CS_PermitDTO(string idToken, bool isPermit)
        {
            IDToken = idToken;
			IsPermit = isPermit;
        }
    }
}

namespace YoungManGomoku_Protocol.ServerToClient
{
    // 서버에서는 클라이언트로 \"{내용}\" 형태의 문자열 아니면 DTO를 보내게 될 것이다.
    
    /// <summary> 단순 서버 응답 요청 결과 </summary>
    public class SC_ResponseStringDTO
    {
        public string Message{ get; set; }
        public bool IsSuccess { get; set; }
        public SC_ResponseStringDTO(string msg, bool isSuccess)
        {
            Message = msg;
            IsSuccess = isSuccess;
        }
    }
    
    public class SC_MatchResultDTO
    {
        public OpponentPlayerData OpponentPlayer { get; set; }
		public string Message { get; set; }
		public bool MatchingSuccess { get; set; }

        public StoneColorType MyStoneColorType { get; set; }

        public TimerSettingData TimerSettingDTO { get; set; }
  
        public SC_MatchResultDTO(OpponentPlayerData opponent, string msg, bool isSuccess, StoneColorType stoneColor, in TimerSettingData timerSettingDTO) 
        { 
            OpponentPlayer = opponent;
            Message = msg;
            MatchingSuccess = isSuccess;
            MyStoneColorType = stoneColor;
            TimerSettingDTO = timerSettingDTO;
        }
    }
   

    /// <summary> 착수가 이루어질 때 상대방 클라이언트가 받을 착수 위치 및 타이머 정보 </summary>
    public class SC_OpponentPlaceStoneDTO
    {
	    /// <summary> 상대방의 타이머 </summary>
        public TimerSyncData OpponentTimer { get; set; }

        public byte Row { get; set; }
	    public byte Col { get; set; }

        public SC_OpponentPlaceStoneDTO(TimerSyncData opponentTimer, byte row, byte col)
	    {
		    OpponentTimer = opponentTimer;
		    Row = row;
		    Col = col;
	    }
    }

    public class SC_WaitEventDTO
    {
        // 상대방이 항복, 무르기 요청, 초읽기 구매 등을 했을 때
        public IngameRequestType OpponentRequest { get; set; }

        public bool IsTakeBackSuccess { get; set; }

        public SC_WaitEventDTO(IngameRequestType opponentRequest)
        {
            OpponentRequest = opponentRequest;
        }
    }

    public struct RematchOpponentData
    {
        public int Level { get; set; }
        public float Rating { get; set; }
        public uint WinCount { get; set; }
        public uint DrawCount { get; set; }
        public uint LoseCount { get; set; }

        public RematchOpponentData(int level, float rating, uint winCount, uint drawCount, uint loseCount)
        {
            Level = level;
            Rating = rating;
            WinCount = winCount;
            DrawCount = drawCount;
            LoseCount = loseCount;
        }
    }

    public class SC_RematchResultDTO
    {
        public RematchOpponentData OpponentPlayer { get; set; }
        public bool IsRematchSuccess { get; set; }

        public SC_RematchResultDTO(bool isRematchable)
        {
            OpponentPlayer = new RematchOpponentData();
            IsRematchSuccess = isRematchable;
        }
    }
}


/*
단촐한 예정도 및 변경, 관련 사항 제작 완료 시 제거 예정
패킷타입은 웹서버 URL로 대체 예정

    // 어드레서블 -> 웹서버 안쓰고 AWS로 바로?
    public class AddressableData
    {

    }

    //상점
    public class SC_ShopData
    {
        // 상점 판매 카테고리
        // 프로필타입
        // 돌 스킨
        // 판 스킨
    }

    // ItemID
    public enum ItemIDType
    {
        // 1001 스킨A
        // 1002 스킨B
        // 2001 돌A
        // 2002 돌B
        // 1 강제무르기 (rate-1)
        // 2 시간연장
        // 
    }

    public class CS_BuyItem
    {
        public CS_PacketType type { get; set; }
        public ItemIDType ItemID { get; set; }
    }
*/