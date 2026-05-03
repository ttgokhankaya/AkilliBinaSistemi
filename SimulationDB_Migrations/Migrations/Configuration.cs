namespace SimulationDB_Migrations.Migrations
{
    using SimulationObjects;
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public sealed class Configuration : DbMigrationsConfiguration<SimulationDB_Migrations.DB>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(SimulationDB_Migrations.DB context)
        {
            SeedData(context);
        }

        public static void SeedData(SimulationDB_Migrations.DB context)
        {
            if (context.AreaBases.Any()) return;

            var areas = new[]
            {
                new AreaBase { Name = "Oturma Odası" },
                new AreaBase { Name = "Yatak Odası 1" },
                new AreaBase { Name = "Yatak Odası 2" },
                new AreaBase { Name = "Mutfak" },
                new AreaBase { Name = "Banyo" },
                new AreaBase { Name = "Koridor" },
            };
            context.AreaBases.AddRange(areas);
            context.SaveChanges();

            var living   = context.AreaBases.First(x => x.Name == "Oturma Odası");
            var bedroom1 = context.AreaBases.First(x => x.Name == "Yatak Odası 1");
            var bedroom2 = context.AreaBases.First(x => x.Name == "Yatak Odası 2");
            var kitchen  = context.AreaBases.First(x => x.Name == "Mutfak");
            var hallway  = context.AreaBases.First(x => x.Name == "Koridor");

            var devices = new[]
            {
                new DeviceBase { Name = "Oturma Odası Lambası",    ip = "192.168.1.10", AreaID = living.ID },
                new DeviceBase { Name = "Oturma Odası Sensörü",    ip = "192.168.1.11", AreaID = living.ID },
                new DeviceBase { Name = "Oturma Odası Termostası", ip = "192.168.1.12", AreaID = living.ID },
                new DeviceBase { Name = "Yatak Odası 1 Lambası",   ip = "192.168.1.20", AreaID = bedroom1.ID },
                new DeviceBase { Name = "Yatak Odası 1 Sensörü",   ip = "192.168.1.21", AreaID = bedroom1.ID },
                new DeviceBase { Name = "Yatak Odası 2 Lambası",   ip = "192.168.1.22", AreaID = bedroom2.ID },
                new DeviceBase { Name = "Yatak Odası 2 Sensörü",   ip = "192.168.1.23", AreaID = bedroom2.ID },
                new DeviceBase { Name = "Mutfak Lambası",          ip = "192.168.1.30", AreaID = kitchen.ID },
                new DeviceBase { Name = "Mutfak Akıllı Priz",      ip = "192.168.1.31", AreaID = kitchen.ID },
                new DeviceBase { Name = "Koridor Lambası",         ip = "192.168.1.40", AreaID = hallway.ID },
                new DeviceBase { Name = "Kapı Sensörü",            ip = "192.168.1.41", AreaID = hallway.ID },
            };
            context.DeviceBases.AddRange(devices);
            context.SaveChanges();

            context.Actors.AddRange(new[]
            {
                new Actor { Name = "Sakin 1" },
                new Actor { Name = "Sakin 2" },
            });
            context.SaveChanges();

            var now = DateTime.Now.Date;
            context.Operations.AddRange(new[]
            {
                new Operation { Name = "Sabah Rutini",  StartTime = now.AddHours(7),  Duration = TimeSpan.FromMinutes(30) },
                new Operation { Name = "Öğle Arası",   StartTime = now.AddHours(12), Duration = TimeSpan.FromMinutes(60) },
                new Operation { Name = "Akşam Rutini", StartTime = now.AddHours(19), Duration = TimeSpan.FromMinutes(45) },
                new Operation { Name = "Gece Rutini",  StartTime = now.AddHours(23), Duration = TimeSpan.FromMinutes(20) },
            });
            context.SaveChanges();
        }
    }
}
