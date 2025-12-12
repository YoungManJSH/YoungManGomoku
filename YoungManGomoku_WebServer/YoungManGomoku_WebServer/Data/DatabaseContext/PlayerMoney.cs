using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Principal;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
    internal class PlayerMoney
    {
        [Key]
        public ulong UID { get; set; }

        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; }


        // 인게임 재화, 상점 이용에 쓴다
        public int GameMoney { get; set; }

        // 캐쉬 재화, 과금 시 쓰기 위한 재화인데 이거 구현할 일 있을까? 
        // 세븐나이츠 루비같은 가챠게임 보석 느낌으로 만든 재화
        // 유료결제 직빵 거래로 퉁치면 2차 현금재화가 필요할 지 모르겠다.
        public int CashMoney { get; set; }

        public PlayerMoney(PlayerAccount account)
        {
            this.Account = account;
            this.UID = account.UID;

            this.GameMoney = 0;
            this.CashMoney = 0;
        }
    }
}
