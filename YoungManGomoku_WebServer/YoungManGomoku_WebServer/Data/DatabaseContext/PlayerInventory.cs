using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using System.Security.Principal;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
	public class PlayerInventoryItem
	{
        // 단일 Primary Key (Identity)
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long InventoryId { get; set; }

		// FK: PlayerAccount UID
		public ulong UID { get; set; }

        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; } = null!; // EF를 위한 null 허용, 도메인적으로는 "항상 존재"라는 의미 유지

		public ItemType ItemType { get; set; }

        // 어떤 스킨인가 enum 번호
        public uint ItemID { get; set; }

        public DateTime AcquiredDate { get; set; } = DateTime.UtcNow;

        public PlayerInventoryItem() {}

		// account 유저에게 type 형태의 itemID 아이템 추가
        public PlayerInventoryItem(PlayerAccount account, ItemType type, uint itemID)
		{
			this.Account = account;
			this.UID = account.UID;
			this.ItemType = type;
			this.ItemID = itemID;
		}
	}
}
