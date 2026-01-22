using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;
using YoungManGomoku_WebServer.Sessions;

namespace YoungManGomoku_WebServer.SingletoneManager
{
	public class ItemManager
	{
		private readonly ILogger<ItemManager> _logger;

		// Item UID 없이 Item ID와 타입만을 가지고 판단하는 구조
		private readonly ConcurrentDictionary<ProfileImageType, ShopItemData> _profileShopItemDatas;
		private readonly ConcurrentDictionary<StoneSkinType, ShopItemData> _stoneSkinShopItemDatas;
		private readonly ConcurrentDictionary<BoardSkinType, ShopItemData> _boardSkinShopDatas;

		// 외부 수정 방지
		public IReadOnlyDictionary<ProfileImageType, ShopItemData> ProfilenShopDatas => _profileShopItemDatas;
		public IReadOnlyDictionary<StoneSkinType, ShopItemData> StoneSkinShopDatas => _stoneSkinShopItemDatas;
		public IReadOnlyDictionary<BoardSkinType, ShopItemData> BoardSkinShopDatas => _boardSkinShopDatas;


		public ItemManager(ILogger<ItemManager> logger)
		{
			_logger = logger;

			// readonly라서 일단 생성자에서 생성
			_profileShopItemDatas = new ConcurrentDictionary<ProfileImageType, ShopItemData>();
			_stoneSkinShopItemDatas = new ConcurrentDictionary<StoneSkinType, ShopItemData>();
			_boardSkinShopDatas = new ConcurrentDictionary<BoardSkinType, ShopItemData>();

			// 내부 데이터는 여기서 정의 후 조립
			InitializeShopItemData();
		}

		// 서버가 켜진 상태에서도 런타임에서 새 상점 목록을 업데이트할 수 있음을 염두
		// (ex. 운영 툴로 파일 입출력 형태로 새 상점 컨텐츠 업데이트해 스크립트를 새로 리로드해오는 형식으로 확장 가능)
		private void InitializeShopItemData()
		{
			// 인덱스가 0인 None을 제외하고 일단은 다 구매 가능함
			for (ProfileImageType imgType = ProfileImageType.None; imgType < ProfileImageType.MAXCOUNT; ++imgType)
			{
				// 공통 처리를 위한 for문
				_profileShopItemDatas[imgType].ItemType = ItemType.ProfileImage;
				_profileShopItemDatas[imgType].IsShopBuyAble = true;

				// 우선 현재는 별다른 파일입출력, 스크립트 로드 없는 하드 코딩
				switch (imgType)
				{
					case ProfileImageType.None:
						_profileShopItemDatas[imgType].IsShopBuyAble = false;
						_profileShopItemDatas[imgType].ItemName = "None";
						_profileShopItemDatas[imgType].Cost = 0;
						_profileShopItemDatas[imgType].LevelLimit = 0;
						break;
					case ProfileImageType.StudentBoy:
						_profileShopItemDatas[imgType].ItemName = "StudentBoy";
						_profileShopItemDatas[imgType].Cost = 300;
						_profileShopItemDatas[imgType].LevelLimit = 1;
						break;
					case ProfileImageType.StudentGirl:
						_profileShopItemDatas[imgType].ItemName = "StudentGirl";
						_profileShopItemDatas[imgType].Cost = 10000;
						_profileShopItemDatas[imgType].LevelLimit = 35;
						break;
					case ProfileImageType.GentleMan:
						_profileShopItemDatas[imgType].ItemName = "GentleMan";
						_profileShopItemDatas[imgType].Cost = 100;
						_profileShopItemDatas[imgType].LevelLimit = 0;
						break;
					case ProfileImageType.Maam:
						_profileShopItemDatas[imgType].ItemName = "Maam";
						_profileShopItemDatas[imgType].Cost = 250;
						_profileShopItemDatas[imgType].LevelLimit = 5;
						break;
					case ProfileImageType.GrandFather:
						_profileShopItemDatas[imgType].ItemName = "GrandFather";
						_profileShopItemDatas[imgType].Cost = 2500;
						_profileShopItemDatas[imgType].LevelLimit = 15;
						break;
					case ProfileImageType.GrandMather:
						_profileShopItemDatas[imgType].ItemName = "GrandMather";
						_profileShopItemDatas[imgType].Cost = 1000;
						_profileShopItemDatas[imgType].LevelLimit = 10;
						break;
					default:
						_logger.LogDebug($"[{DateTime.Now}] [Item Manager - Shop Init] Undefined Profile Image Type!!!");
						break;
				}
			}

			_stoneSkinShopItemDatas[StoneSkinType.None].ItemType = ItemType.StoneSkin;
			_stoneSkinShopItemDatas[StoneSkinType.None].IsShopBuyAble = false;

			_boardSkinShopDatas[BoardSkinType.None].ItemType = ItemType.BoardSkin;
			_boardSkinShopDatas[BoardSkinType.None].IsShopBuyAble = false;
		}

		// 클라이언트 전송용 데이터로 조립
		// 서버가 켜진 상태에서도 런타임에서 새 상점 목록을 업데이트할 수 있음을 염두
		// 따라서 const 형태로 조립해놓고 들고있는 것이 아닌. 매번 생성해 GC의 new 할당을 감수함
		public ShotItemBuyables ComposeBuyableData()
		{           
			// 상점 목록 갱신
			ShotItemBuyables shopItemBuyablesData = new ShotItemBuyables();
			
			// 프로필 이미지
			for (ProfileImageType imgType = ProfileImageType.None; imgType < ProfileImageType.MAXCOUNT; ++imgType)
			{
				if (_profileShopItemDatas.TryGetValue(imgType, out ShopItemData? shopItemData) == false || shopItemData == null) continue;
				shopItemBuyablesData.ProfilenShopDatas[(int)imgType] = shopItemData;
			}

			// 돌 스킨
			for (StoneSkinType stoneType = StoneSkinType.None; stoneType < StoneSkinType.MAXCOUNT; ++stoneType)
			{
				if (_stoneSkinShopItemDatas.TryGetValue(stoneType, out ShopItemData? shopItemData) == false || shopItemData == null) continue;
				shopItemBuyablesData.StoneSkinShopDatas[(int)stoneType] = shopItemData;
			}

			// 판 스킨
			for (BoardSkinType boardType = BoardSkinType.None ; boardType < BoardSkinType.MAXCOUNT; ++boardType)
			{
				if (_boardSkinShopDatas.TryGetValue(boardType, out ShopItemData? shopItemData) == false || shopItemData == null) continue;
				shopItemBuyablesData.BoardSkinShopDatas[(int)boardType] = shopItemData;
			}

			return shopItemBuyablesData;
		}
	}
}
