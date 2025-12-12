using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YoungManGomoku_Protocol.Source.TypeEnum;

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
        // 총 보유중인 프로필... 이건 나중에 정하자
        // 예시 1 ) bool List. 미보유 아이템이면 해당 enum Index를 false로 하는 구조, on/off 도감 형식
        // List<bool> ImageInventory[ProfileImageType.MAXCOUNT]; 
        // 어차피 인벤토리를 만든다고 하면 아예 다른 DB 테이블을 새로 파지 싶다.
        //public bool[] ProfileImageCodex { get; set; }

        // 바둑돌 스킨
        // 현재 장착중인 스킨
        public StoneSkinType EquipStoneSkin { get; set; }

        // 바둑판 스킨
        public BoardSkinType EquipBoardSkin { get; set; }
        // 총 보유중인 판 스킨 인벤토리. true면 해당 index의 Board 스킨은 보유중
        //public bool[] BoardSkinCodex { get; set; }


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
