using DataAccess;
using SimulationObjects;
using System.Data.Entity;

namespace SimulationDB_Migrations
{
    [System.Data.Entity.DbConfigurationType(typeof(NpgsqlDbConfiguration))]
    public class DB : DbContext, IDataContext

    {
        public DB()
        {
            this.Database.Connection.ConnectionString = @"Host=localhost;Port=5432;Database=adle_sim;Username=adle_user;Password=Password1;";
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Actor>().ToTable("Actor");
        }

        public int SaveAllChanges()
        {
            return base.SaveChanges();
        }

        public DbSet<DeviceBase> DeviceBases { get; set; }
        public DbSet<AreaBase> AreaBases { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<Actor> Actors { get; set; }

        public DbSet<Habit> Habits { get; set; }

        public DbSet<OperationDevice> OperationDevices { get; set; }

        public DbSet<OperationHabitMapping> OperationHabitMappings { get; set; }

        public DbSet<Scenario> Scenarios { get; set; }

        public DbSet<GraphObject> GraphObjects { get; set; }

        public DbSet<GraphNodeDeviceMapping> GraphNodeDeviceMappings { get; set; }

    }
}
