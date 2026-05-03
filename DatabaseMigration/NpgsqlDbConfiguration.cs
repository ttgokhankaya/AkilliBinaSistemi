using System.Data.Entity;
using Npgsql;

namespace DatabaseMigration
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
