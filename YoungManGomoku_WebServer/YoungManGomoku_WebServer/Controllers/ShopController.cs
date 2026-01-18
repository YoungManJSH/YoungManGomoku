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

			for (ProfileImageType i = ProfileImageType.None; i < ProfileImageType.MAXCOUNT; ++i)
			{
				shopItemData.ProfilenShopDatas[(int)i].ItemType = ItemType.ProfileImage;
				shopItemData.ProfilenShopDatas[(int)i].IsShopBuyAble = true;

				// 딱히 외부 기획 데이터 파싱이 없으므로 우선 서버에 직접 하드코딩 때림
				// 후일 바뀔지 안 바뀔지 미정
				// 서버매니저가 따로 들고있는것이 좋겠지만, 우선 TDD를 위한 선 적용

				switch (i)
				{
					case ProfileImageType.None:
						shopItemData.ProfilenShopDatas[(int)i].IsShopBuyAble = false;
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "None";						
						shopItemData.ProfilenShopDatas[(int)i].Cost = 0;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 0;
						break;
					case ProfileImageType.StudentBoy:
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "StudentBoy";
						shopItemData.ProfilenShopDatas[(int)i].Cost = 300;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 1;
						break;
					case ProfileImageType.StudentGirl:
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "StudentGirl";
						shopItemData.ProfilenShopDatas[(int)i].Cost = 5000;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 50;
						break;
					case ProfileImageType.GentleMan:
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "GentleMan";
						shopItemData.ProfilenShopDatas[(int)i].Cost = 100;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 0;
						break;
					case ProfileImageType.Maam:
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "Maam";
						shopItemData.ProfilenShopDatas[(int)i].Cost = 500;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 10;
						break;
					case ProfileImageType.GrandFather:
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "GrandFather";
						shopItemData.ProfilenShopDatas[(int)i].Cost = 1000;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 15;
						break;
					case ProfileImageType.GrandMather:
						shopItemData.ProfilenShopDatas[(int)i].ItemName = "GrandMather";
						shopItemData.ProfilenShopDatas[(int)i].Cost = 250;
						shopItemData.ProfilenShopDatas[(int)i].LevelLimit = 5;
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
