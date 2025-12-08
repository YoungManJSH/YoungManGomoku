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
    }
}
