using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;
using YoungManGomoku_Protocol.Source.TypeEnum;

namespace YoungManGomoku_WebServer.Data.DatabaseContext
{
	internal class PlayerAccount
	{
		[Key]
		public ulong UID { get; set; }

		// Guest ID Judge
		public string AuthToken { get; set; }

		public AuthLevel AuthLevel { get; set; }

		public string Nickname { get; set; }

		// 인게임에서 사용할 돌 데이터... 그런데 인게임에서만 쓰지 않나? 어차피 서버가 들고있다가 요청하면 뿌리면 되겠지?
		// 확실한건 DB에 저장할 필요는 없는 데이터다. 
		// public Stone StoneType { get; set; }
		public DateTime RegisterDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastPlayDate { get; set; }

        public PlayerMoney Money { get; set; }
        public PlayerStatus Status { get; set; }
        public PlayerEquip Equip { get; set; }
        public ICollection<PlayerInventoryItem> Inventory { get; set; }
        public PlayerBattleRecord GomokuBattleRecord { get; set; }

        public PlayerAccount(string authToken, string nickname)
		{
			this.UID = ServerManager.GenerateUID64();
			this.AuthToken = authToken;
			this.Nickname = nickname;

			// 인증 레벨, 이게 만약 Ban이라면 로그인 시도 시 서버에서 막자
			this.AuthLevel = AuthLevel.Common;

			this.RegisterDate = DateTime.Now;
			this.LastLoginDate = DateTime.Now;
			this.LastPlayDate = DateTime.Now;

			Money = new PlayerMoney(this);
			Status = new PlayerStatus(this);
			Equip = new PlayerEquip(this);
			GomokuBattleRecord = new PlayerBattleRecord(this);

            Inventory = new List<PlayerInventoryItem>();
        }
    }   
}