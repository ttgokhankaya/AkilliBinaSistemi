using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Windows;
using System.Windows.Threading;

namespace GUI
{
    public partial class App : Application
    {
        static App()
        {
            DbConfiguration.SetConfiguration(new DatabaseMigration.NpgsqlDbConfiguration());
        }

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            RunMigrations();
        }

        private static void RunMigrations()
        {
            try
            {
                var migrator = new DbMigrator(new DatabaseMigration.Migrations.Configuration());
                migrator.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Veritabanı migration hatası:\n{ex.Message}\n\nDocker PostgreSQL çalışıyor mu kontrol edin.",
                    "Migration Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }
    }
}
