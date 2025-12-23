using System;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;
using YoungManGomoku_Protocol.TypeEnum.InGame;
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
		public UserTimer MyTimer { get; set; }
		public byte Row { get; set; }
        public byte Col { get; set; }
        
        // Unity Transform과 무관한 보드 좌표 Read 전용
		public byte X => Row;
		public byte Y => Col;
    }
    
    // 그밖에 인게임 요청은 IngameRequest enum값만 보내면 될 듯

    public class CS_InGameRequestDTO
    {
        public string IDToken { get; set; }
        public IngameRequest IngameRequest { get; set; }
    }
}

namespace YoungManGomoku_Protocol.ServerToClient
{
    // 서버에서는 클라이언트로 \"{내용}\" 형태의 문자열 아니면 DTO를 보내게 될 것이다.

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

        public SC_TimerSettingDTO TimerSettingDTO { get; set; }

        
        public SC_MatchResultDTO(OpponentPlayerData opponent, string msg, bool isSuccess, StoneColorType stoneColor, in SC_TimerSettingDTO timerSettingDTO) 
        { 
            OpponentPlayer = opponent;
            Message = msg;
            MatchingSuccess = isSuccess;
            MyStoneColorType = stoneColor;
            TimerSettingDTO = timerSettingDTO;
        }
    }
    
    // 게임이 시작될 때 클라가 받을 타이머 설정 정보
    public struct SC_TimerSettingDTO
    {
        public float MainTime { get; set; } // 처음에 누적하여 소모되는 자유시간
        public int ByoyomiCount { get; set; }// 처음에 제공되는 초읽기 개수
        public float ByoyomiSeconds { get; set; } // 초읽기 시간
        public int ByoyomiPurchaseAmount { get; set; } // 초읽기 구매 시 추가되는 개수

        public SC_TimerSettingDTO(float mainTime, int byoyomiCount, float byoyomiSeconds, int byoyomiPurchaseAmount)
        {
            MainTime = mainTime;
            ByoyomiCount = byoyomiCount;
            ByoyomiSeconds = byoyomiSeconds;
            ByoyomiPurchaseAmount = byoyomiPurchaseAmount;
        }
    }

    // 착수가 이루어질 때 상대방 클라이언트가 받을 착수 및 타이머 정보
    public class SC_OpponentMoveDTO
    {
        public UserTimer OpponentTimer { get; set; }
        public GameEndCode EndCode { get; set; }

        public byte Row { get; set; }
	    public byte Col { get; set; }

        // Unity Transform과 무관한 보드 좌표 Read 전용
        public byte X => Row;
        public byte Y => Col;

        // 재대결 가능 알림? 일단 만들어는 봤는데... 쓸 일이 있을까?
        public bool CanRequestRematch { get; set; }

        public SC_OpponentMoveDTO(byte row, byte col, UserTimer opponentTimer, GameEndCode endCode = GameEndCode.None)
	    {
		    Row = row;
		    Col = col;
		    OpponentTimer = opponentTimer;
		    EndCode = endCode; // None or GomokuLose or BlackUnmovable
	    }
    }
    
    /* 플레이어의 타이머를 반려시킬 때 보낼 동기화용 타이머 정보는 그냥 UserTimer 바로 보내주면 될 듯?
     * 상대방 착수 없이 게임 종료되는 케이스에는 EndCode enum값만 보내주면 될 듯? */
}


/*
단촐한 예정도 및 변경, 관련 사항 제작 완료 시 제거 예정
패킷타입은 웹서버 URL로 대체 예정


 public enum CS_PacketType
    {
        None,

        HeartBeat, // 심장박동, 이게 끊기면 클라 접속 끊긴거임
                   // 대상 A가 심장박동을 보냈을 때 대상 B의 심장박동이 1분째 끊겼다? 접속끊김

        Login,  // 로그인했어
                // ReLogin, // 팅겨서 재로그인했으니 저장된 보드정보를 넘겨줘
        MatchMaking, // 매칭시켜줘
        MatchingCancle, // 매칭취소
        ShopData,   // 상점정보 내놔
        BuyItem_Shop,   // 상점템 이거 살게
        BuyItem_Ingame, // 인게임 중 구매
        TakeBack,   // 무르기
        SetStone,   // 내돌 뒀다
        TimeOver,   // 내 시간 다 끝남
        Surrender,  // 항복


        MAXCount
    }



    // 서버가 클라로 보내는건데... 서버가 수동적이라 '클라의 요청'에만 반응해야함
    public enum SC_PacketType
    {
        GameResult, // 게임결과를 클라로 보냄
        Announce // 공지
    }

    // 매치메이킹 시 매칭된 상대 정보
    public class SC_MatchMaking_EnemyData // Packet
    {
        public string Nickname { get; set; }
        // 승률
        // 승리횟수
        // 프로필사진

        // 돌 타입을 서버에서 랜덤하게 정하기?
    }

    //상점

    public class SC_ShopData
    {
        // 상점 판매 카테고리
        // 프로필타입
        // 돌 스킨
        // 판 스킨
    }


    // 어드레서블 -> 웹서버 안쓰고 AWS로 바로?
    public class AddressableData
    {

    }





    // 인게임
    public class CS_SetStone
    {
        // 내가 둔 돌 위치를 서버에게 전달
        // row col
        // 내가 둔 시간을 서버에게 전달
        // 서버는 지금 서버시간초와 내가 둔 시간초를 비교할 것
    }

    public class CS_Surrender
    {
        // 클라가 항복을 눌렀음
    }

    public class CS_TakeBack
    {

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


    public class SC_pos // (2명 다 줘야됨)
    {
        // 상대가 둔 돌 위치 row col
        // bool 돌 시간차 계산해서 실격이면 false
    }
*/