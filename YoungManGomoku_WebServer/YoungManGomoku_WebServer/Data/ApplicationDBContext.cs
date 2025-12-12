using Microsoft.EntityFrameworkCore;
using System.Numerics;
using YoungManGomoku_Protocol.Source;
using YoungManGomoku_WebServer.Data.DatabaseContext;

namespace YoungManGomoku_WebServer.Data
{
    public class ApplicationDBContext : DbContext
    {
        internal DbSet<PlayerProfile> PlayerProfileTable { get; set; }
        internal DbSet<PlayerBattleRecord> PlayerGomokuRecordTable { get; set; }

		public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        { 

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PlayerProfile PK
            modelBuilder.Entity<PlayerProfile>()
                .HasKey(p => p.UID);

            // AuthToken은 Unique
            modelBuilder.Entity<PlayerProfile>()
                .HasIndex(p => p.AuthToken)
                .IsUnique();

            // PlayerBattleRecord PK
            modelBuilder.Entity<PlayerBattleRecord>()
                .HasKey(b => b.UID); // Shared PK

            // 1:1 관계 설정
            modelBuilder.Entity<PlayerProfile>()
                .HasOne(p => p.BattleRecord)
                .WithOne(b => b.PlayerProfile)
                .HasForeignKey<PlayerBattleRecord>(b => b.UID); // FK = PK

            base.OnModelCreating(modelBuilder);
        }
    }
}
