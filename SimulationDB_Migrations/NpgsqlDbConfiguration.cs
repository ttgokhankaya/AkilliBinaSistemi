using System.Data.Entity;
using Npgsql;

namespace SimulationDB_Migrations
{
    public class NpgsqlDbConfiguration : DbConfiguration
    {
        public NpgsqlDbConfiguration()
        {
            SetDefaultConnectionFactory(new NpgsqlConnectionFactory());
            SetProviderServices("Npgsql", NpgsqlServices.Instance);
            SetProviderFactory("Npgsql", NpgsqlFactory.Instance);
        }
    }
}
