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
		private readonly ItemManager _itemManager;

        public ShopController(ILogger<ShopController> logger, ApplicationDBContext context, ServerManager serverManager, ItemManager itemManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
			_itemManager = itemManager;
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

			return Ok(new SC_FirstEnterShopDTO(playerSkinInventory, _itemManager.ComposeBuyableData()));
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
							return Ok(new SC_ResponseStringDTO("Unknow Profile Equip Fail", false));
						}
						equipState.EquipProfile = (ProfileImageType)equipItemDTO.EquipItemID;
						break;
					case ItemType.StoneSkin:
						if ((StoneSkinType)equipItemDTO.EquipItemID >= StoneSkinType.MAXCOUNT)
						{
							_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip Stone] {equipItemDTO.EquipItemID} 라는 돌 스킨은 없습니다.");
							return Ok(new SC_ResponseStringDTO("Unknow Stone Equip Fail", false));
						}
						equipState.EquipStoneSkin = (StoneSkinType)equipItemDTO.EquipItemID;
						break;
					case ItemType.BoardSkin:
						if ((BoardSkinType)equipItemDTO.EquipItemID >= BoardSkinType.MAXCOUNT)
						{
							_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip Board] {equipItemDTO.EquipItemID} 라는 보드 스킨은 없습니다.");
							return Ok(new SC_ResponseStringDTO("Unknow Board Equip Fail", false));
						}
						equipState.EquipBoardSkin = (BoardSkinType)equipItemDTO.EquipItemID;
						break;
					default:
						_logger.LogWarning($"[{DateTime.Now}] [Shop] [Equip Unknown] {equipItemDTO.EquipItemType} 라는 아이템 타입은 없습니다.");
						return Ok(new SC_ResponseStringDTO("Unknow ItemType Equip Fail", false));
				}

				// DB Process
				_context.PlayerEquipItemStateTable.Update(equipState);
				_context.SaveChanges();
			}
			else
			{
				// 보유중이지 않은 아이템 장착을 시도
				return Ok(new SC_ResponseStringDTO("Equip Fail", true));
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

			
			if (buyItemDTO.GameMoney != player.Account.Money.GameMoney)
			{
				_logger.LogWarning($"[{DateTime.Now}] [Shop Controller] 플레이어가 요청해온 소지금과 서버가 확인한 소지금이 다릅니다! 클라이언트 변조 같아요! [{uid}] : {buyItemDTO.IDToken}");
				return Unauthorized($"[{buyItemDTO.IDToken}] The Values of Client Game Money and Server Game Money are different!!!");
			}

			ShopItemData? buyTargetItem = null;
			switch(buyItemDTO.BuyItemType)
			{
				case ItemType.ProfileImage:
					_itemManager.ProfilenShopDatas.TryGetValue((ProfileImageType)buyItemDTO.BuyItemID, out buyTargetItem);
					break;
				case ItemType.StoneSkin:
					_itemManager.StoneSkinShopDatas.TryGetValue((StoneSkinType)buyItemDTO.BuyItemID, out buyTargetItem);
					break;
				case ItemType.BoardSkin:
					_itemManager.BoardSkinShopDatas.TryGetValue((BoardSkinType)buyItemDTO.BuyItemID, out buyTargetItem);
					break;
			}

			if (buyTargetItem == null)
			{
				_logger.LogDebug($"[{DateTime.Now}] [Shop Controller] 알 수 없는 아이템 타입 혹은 아이템 ID입니다. - Type {buyItemDTO.BuyItemType} : [{buyItemDTO.BuyItemID}]");
				return BadRequest($"Type {buyItemDTO.BuyItemType} : [{buyItemDTO.BuyItemID}] is Unkonw Item Type or Unknown Item ID.");
			}

			// 레벨 제한과 소지금 검사
			if (buyTargetItem.LevelLimit <= player.Account.Status.Level && buyTargetItem.Cost <= player.Account.Money.GameMoney)
			{
				// 서버 세션의 소지금 변경 후 DB 업데이트
				player.Account.Money.GameMoney -= buyTargetItem.Cost;
				_context.PlayerMoneyTable.Update(player.Account.Money);

				// 인벤토리 DB 테이블에 새 행 추가
				PlayerInventoryItem newItem = new PlayerInventoryItem(player.Account, buyTargetItem.ItemType, buyItemDTO.BuyItemID);
				_context.PlayerInventoryTable.Add(newItem);

				_context.SaveChanges();
				return Ok(new SC_ResponseStringDTO("Buy Success", true));
			}

			return Ok(new SC_ResponseStringDTO("Buy Failed. Not enough Level or GameMoney.", false));
        }
    }
}
