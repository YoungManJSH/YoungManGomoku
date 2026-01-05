using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Security.Principal;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
	public class PlayerBattleRecord
	{
		[Key]
		public ulong UID { get; set; }

        [ForeignKey(nameof(UID))]
        public PlayerAccount Account { get; set; }

        // 승리 횟수
        public uint WinCount { get; set; }

		// 오목판이 꽉 찰 때까지 결판이 나지 않았다면 무승부 카운트
		public uint DrawCount { get; set; }

		public uint LoseCount { get; set; }

		public uint DisconnectCount { get; set; }

		public uint BattleCount => (WinCount + DrawCount + LoseCount);

		// 전체 승률은 승리 횟수 / 전체 판수 형태로 계산한다.
		// 게임을 1판도 플레이하지 않으면 DIV 0 예외이기 때문에 승률 0% 처리
		public float WinRate => BattleCount > 0 ? (float)WinCount / BattleCount : 0;
        
		public PlayerBattleRecord() { }
        public PlayerBattleRecord(PlayerAccount account)
		{
            this.Account = account;
            this.UID = account.UID;

            this.WinCount = 0;
            this.DrawCount = 0;
            this.LoseCount = 0;
            this.DisconnectCount = 0;
        }
	}
}
