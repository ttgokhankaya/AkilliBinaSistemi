using DataAccess;
using SimulationDB_Migrations;

namespace GUI_Simulation
{
    public class Utility
    {
        public static DataAccess.Repository.IUnitOfWork GetOuw()
        {
            return UnitOfWorkFactory.CreateBasicContext(new DB());
        }
    }
}
