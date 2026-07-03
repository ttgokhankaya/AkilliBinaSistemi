using SharedObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldModel
{
    public class HouseArea : AdleAreaBase
    {
        public HouseArea()
        {
        }

        public HouseArea(AdleAreaBase rootArea) : base(rootArea)
        {
        }

        public HouseArea(AdleAreaBase rootArea, string name) : base(rootArea, name)
        {
        }

        public override void RegisterItem(AdleItemBase item)
        {
            base.RegisterItem(item);
        }

        public override void RegisterToSubAreas(AdleAreaBase area)
        {
            base.RegisterToSubAreas(area);
        }
    }
}
