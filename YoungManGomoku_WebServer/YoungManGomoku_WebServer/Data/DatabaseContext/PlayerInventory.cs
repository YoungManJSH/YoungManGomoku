using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Principal;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
    public enum ItemType
    {
        BoardSkin = 0,
        StoneSkin = 1,
        ProfileImage = 2
    }

    internal class PlayerInventoryItem
    {
        [Key]
        public long InventoryId { get; set; } // optional

        public ulong UID { get; set; }

        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; }

        public ItemType ItemType { get; set; }

        public uint ItemId { get; set; }

        public DateTime AcquiredDate { get; set; } = DateTime.UtcNow;

        public PlayerInventoryItem(PlayerAccount account)
        {
            this.Account = account;
            this.UID = account.UID;

        }
    }
}
