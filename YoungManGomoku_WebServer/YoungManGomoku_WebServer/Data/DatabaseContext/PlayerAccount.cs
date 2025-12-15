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
		public string AuthToken { get; set; }
		public AuthLevel AuthLevel { get; set; }
		public string Nickname { get; set; }
		public DateTime RegisterDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastPlayDate { get; set; }	
		// Shared Key로 UID 제공
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