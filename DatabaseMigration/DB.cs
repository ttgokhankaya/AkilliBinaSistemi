using DataAccess;
using DomainObjects;
using Npgsql;
using System.Data.Entity;

namespace DatabaseMigration
{
    [DbConfigurationType(typeof(NpgsqlDbConfiguration))]
    public class DB : DbContext, IDataContext
    {
        private const string DefaultConnStr = "Host=localhost;Port=5432;Database=adle_sim;Username=adle_user;Password=Password1;";

        public static string ConnStr =>
            System.Environment.GetEnvironmentVariable("ADLE_DB_CONNECTION") ?? DefaultConnStr;

        public DB() : base(new NpgsqlConnection(ConnStr), contextOwnsConnection: true)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Migrations create tables in "public"; align the runtime model so
            // queries target public.* instead of EF6's default dbo.* schema.
            modelBuilder.HasDefaultSchema("public");
        }

        public DbSet<Area> Areas { get; set; }
        public DbSet<AreaType> AreaTypes { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Memory> Memoryies { get; set; }

        public int SaveAllChanges() => base.SaveChanges();
    }
}
