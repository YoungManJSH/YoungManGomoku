using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;
using YoungManGomoku_Protocol.TypeEnum.InGame;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
    internal class PlayerEquip
    {
        [Key]
        public ulong UID { get; set; }
        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; }

        // 프로필이미지 = 캐릭터 (상점에서 팜)
        public ProfileImageType EquipProfile { get; set; }

        // 바둑돌 스킨
        // 현재 장착중인 스킨
        public StoneSkinType EquipStoneSkin { get; set; }

        // 바둑판 스킨
        public BoardSkinType EquipBoardSkin { get; set; }

        public PlayerEquip() { }
        public PlayerEquip(PlayerAccount account)
        {
            this.Account = account;
            this.UID = account.UID;

            this.EquipProfile = ProfileImageType.None;
            this.EquipStoneSkin = StoneSkinType.None;
            this.EquipBoardSkin = BoardSkinType.None;
        }
    }
}
