using DataAccess;
using DataAccess.Repository;
using DatabaseMigration;
using DomainObjects;
using SharedObject;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace MemoryModel
{
    public class MemoryManager : IMemoryManager
    {
        private AdleSCUBase _manager;
        public MemoryManager(AdleSCUBase manager)
        {
            _manager = manager;
        }

        private IUnitOfWork GetContext { get { return UnitOfWorkFactory.CreateBasicContext(new DB()); } }

        public List<AdleMemoryObject> Memories { get; set; }

        public AdleSCUBase Manager
        {
            get
            {
                return _manager;
            }

            set
            {
                _manager = value;
            }
        }

        public string Name { get; set; }

        public bool AnalyzeMemory(AdleMemoryObject memory)
        {
            using (var uow = GetContext)
            {
                var foundMemory = uow.Repository<Memory>().Find(x => x.AreaID == memory.Area.ID && x.ItemID == memory.Item.ID && x.ActionName == memory.ActionName).OrderBy(x => x.Date).FirstOrDefault();

                return memory.ActionValue.Equals(foundMemory.ActionValue);
            }
        }

        public void AddMemory(AdleMemoryObject memory)
        {
            using (var uow = GetContext)
            {
                var foundMemory = uow.Repository<Memory>().Find(x => x.ItemID == memory.Item.ID && x.ActionName == memory.ActionName).OrderByDescending(x => x.Date).Take(1).FirstOrDefault();

                if (foundMemory == null)
                {
                    var newMemory = new Memory()
                    {
                        ActionName = memory.ActionName,
                        ActionValue = memory.ActionValue,
                        AreaID = memory.Area.ID,
                        Date = memory.MemoryMoment,
                        Definition = memory.ToString(),
                        ItemID = memory.Item.ID,
                        CreatedBy = "USER",
                        CreatedDate = DateTime.Now
                    };
                    AddMemoryToDatabase(memory, uow, newMemory);

                }
                else if (!memory.ActionValue.Equals(foundMemory.ActionValue))
                    AddMemoryToDatabase(memory, uow, foundMemory);
            }
        }

        private static void AddMemoryToDatabase(AdleMemoryObject memory, IUnitOfWork uow, Memory foundMemory)
        {
            uow.Repository<Memory>().Add(new Memory()
            {
                ActionName = foundMemory.ActionName,
                ActionValue = memory.ActionValue,
                AreaID = foundMemory.AreaID,
                Date = memory.MemoryMoment,
                Definition = memory.ToString(),
                ItemID = foundMemory.ItemID,
                CreatedBy = "USER",
                CreatedDate = DateTime.Now
            });

            uow.SaveChanges();
        }

        public void Dispose()
        {

        }

        public List<AdleMemoryObject> GetAllMemories()
        {
            using (var uow = GetContext)
            {
                var list = uow.Repository<Memory>().FindAll().Include("Area").Include("Item").ToList();
                List<AdleMemoryObject> data = new List<AdleMemoryObject>();

                foreach (var item in list)
                {
                    AdleMemoryObject memoryObject = new AdleMemoryObject();
                    memoryObject.Key = item.ID.ToString();
                    memoryObject.ActionName = item.ActionName;
                    memoryObject.ActionValue = item.ActionValue;
                    memoryObject.MemoryMoment = item.Date;
                    memoryObject.BasiclyShow = item.Definition;
                    data.Add(memoryObject);
                }

                return data;
            }
        }
    }
}
