using AdleGraph;
using AdleGraph.Interfaces;
using IoC;
using Npgsql;
using System;
using System.Data.Entity;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GUI_Simulation
{
    public partial class App : Application
    {
        static App()
        {
            DbConfiguration.SetConfiguration(new DatabaseMigration.NpgsqlDbConfiguration());
        }

        public App()
        {
            InitializeDatabase();

            Container.InitContainer();
            Container.Register<IGraph, Graph>();

            new SimulationPortal.PortalWindow().Show();
        }

        // Both EF6 contexts share one PostgreSQL database. The schema is created
        // deterministically from db/schema.sql (shipped as an embedded resource)
        // rather than via DbMigrator: the EF6 migration snapshots were left in an
        // inconsistent state by the SQL Server -> PostgreSQL port and Npgsql EF6
        // cannot regenerate them. The contexts only query/seed (public schema).
        private static void InitializeDatabase()
        {
            try
            {
                Database.SetInitializer<DatabaseMigration.DB>(null);
                Database.SetInitializer<SimulationDB_Migrations.DB>(null);

                EnsureDatabaseExists();
                ExecuteSchemaScript();

                using (var db = new DatabaseMigration.DB())
                    DatabaseMigration.Migrations.Configuration.SeedData(db);

                using (var db = new SimulationDB_Migrations.DB())
                    SimulationDB_Migrations.Migrations.Configuration.SeedData(db);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Veritabanı hazırlama hatası:\n{ex.Message}\n\nDocker PostgreSQL çalışıyor mu kontrol edin.",
                    "Veritabanı Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static void EnsureDatabaseExists()
        {
            var builder = new NpgsqlConnectionStringBuilder(DatabaseMigration.DB.ConnStr);
            string targetDb = builder.Database;
            builder.Database = "postgres";

            using (var conn = new NpgsqlConnection(builder.ConnectionString))
            {
                conn.Open();
                bool exists;
                using (var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", conn))
                {
                    check.Parameters.AddWithValue("n", targetDb);
                    exists = check.ExecuteScalar() != null;
                }
                if (!exists)
                {
                    using (var create = new NpgsqlCommand($"CREATE DATABASE \"{targetDb}\"", conn))
                        create.ExecuteNonQuery();
                }
            }
        }

        private static void ExecuteSchemaScript()
        {
            var asm = Assembly.GetExecutingAssembly();
            string sql;
            using (var stream = asm.GetManifestResourceStream("schema.sql"))
            using (var reader = new StreamReader(stream))
                sql = reader.ReadToEnd();

            using (var conn = new NpgsqlConnection(DatabaseMigration.DB.ConnStr))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }
    }
}
