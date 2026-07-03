using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adle.Analysis
{
    public class Scenario
    {
        public string name { get; set; }

        public List<Sensor> sensors { get; set; }

        public Scenario()
        {
            sensors = new List<Sensor>();
        }

        public override string ToString()
        {
            string details = $"{name} - {sensors.Count} adım : (";
            foreach (var item in sensors)
            {
                details += $"{item.Name} -> ";
            }

            details = details.TrimEnd(new char[] { ' ', '-', '>', ' ' });
            details += ")";

            return details;
        }
    }
}
