using SharedObject;
using System;
using System.Linq;
using System.Reflection;

namespace ActionModel
{
    public class Adaptor<T> : IAdaptor<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="param">Param0 => name, Param1 => method name, Param2 => ipV4</param>
        public void Execute(params object[] param)
        {
            string className = string.Empty;
            string method = string.Empty;
            string ip = string.Empty;
            object obje = null;
            MethodInfo mInfo = null;

            GetParams(param, out className, out method, out ip);
            CreateAssembly(className, method, out obje, out mInfo);
            object[] newParam = SetParams(param, ip, 3);

            mInfo.Invoke(obje, newParam);
        }

        public T GetValue(params object[] param)
        {
            string name = string.Empty;
            string method = string.Empty;
            string ip = string.Empty;
            object obje = null;
            MethodInfo mInfo = null;

            GetParams(param, out name, out method, out ip);
            CreateAssembly(name, method, out obje, out mInfo);
            object[] newParam = SetParams(param, ip, 2);

            T value = (T)mInfo.Invoke(obje, newParam);
            return value;
        }

        private object[] SetParams(object[] param, string ip, int offset)
        {
            object[] newParam;
            if (param.Length > offset)
            {
                newParam = new object[param.Length - offset];
                for (int i = 0; i < param.Length; i++)
                {
                    if (i + offset == param.Length)
                        break;

                    newParam[i] = param[i + offset];
                }
            }
            else
            {
                newParam = new object[1];
                newParam[0] = ip;
            }

            return newParam;
        }

        private void GetParams(object[] param, out string name, out string method, out string ip)
        {
            try
            {
                name = param[0].ToString();
                if (string.IsNullOrEmpty(name))
                    throw new Exception();//TODO: Throw Exception ip verilmemiş

                //TODO=> manifestten isim karşılığını al
                method = param[1].ToString();
                if (string.IsNullOrEmpty(method))
                    throw new Exception();//TODO: Throw Exception method verilmemiş

                //TODO=> manifestten method karşılığını al
                ip = param[2].ToString();
                if (string.IsNullOrEmpty(ip))
                    throw new Exception();//TODO: Throw Exception ip verilmemiş
            }
            catch (Exception ex)
            {
                //TODO: Throw Exception
                throw ex;
            }
        }

        private void CreateAssembly(string name, string method, out object obje, out MethodInfo mInfo)
        {
            //TODO=> manifestten çek veya belli bir dizindeki bütün dll'lerden çekerek yapılabilir, path manifestten çekilir.
            // Assembly.Load (LoadFile değil): zaten yüklü assembly'yi döndürür, böylece
            // Device.Devices gibi statik registry'ler çağıran tarafla aynı kopyayı paylaşır.
            Assembly asm = Assembly.Load("SimulationObjects");

            if (asm == null)
                throw new Exception(); //TODO: Throw Exception assebly bulunamadı

            var type = asm.GetTypes().Where(x => x.Name == name).FirstOrDefault();
            if (type == null)
                throw new Exception(); //TODO: Throw Exception cihaz bulunamadı

            obje = Activator.CreateInstance(type);
            if (obje == null)
                throw new Exception(); //TODO: Throw Exception 

            mInfo = type.GetMethod(method);
            if (mInfo == null)
                throw new Exception(); //TODO: Throw Exception method bulunamadı
        }
    }
}
