using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using System.Security.Principal;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
    public enum ItemType
    {
        BoardSkin = 0,
        StoneSkin = 1,
        ProfileImage = 2
    }

	public class PlayerInventoryItem
	{
        // 단일 Primary Key (Identity)
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long InventoryId { get; set; }

		// FK: PlayerAccount UID
		public ulong UID { get; set; }

        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; }

        public ItemType ItemType { get; set; }

        public uint ItemId { get; set; }

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
			this.ItemId = itemId;
		}
	}
}
