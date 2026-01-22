using System;

using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

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

        /// <summary>재대결 성사 상황에서 상대 정보 최신화</summary>
        /// <param name="rematchInform">재대결 성사 정보 DTO</param>
        public void UpdateData(SC_RematchResultDTO rematchInform)
        {
	        Level = rematchInform.OpponentPlayer.Level;
	        Rating = rematchInform.OpponentPlayer.Rating;
	        WinCount = rematchInform.OpponentPlayer.WinCount;
	        DrawCount = rematchInform.OpponentPlayer.DrawCount;
	        LoseCount = rematchInform.OpponentPlayer.LoseCount;
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
		
		/// <summary>유효하지 않은 타이머 정보인지를 반환</summary>
		/// <returns>
		/// <para>true: 0 이하의 타이머 정보 (유효하지 않음)</para>
		/// <para>false: 0보다 큰 타이머 정보 (유효)</para>
		/// </returns>
	    public bool IsInvalid()
	    {
		    // 둘 중 하나라도 음수면 유효하지 않음
		    if (MainTime < 0 || ByoyomiCount < 0) return true;

		    // 둘 다 0이어도 유효하지 않음
		    return MainTime == 0 && ByoyomiCount == 0;
	    }
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


	public class PlayerInventoryData
	{
        // enum type을 이용해 인덱스 접근
        // None은 기본적으로 보유했다고 판단 (기본프로필사진), 다른 스킨 장착 중에도 None으로 돌아갈 수 있어야 함
        // 상점에서 판매하지 않더라도 보유 및 장착이 가능해야함 (ex. None이나 기간 한정 판매 상품)
        public bool[] ProfileInventrory { get; set; }

		public bool[] StoneSkinInventory { get; set; }

		public bool[] BoardSkinInventory { get; set; }

        public PlayerInventoryData()
        {
			// 기본적으로 모든 인벤토리 배열의 값은 false

			ProfileInventrory = new bool[(int)ProfileImageType.MAXCOUNT];
			StoneSkinInventory = new bool[(int)StoneSkinType.MAXCOUNT];
			BoardSkinInventory = new bool[(int)BoardSkinType.MAXCOUNT];

			ProfileInventrory[(int)ProfileImageType.None] = true;
			StoneSkinInventory[(int)StoneSkinType.None] = true;
			BoardSkinInventory[(int)BoardSkinType.None] = true;

			// 서버에서 보유중인 아이템만 확인해 추가적으로 true로 바뀜
		}
	}


    public class ShopItemData
    {
        public ItemType ItemType { get; set; }
        public string ItemName { get; set; }
        public int Cost { get; set; }
        
        public int LevelLimit { get; set; }
		public bool IsShopBuyAble { get; set; }

        public ShopItemData()
        {
			ItemType = ItemType.None;
			ItemName = "";
			Cost = 0;
			IsShopBuyAble = false;
			LevelLimit = 0;
		}
        
		public ShopItemData(string name, ItemType type, int sellCost, bool buyAble, int levelLimit = 0)
        {
            ItemType = type;
			ItemName = name;
            Cost = sellCost;
			IsShopBuyAble = buyAble;
            LevelLimit = levelLimit;
        }
	}


    // 서버에서 보유한, 상점에서 구매 가능한 아이템 목록
    public class ShotItemBuyables
    {
		// enum type을 이용해 인덱스 접근
		// true면 판매중, false면 판매하지 않음
		// 판매하지 않는 스킨은 상점에 노출되지는 않지만, 이미 소유중이면 장착이 가능해야 하므로 노출 (None이나 기간 한정 스킨)
        // 레벨 제한 등 구매 조건부가 있다고 하더라도 일단 상점 구매는 가능하다는 뜻이므로 true


		// 현재 판매중인 프로필 이미지 목록
		public ShopItemData[] ProfilenShopDatas { get; set; }

		public ShopItemData[] StoneSkinShopDatas { get; set; }

		public ShopItemData[] BoardSkinShopDatas { get; set; }

        
		public ShotItemBuyables()
		{
			ProfilenShopDatas = new ShopItemData[(int)ProfileImageType.MAXCOUNT];
            for (ProfileImageType iType = 0; iType < ProfileImageType.MAXCOUNT; ++iType)
                ProfilenShopDatas[(int)iType] = new ShopItemData();
			
			StoneSkinShopDatas = new ShopItemData[(int)StoneSkinType.MAXCOUNT];
			for (StoneSkinType iType = 0; iType < StoneSkinType.MAXCOUNT; ++iType)
				StoneSkinShopDatas[(int)iType] = new ShopItemData();

			BoardSkinShopDatas = new ShopItemData[(int)BoardSkinType.MAXCOUNT];
			for (BoardSkinType iType = 0; iType < BoardSkinType.MAXCOUNT; ++iType)
				BoardSkinShopDatas[(int)iType] = new ShopItemData();
		}
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

        /// <summary> JSON 역직렬화를 위한 기본 생성자</summary>
        public CS_RequestTimerSynchroDTO() { }

	    public CS_RequestTimerSynchroDTO(string idToken, int nowTurn)
	    {
		    IDToken = idToken;
		    NowTurn = nowTurn;
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

    public class CS_RequestEquipItemDTO
    {
		public string IDToken { get; set; }

        // 지금 장착 요청하는 아이템 카테고리가 프로필인지 판인지 돌인지
        public ItemType EquipItemType { get; set; }

		// ProfileImageType 등을 형변환한 int 값. Request ItemType에 따라 Enum 의미가 바뀌므로 Int로 통일
		// 어떤 아이템을 장착하려는지의 요청
		public int EquipItemID { get; set; }

        public CS_RequestEquipItemDTO() { }
	}

    public class CS_RequestBuyItemDTO
    {
		public string IDToken { get; set; }

		// 지금 구매 요청하는 아이템 카테고리가 프로필인지 판인지 돌인지
		public ItemType BuyItemType { get; set; }

		// ProfileImageType 등을 형변환한 int 값. Request ItemType에 따라 Enum 의미가 바뀌므로 Int로 통일
        // 어떤 아이템을 구매하고 싶은지의 요청
		public uint BuyItemID { get; set; }

		public int GameMoney { get; set; } // 클라이언트가 보유한 돈, 서버 데이터와 비교 및 유효성 검사를 통해 변조 클라인지 확인

        public CS_RequestBuyItemDTO() { }
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

        /// <summary>재대결 성사 상황에서 매칭 정보 최신화</summary>
        /// <param name="rematchInform">재대결 성사 정보 DTO</param>
        public void ApplyRematchInform(SC_RematchResultDTO rematchInform)
        {
	        MyStoneColorType = rematchInform.MyStoneColor;
	        OpponentPlayer.UpdateData(rematchInform);
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
        public StoneColorType MyStoneColor { get; set; }
        public bool IsRematchSuccess { get; set; }

        public SC_RematchResultDTO(bool isRematchable, StoneColorType myStoneColor)
        {
            OpponentPlayer = new RematchOpponentData();
            IsRematchSuccess = isRematchable;
            MyStoneColor = myStoneColor;
        }
    }
    
    // 한 클라이언트에서 최초로 상점에 진입 시 요청해온 인벤토리, 상점 목록 응답
    // 서버 버전 등 목록이 바뀐 것이 아니라면 2회 이상 재요청 하지 않음
    public class SC_FirstEnterShopDTO
    {
        public PlayerInventoryData PlayerSkinInventory { get; set; }
        public ShotItemBuyables ShopItemData { get; set; }        

        public SC_FirstEnterShopDTO(PlayerInventoryData playerInventoryData, ShotItemBuyables shotItemBuyables)
        {
            PlayerSkinInventory = playerInventoryData;
            ShopItemData = shotItemBuyables;
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