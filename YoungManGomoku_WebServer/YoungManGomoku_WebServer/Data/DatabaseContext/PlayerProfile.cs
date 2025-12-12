using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;
using YoungManGomoku_Protocol.Source.TypeEnum;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
	internal class PlayerProfile
	{
		[Key]
		public ulong UID { get; set; }

		// Guest ID Judge
		public string AuthToken { get; set; }

		public AuthLevel AuthLevel { get; set; }

		public string Nickname { get; set; }

		// 인게임 재화, 상점 이용에 쓴다
		public int GameMoney { get; set; }

		// 캐쉬 재화, 과금 시 쓰기 위한 재화인데 이거 구현할 일 있을까? 
		// 세븐나이츠 루비같은 가챠겜 보석 느낌으로 만든 재화
		// 유료결제 직빵 거래로 퉁치면 2차 현금재화가 필요할 지 모르겠다.
		public int CashMoney { get; set; }


		// 프로필이미지 = 캐릭터 (상점에서 팜)
		public ProfileImageType EquipProfile { get; set; }
		// 총 보유중인 프로필... 이건 나중에 정하자
		// 예시 1 ) bool List. 미보유 아이템이면 해당 enum Index를 false로 하는 구조, on/off 도감 형식
		// List<bool> ImageInventory[ProfileImageType.MAXCOUNT]; 
		// 어차피 인벤토리를 만든다고 하면 아예 다른 DB 테이블을 새로 파지 싶다.
		//public bool[] ProfileImageCodex { get; set; }

		// 바둑돌 스킨
		// 현재 장착중인 스킨
		public StoneSkinType EquipStoneSkin { get; set; }
		// 총 보유중인 돌 스킨 인벤토리. false면 해당 index의 돌 스킨은 미보유
		//public bool[] StoneSkinCodex { get; set; }

		// 바둑판 스킨
		public BoardSkinType EquipBoardSkin { get; set; }
        // 총 보유중인 판 스킨 인벤토리. true면 해당 index의 Board 스킨은 보유중
        //public bool[] BoardSkinCodex { get; set; }

        // 실력 판단용 내부 지표 레이팅
        // MMR은 서버에서만 쓰고 클라이언트에서는 딱히 보여주지 않기로 합의함
        public float Rating { get; set; }

		// 게임을 얼마나 많이 했는지 판단하는 지표, Exp가 일정량 찰 때마다 레벨 업
		public int Level { get; set; }
		public int ExperiencePoint { get; set; }
		public int MaxExperiencePoint { get; set; }

		// Max Exp 초기값을 0으로 세팅해서 테스트할 수 있기 때문에 Assert 하지 않음
		// 아니 잠깐, 웹서버가 Assert걸면 그냥 터지잖아, 안되지그건
		// 만렙 개념이 있다면 Max Exp가 0일 수도 있는데 기획이 확정된 것이 없으므로 일단 예외처리
		public float ExperienceRate => MaxExperiencePoint != 0 ? (float)ExperiencePoint / MaxExperiencePoint : 0f;

		// 인게임에서 사용할 돌 데이터... 그런데 인게임에서만 쓰지 않나? 어차피 서버가 들고있다가 요청하면 뿌리면 되겠지?
		// 확실한건 DB에 저장할 필요는 없는 데이터다. 
		// public Stone StoneType { get; set; }
		public DateTime RegisterDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastPlayDate { get; set; }

        public PlayerBattleRecord BattleRecord { get; set; }

        public PlayerProfile(string nickname)
		{
			this.UID = ServerManager.GenerateUID64();
			this.AuthToken = nickname;
			this.Nickname = nickname;

			// 인증 레벨, 이게 만약 Ban이라면 로그인 시도 시 서버에서 막자
			this.AuthLevel = AuthLevel.Common;

			this.Rating = 1000;
			this.Level = 1;
			this.ExperiencePoint = 0;
			this.MaxExperiencePoint = 100;

			this.GameMoney = 0;
			this.CashMoney = 0;

			this.EquipProfile = ProfileImageType.None;
			this.EquipStoneSkin = StoneSkinType.None;
			this.EquipBoardSkin = BoardSkinType.None;

			this.RegisterDate = DateTime.Now;
			this.LastLoginDate = DateTime.Now;
			this.LastPlayDate = DateTime.Now;

			//this.ProfileImageCodex = new bool[(int)ProfileImageType.MAXCOUNT];
			//this.StoneSkinCodex = new bool[(int)StoneSkinType.MAXCOUNT];
			//this.BoardSkinCodex = new bool[(int)BoardSkinType.MAXCOUNT];
		}
    }   
}