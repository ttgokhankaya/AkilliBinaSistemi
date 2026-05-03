using DataAccess;
using Npgsql;
using SimulationObjects;
using System.Data.Entity;

namespace SimulationDB_Migrations
{
    [DbConfigurationType(typeof(DatabaseMigration.NpgsqlDbConfiguration))]
    public class DB : DbContext, IDataContext
    {
        private const string ConnStr = "Host=localhost;Port=5432;Database=adle_sim;Username=adle_user;Password=Password1;";

        public DB() : base(new NpgsqlConnection(ConnStr), contextOwnsConnection: true)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Actor>().ToTable("Actor");
        }

        public int SaveAllChanges() => base.SaveChanges();

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
