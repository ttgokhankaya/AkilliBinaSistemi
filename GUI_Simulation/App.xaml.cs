using AdleGraph;
using AdleGraph.Interfaces;
using IoC;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
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
            RunMigrations();

            Container.InitContainer();
            Container.Register<IGraph, Graph>();

            new SimulationPortal.PortalWindow().Show();
        }

        private static void RunMigrations()
        {
            try
            {
                var adle = new DbMigrator(new DatabaseMigration.Migrations.Configuration());
                adle.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Veritabanı migration hatası:\n{ex.Message}\n\nDocker PostgreSQL çalışıyor mu kontrol edin.",
                    "Migration Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            try
            {
                Database.SetInitializer<SimulationDB_Migrations.DB>(null);
                using (var db = new SimulationDB_Migrations.DB())
                {
                    db.Database.CreateIfNotExists();
                    SimulationDB_Migrations.Migrations.Configuration.SeedData(db);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Simülasyon DB hatası:\n{ex.Message}\n\nDocker PostgreSQL çalışıyor mu kontrol edin.",
                    "DB Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
