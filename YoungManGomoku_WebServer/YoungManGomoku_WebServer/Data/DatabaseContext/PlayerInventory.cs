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

        public PlayerInventoryItem(PlayerAccount account)
        {
            this.Account = account;
            this.UID = account.UID;
        }

        public PlayerInventoryItem(PlayerAccount account, ItemType type, uint itemId)
		{
			this.Account = account;
			this.UID = account.UID;
			this.ItemType = type;
			this.ItemID = itemId;
		}
	}
}
