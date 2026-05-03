using DataAccess;
using DomainObjects;
using Npgsql;
using System.Data.Entity;

namespace DatabaseMigration
{
    [DbConfigurationType(typeof(NpgsqlDbConfiguration))]
    public class DB : DbContext, IDataContext
    {
        private const string ConnStr = "Host=localhost;Port=5432;Database=adle_sim;Username=adle_user;Password=Password1;";

        public DB() : base(new NpgsqlConnection(ConnStr), contextOwnsConnection: true)
        {
        }

        public DbSet<Area> Areas { get; set; }
        public DbSet<AreaType> AreaTypes { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Memory> Memoryies { get; set; }

        public int SaveAllChanges() => base.SaveChanges();
    }
}
