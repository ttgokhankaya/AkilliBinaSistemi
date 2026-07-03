using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adle.Analysis
{
    public class Sensor
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string IP { get; set; }
        public override string ToString()
        {
            return $"ID: {ID} - name:{Name} - type: {Type}";
        }
    }

    public enum SensorTypes
    {
        PIR = 0,
        weightSensor = 1,
        Light = 2
    }
}
