using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
    internal class PlayerStatus
    {
        [Key]
        public ulong UID { get; set; }

        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; }


        // 실력 판단용 내부 지표 레이팅
        public float Rating { get; set; }

        // 게임을 얼마나 많이 했는지 판단하는 지표, Exp가 일정량 찰 때마다 레벨 업
        public int Level { get; set; }
        public int ExperiencePoint { get; set; }
        public int MaxExperiencePoint { get; set; }

        // Max Exp 초기값을 0으로 세팅해서 테스트할 수 있기 때문에 Assert 하지 않음
        // 아니 잠깐, 웹서버가 Assert걸면 그냥 터지잖아, 안되지그건
        // 만렙 개념이 있다면 Max Exp가 0일 수도 있는데 기획이 확정된 것이 없으므로 일단 예외처리
        [NotMapped]
        public float ExperienceRate =>
            MaxExperiencePoint != 0 ? (float)ExperiencePoint / MaxExperiencePoint : 0f;


        public PlayerStatus() { }
        public PlayerStatus(PlayerAccount account)
        {
            Account = account;
            UID = account.UID;
            Rating = 1000;
            Level = 1;
            ExperiencePoint = 0;
            MaxExperiencePoint = 100;
        }
    }
}
