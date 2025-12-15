using Microsoft.EntityFrameworkCore;
using System.Numerics;
using YoungManGomoku_Protocol.Source;
using YoungManGomoku_WebServer.Data.DatabaseContext;

namespace YoungManGomoku_WebServer.Data
{
    public class ApplicationDBContext : DbContext
    {
        internal DbSet<PlayerAccount> PlayerAccountTable { get; set; }       
        internal DbSet<PlayerStatus> PlayerStatusTable { get; set; }
        internal DbSet<PlayerBattleRecord> PlayerGomokuBattleRecordTable { get; set; }
        internal DbSet<PlayerMoney> PlayerMoneyTable { get; set; }
        internal DbSet<PlayerEquip> PlayerEquipItemStateTable { get; set; }
        internal DbSet<PlayerInventoryItem> PlayerInventoryTable { get; set; }
              
		public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        { 

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PlayerAccount PK
            modelBuilder.Entity<PlayerAccount>()
                .HasKey(p => p.UID);

            // AuthToken is Unique
            modelBuilder.Entity<PlayerAccount>()
                .HasIndex(p => p.AuthToken)
                .IsUnique();

            // 인벤토리 복합 키 설정
			// modelBuilder.Entity<PlayerInventoryItem>().HasKey(i => new { i.InventoryId, i.UID });

			// Shared Primary Key Setting
			modelBuilder.Entity<PlayerBattleRecord>()
                .HasKey(b => b.UID); // Shared PK

            modelBuilder.Entity<PlayerStatus>()
                .HasKey(s => s.UID); // Shared PK

            modelBuilder.Entity<PlayerMoney>()
                .HasKey(m => m.UID); // Shared PK

            modelBuilder.Entity<PlayerEquip>()
                .HasKey(e => e.UID); // Shared PK

            // Enum Setting
            modelBuilder.Entity<PlayerEquip>()
                .Property(e => e.EquipProfile)
                .HasConversion<uint>();

            modelBuilder.Entity<PlayerEquip>()
                .Property(e => e.EquipBoardSkin)
                .HasConversion<uint>();

            modelBuilder.Entity<PlayerEquip>()
                .Property(e => e.EquipStoneSkin)
                .HasConversion<uint>();

            modelBuilder.Entity<PlayerInventoryItem>()
                .Property(i => i.ItemType)
                .HasConversion<uint>();

			// 1:1 관계 설정
			modelBuilder.Entity<PlayerAccount>()
                .HasOne(p => p.GomokuBattleRecord)
                .WithOne(b => b.Account)
                .HasForeignKey<PlayerBattleRecord>(b => b.UID) // FK = PK
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerAccount>()
                .HasOne(a => a.Money)
                .WithOne(m => m.Account)
                .HasForeignKey<PlayerMoney>(m => m.UID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerAccount>()
                .HasOne(a => a.Status)
                .WithOne(s => s.Account)
                .HasForeignKey<PlayerStatus>(s => s.UID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerAccount>()
                .HasOne(a => a.Equip)
                .WithOne(e => e.Account)
                .HasForeignKey<PlayerEquip>(e => e.UID)
                .OnDelete(DeleteBehavior.Cascade);

			// 1:N 관계 설정
			modelBuilder.Entity<PlayerAccount>()
                .HasMany(a => a.Inventory)
                .WithOne(i => i.Account)
                .HasForeignKey(i => i.UID)
                .OnDelete(DeleteBehavior.Cascade);

            // 유저가 인벤토리를 갖고 있는 거니까 아래 처럼 코딩하면 가독성이 좋지 않다
            // 기능적으로는 동일하긴 하다
			// modelBuilder.Entity<PlayerInventoryItem>().HasOne(i => i.Account).WithMany(a => a.Inventory).HasForeignKey(i => i.UID).OnDelete(DeleteBehavior.Cascade);

			// 복합 키 대신 중복 방지 Unique Index (UID + ItemType + ItemId)
			modelBuilder.Entity<PlayerInventoryItem>()
				.HasIndex(i => new { i.UID, i.ItemType, i.ItemId })
				.IsUnique();

			base.OnModelCreating(modelBuilder);
        }
    }
}
