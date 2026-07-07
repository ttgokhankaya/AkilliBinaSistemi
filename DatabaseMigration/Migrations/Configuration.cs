namespace DatabaseMigration.Migrations
{
    using DomainObjects;
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public sealed class Configuration : DbMigrationsConfiguration<DatabaseMigration.DB>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(DatabaseMigration.DB context)
        {
            SeedData(context);
        }

        public static void SeedData(DatabaseMigration.DB context)
        {
            if (context.AreaTypes.Any()) return;

            var now = DateTime.Now;

            var areaTypes = new[]
            {
                new AreaType { Name = "LivingRoom",  Definition = "Oturma odası",   CreatedDate = now, CreatedBy = "SEED" },
                new AreaType { Name = "Bedroom",     Definition = "Yatak odası",     CreatedDate = now, CreatedBy = "SEED" },
                new AreaType { Name = "Kitchen",     Definition = "Mutfak",          CreatedDate = now, CreatedBy = "SEED" },
                new AreaType { Name = "Bathroom",    Definition = "Banyo",           CreatedDate = now, CreatedBy = "SEED" },
                new AreaType { Name = "Hallway",     Definition = "Koridor",         CreatedDate = now, CreatedBy = "SEED" },
                new AreaType { Name = "HouseArea",   Definition = "Evin genel alanı", CreatedDate = now, CreatedBy = "SEED" },
            };
            context.AreaTypes.AddRange(areaTypes);
            context.SaveChanges();

            var houseType = context.AreaTypes.First(x => x.Name == "HouseArea");
            var livingType = context.AreaTypes.First(x => x.Name == "LivingRoom");
            var bedroomType = context.AreaTypes.First(x => x.Name == "Bedroom");
            var kitchenType = context.AreaTypes.First(x => x.Name == "Kitchen");
            var bathroomType = context.AreaTypes.First(x => x.Name == "Bathroom");

            var rootArea = new Area { Name = "Ev", Width = 120, Height = 80, AreaTypeID = houseType.ID, CreatedDate = now, CreatedBy = "SEED" };
            context.Areas.Add(rootArea);
            context.SaveChanges();

            var subAreas = new[]
            {
                new Area { Name = "Oturma Odası", Width = 30, Height = 20, AreaTypeID = livingType.ID,  AreaID = rootArea.ID, CreatedDate = now, CreatedBy = "SEED" },
                new Area { Name = "Yatak Odası 1", Width = 20, Height = 15, AreaTypeID = bedroomType.ID, AreaID = rootArea.ID, CreatedDate = now, CreatedBy = "SEED" },
                new Area { Name = "Yatak Odası 2", Width = 15, Height = 12, AreaTypeID = bedroomType.ID, AreaID = rootArea.ID, CreatedDate = now, CreatedBy = "SEED" },
                new Area { Name = "Mutfak",        Width = 18, Height = 14, AreaTypeID = kitchenType.ID, AreaID = rootArea.ID, CreatedDate = now, CreatedBy = "SEED" },
                new Area { Name = "Banyo",         Width = 10, Height = 8,  AreaTypeID = bathroomType.ID, AreaID = rootArea.ID, CreatedDate = now, CreatedBy = "SEED" },
            };
            context.Areas.AddRange(subAreas);
            context.SaveChanges();

            var living = context.Areas.First(x => x.Name == "Oturma Odası");
            var bedroom1 = context.Areas.First(x => x.Name == "Yatak Odası 1");
            var kitchen = context.Areas.First(x => x.Name == "Mutfak");

            var items = new[]
            {
                new Item { Name = "Oturma Odası Lambası", Availablity = true, IpV4 = "192.168.1.10", ItemType = "Light",          AreaOfItemID = living.ID,    CreatedDate = now, CreatedBy = "SEED" },
                new Item { Name = "Termostat",             Availablity = true, IpV4 = "192.168.1.11", ItemType = "Thermostat",     AreaOfItemID = living.ID,    CreatedDate = now, CreatedBy = "SEED" },
                new Item { Name = "Hareket Sensörü 1",    Availablity = true, IpV4 = "192.168.1.12", ItemType = "MotionSensor",   AreaOfItemID = living.ID,    CreatedDate = now, CreatedBy = "SEED" },
                new Item { Name = "Yatak Odası Lambası",  Availablity = true, IpV4 = "192.168.1.20", ItemType = "Light",          AreaOfItemID = bedroom1.ID,  CreatedDate = now, CreatedBy = "SEED" },
                new Item { Name = "Hareket Sensörü 2",   Availablity = true, IpV4 = "192.168.1.21", ItemType = "MotionSensor",   AreaOfItemID = bedroom1.ID,  CreatedDate = now, CreatedBy = "SEED" },
                new Item { Name = "Mutfak Lambası",       Availablity = true, IpV4 = "192.168.1.30", ItemType = "Light",          AreaOfItemID = kitchen.ID,   CreatedDate = now, CreatedBy = "SEED" },
                new Item { Name = "Akıllı Priz",          Availablity = true, IpV4 = "192.168.1.31", ItemType = "SmartPlug",      AreaOfItemID = kitchen.ID,   CreatedDate = now, CreatedBy = "SEED" },
            };
            context.Items.AddRange(items);
            context.SaveChanges();
        }
    }
}
