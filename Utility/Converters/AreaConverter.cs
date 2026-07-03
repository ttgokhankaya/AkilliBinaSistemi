using DomainObjects;
using SharedObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Converters
{
    public class AreaConverter : IAdleConverter<AdleAreaBase, Area>
    {
        public AdleAreaBase DomainObjectToAdleObject(Area Obj, bool addSubs = true, AdleSCUBase SCU = null)
        {
            if (Obj == null)
            {
                throw new ArgumentNullException("AdleObject");
            }

            Area obje = Obj;

            AdleAreaBase area = null;

            if (obje.AreaType == null)
                area = new AdleAreaBase();
            else
            {

                Assembly asm = Assembly.Load("FieldModel");
                if (asm == null)
                    throw new Exception(); //TODO: Throw Exception

                var type = asm.GetTypes().Where(x => x.Name == obje.AreaType.Name).FirstOrDefault();
                if (type == null)
                    throw new Exception(); //TODO: Throw Exception 

                area = (AdleAreaBase)Activator.CreateInstance(type);
            }

            area.Name = obje.Name;
            area.Width = obje.Width;
            area.Height = obje.Height;
            area.ID = obje.ID;

            if (addSubs)
            {
                if (obje.RootArea != null)
                    area.RootArea = DomainObjectToAdleObject(obje.RootArea, false);

                if (obje.Items != null || obje.Items.Count > 0)
                {
                    foreach (var item in obje.Items)
                    {
                        //area.Items.Add(new ItemConverter. (item));
                        //area.Items.Add(new ItemConverter().DomainObjectToAdleObject(item, false));
                    }
                }

                if (obje.SubAreas != null && obje.SubAreas?.Count > 0)
                {
                    foreach (var item in obje.SubAreas)
                    {
                        area.SubAreas.Add(DomainObjectToAdleObject(item, false));
                    }
                }
            }

            area.Manager = SCU;

            return area;
        }

        public Area AdleObjectToDomainObject(AdleAreaBase AdleObject, bool addSubs = true)
        {
            if (AdleObject == null)
            {
                throw new ArgumentNullException("AdleObject");
            }

            AdleAreaBase obje = AdleObject;

            Area area = new Area();

            //TODO:Config
            //using (var uow = SCU.GetContextMember())
            //{
            //    string name = obje.GetType().Name;
            //    var Type = uow.Repository<AreaType>().Find(x => x.Name == name).FirstOrDefault();
            //    area.AreaTypeID = Type.ID;
            //}

            area.Name = obje.Name;
            area.Width = obje.Width;
            area.Height = obje.Height;

            if (addSubs)
            {
                if (obje.RootArea != null)
                {
                    //area.RootArea = ConvertBack(obje.RootArea);
                    area.AreaID = obje.RootArea.ID;
                }

                //if (obje.Items != null || obje.Items.Count > 0)
                //{
                //    foreach (var item in obje.Items)
                //    {
                //       area.Items.Add(new ItemConverter().AdleObjectToDomainObject(item, false));
                //    }
                //}

                //if (obje.SubAreas != null || obje.SubAreas?.Count > 0)
                //{
                //    foreach (var item in obje.SubAreas)
                //    {
                //        area.SubAreas.Add(AdleObjectToDomainObject(item));
                //        area.
                //    }
                //}
            }

            return area;
        }
    }
}
