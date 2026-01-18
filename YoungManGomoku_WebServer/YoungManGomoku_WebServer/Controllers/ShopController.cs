using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Linq;

using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
		private  ApplicationDBContext _context;
        private readonly ILogger<ShopController> _logger;
        private readonly ServerManager _serverManager;

        public ShopController(ILogger<ShopController> logger, ApplicationDBContext context, ServerManager serverManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
        }

        // 상점 입장함 (상점 판매 목록 요청)
        [HttpPost("RequestInventoryAndShopItemList")]
        public IActionResult ShopItemAnnounce([FromBody] string idToken)
        {
			// 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
			PlayerSession? player = _serverManager.GetPlayerSession(idToken);
			ulong uid = _serverManager.GetPlayerUID(idToken);
			// 인증 정보는 왔지만 유효한 세션이 아니다
			if (player == null || uid == 0)
			{
				_logger.LogWarning($"[{DateTime.Now}] [Shop Controller] 플레이어 세션 탐색 실패 [{uid}] : {idToken}");
				return Unauthorized($"[{idToken}] Player Session Not Found. Please Re Login.");
			}

            

			player.LastRequestTime = DateTime.UtcNow;


            

			PlayerInventoryData playerSkinInventory = new PlayerInventoryData();
            

			// DB에서 플레이어 인벤토리를 꺼내와서 보유중인 아이템은 true로 변경 후 전송할 것
            PlayerInventoryItem[] inventory = _context.PlayerInventoryTable.Where(m => m.UID == uid).ToArray();

            foreach(PlayerInventoryItem item in inventory)
            {
                switch(item.ItemType)
                {
                    case ItemType.ProfileImage:
                        playerSkinInventory.ProfileInventrory[item.ItemID] = true; 
                        break;
                    case ItemType.StoneSkin:
						playerSkinInventory.StoneSkinInventory[item.ItemID] = true;
						break;
                    case ItemType.BoardSkin:
						playerSkinInventory.BoardSkinInventory[item.ItemID] = true;
						break;
                    default:
						_logger.LogWarning($"[{DateTime.Now}] [Shop Controller] Unknown ItemType [{item.ItemType}][{item.ItemID}] 보유! [{uid}] 유저 DB 확인 요망.");
						break;
                }
            }

			// 상점 목록 갱신
			ShotItemBuyables shopItemData = new ShotItemBuyables();

			// 인덱스가 0인 None을 제외하고 일단은 다 구매 가능함

			for (ProfileImageType imgType = ProfileImageType.None; imgType  < ProfileImageType.MAXCOUNT; ++imgType)
			{
				int i = (int)imgType;
				shopItemData.ProfilenShopDatas[i].ItemType = ItemType.ProfileImage;
				shopItemData.ProfilenShopDatas[i].IsShopBuyAble = true;

				// 딱히 외부 기획 데이터 파싱이 없으므로 우선 서버에 직접 하드코딩 때림
				// 후일 바뀔지 안 바뀔지 미정
				// 서버매니저가 따로 들고있는것이 좋겠지만, 우선 TDD를 위한 선 적용

				switch (imgType)
				{
					case ProfileImageType.None:
						shopItemData.ProfilenShopDatas[i].IsShopBuyAble = false;
						shopItemData.ProfilenShopDatas[i].ItemName = "None";						
						shopItemData.ProfilenShopDatas[i].Cost = 0;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 0;
						break;
					case ProfileImageType.StudentBoy:
						shopItemData.ProfilenShopDatas[i].ItemName = "StudentBoy";
						shopItemData.ProfilenShopDatas[i].Cost = 300;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 1;
						break;
					case ProfileImageType.StudentGirl:
						shopItemData.ProfilenShopDatas[i].ItemName = "StudentGirl";
						shopItemData.ProfilenShopDatas[i].Cost = 5000;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 50;
						break;
					case ProfileImageType.GentleMan:
						shopItemData.ProfilenShopDatas[i].ItemName = "GentleMan";
						shopItemData.ProfilenShopDatas[i].Cost = 100;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 0;
						break;
					case ProfileImageType.Maam:
						shopItemData.ProfilenShopDatas[i].ItemName = "Maam";
						shopItemData.ProfilenShopDatas[i].Cost = 500;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 10;
						break;
					case ProfileImageType.GrandFather:
						shopItemData.ProfilenShopDatas[i].ItemName = "GrandFather";
						shopItemData.ProfilenShopDatas[i].Cost = 1000;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 15;
						break;
					case ProfileImageType.GrandMather:
						shopItemData.ProfilenShopDatas[i].ItemName = "GrandMather";
						shopItemData.ProfilenShopDatas[i].Cost = 250;
						shopItemData.ProfilenShopDatas[i].LevelLimit = 5;
						break;
				}
			}

			for (int i = 1; i < (int)StoneSkinType.MAXCOUNT; ++i)
			{
				shopItemData.StoneSkinShopDatas[i].ItemType = ItemType.StoneSkin;
				shopItemData.StoneSkinShopDatas[i].IsShopBuyAble = true;
			}
			for (int i = 1; i < (int)BoardSkinType.MAXCOUNT; ++i)
			{
				shopItemData.StoneSkinShopDatas[i].ItemType = ItemType.BoardSkin;
				shopItemData.BoardSkinShopDatas[i].IsShopBuyAble = true;
			}

			return Ok(new SC_FirstEnterShopDTO(playerSkinInventory, shopItemData));
        }

		// 아이템 장착 요청
		[HttpPost("EquipItem")]
		public IActionResult EquipItem([FromBody] CS_RequestEquipItemDTO equipItemDTO)
		{
			// 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
			PlayerSession? player = _serverManager.GetPlayerSession(equipItemDTO.IDToken);
			ulong uid = _serverManager.GetPlayerUID(equipItemDTO.IDToken);
			// 인증 정보는 왔지만 유효한 세션이 아니다
			if (player == null || uid == 0)
			{
				_logger.LogWarning($"[{DateTime.Now}] [Shop Controller] 플레이어 세션 탐색 실패 [{uid}] : {equipItemDTO.IDToken}");
				return Unauthorized($"[{equipItemDTO.IDToken}] Player Session Not Found. Please Re Login.");
			}

			PlayerEquip equipState = _context.PlayerEquipItemStateTable.Where(m => m.UID == uid).FirstOrDefault();

			if (equipItemDTO.EquipItemID < 0)
				return BadRequest("Equip Item ID < 0");

			// 소유중인 아이템 목록을 DB에서 가져와 소유중인지 검사
			bool isEquipable = false;
			
			// 사양 변경으로 존재가 말소된 아이템 (ex. 기간 한정으로만 사용 가능했던 스킨) 등이 존재하면 충분히 현재 enum과 다를 수 있다.
			// 따라서 아이템 타입 등의 enum이 유효한지 처리는 이후에 해야 하고, 지금은 보유중인지 아닌지만 탐색한다.
		    // 만약 기간 이후 삭제했어야 하는 아이템이 남아있는 유저라면 클라 뚜따거나 서버 처리 미흡이므로 서버에 로그가 남을 것이다.
			PlayerInventoryItem[] inventory = _context.PlayerInventoryTable.Where(m => m.UID == uid).ToArray();		
			for (int i = 0; i < inventory.Length; ++i)
			{
				if (inventory[i].ItemType != equipItemDTO.EquipItemType) continue;

				if (inventory[i].ItemID != equipItemDTO.EquipItemID) continue;

				// 보유중
				isEquipable = true;
				break;
			}

			if (isEquipable)
			{
				switch (equipItemDTO.EquipItemType)
				{
					case ItemType.ProfileImage:
						if ((ProfileImageType)equipItemDTO.EquipItemID >= ProfileImageType.MAXCOUNT)
						{
							_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip Profile] {equipItemDTO.EquipItemID} 라는 프로필은 없습니다.");
						}
						equipState.EquipProfile = (ProfileImageType)equipItemDTO.EquipItemID;
						break;
					case ItemType.StoneSkin:
						if ((StoneSkinType)equipItemDTO.EquipItemID >= StoneSkinType.MAXCOUNT)
						{
							_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip Stone] {equipItemDTO.EquipItemID} 라는 돌 스킨은 없습니다.");
						}
						equipState.EquipStoneSkin = (StoneSkinType)equipItemDTO.EquipItemID;
						break;
					case ItemType.BoardSkin:
						if ((BoardSkinType)equipItemDTO.EquipItemID >= BoardSkinType.MAXCOUNT)
						{
							_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip Board] {equipItemDTO.EquipItemID} 라는 보드 스킨은 없습니다.");
						}
						equipState.EquipBoardSkin = (BoardSkinType)equipItemDTO.EquipItemID;
						break;
					default:
						_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip] {equipItemDTO.EquipItemType} 라는 아이템 타입은 없습니다.");
						break;
				}

				// DB Process
				_context.PlayerEquipItemStateTable.Update(equipState);
				_context.SaveChanges();
			}
		
			return Ok(new SC_ResponseStringDTO("Equip Success", true));
		}

		// 물건 구매 요청
		[HttpPost("BuyItem")]
        public IActionResult BuyItem([FromBody] CS_RequestBuyItemDTO buyItemDTO)
        {
			// 일단 보내온 유저의 ID 토큰으로 서버에 접속중인 유저를 찾아온다
			PlayerSession? player = _serverManager.GetPlayerSession(buyItemDTO.IDToken);
			ulong uid = _serverManager.GetPlayerUID(buyItemDTO.IDToken);
			// 인증 정보는 왔지만 유효한 세션이 아니다
			if (player == null || uid == 0)
			{
				_logger.LogWarning($"[{DateTime.Now}] [Shop Controller] 플레이어 세션 탐색 실패 [{uid}] : {buyItemDTO.IDToken}");
				return Unauthorized($"[{buyItemDTO.IDToken}] Player Session Not Found. Please Re Login.");
			}

			return Ok(new SC_ResponseStringDTO("Buy Success", true));
        }
    }
}
