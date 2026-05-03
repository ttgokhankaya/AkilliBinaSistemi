using DataAccess;
using DomainObjects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration
{
    [System.Data.Entity.DbConfigurationType(typeof(NpgsqlDbConfiguration))]
    public class DB : DbContext, IDataContext
    {
        public DB()
        {
            this.Database.Connection.ConnectionString = @"Host=localhost;Port=5432;Database=adle_sim;Username=adle_user;Password=Password1;";
        }

        public DbSet<Area> Areas { get; set; }
        public DbSet<AreaType> AreaTypes { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Memory> Memoryies { get; set; }

        public int SaveAllChanges()
        {
            return base.SaveChanges();
        }
    }
}
