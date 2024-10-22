using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DCMLocker.Shared.Locker;

namespace DCMLocker.Server.BaseController
{
    

    public class TLockerMapContent : ILockerStore
    {
        public const string FileName = "LoackerMap.map";
        public const string FileNamebkc = "BckLoackerMap";

        public Dictionary<int,TLockerMap> LockerMaps { get; set; }
        public void Save(string Path)
        {
            try
            {
                string dirbck = System.IO.Path.Combine(Path, "bck");

                if (!Directory.Exists(dirbck)) Directory.CreateDirectory(dirbck);

                string sf = System.IO.Path.Combine(Path, FileName);
                //string sfb = System.IO.Path.Combine(dirbck, FileNamebkc + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bck");
                string s = JsonSerializer.Serialize<TLockerMapContent>(this);
                //if (File.Exists(sf)) File.Move(sf, sfb);
                using (StreamWriter b = File.CreateText(sf))
                {
                    b.Write(s);
                }
            }
            catch
            {
                throw;
            }

        }
        public static TLockerMapContent Create(string Path)
        {
            string sf = System.IO.Path.Combine(Path, FileName);
            string sf2 = System.IO.Path.Combine("/home/pi", FileName);

            try
            {
                if (File.Exists(sf2))
                {
                    string s = "";
                    using (StreamReader b = File.OpenText(sf2))
                    {
                        s = b.ReadToEnd();
                    }
                    return JsonSerializer.Deserialize<TLockerMapContent>(s);
                }
                else if (File.Exists(sf))
                {
                    string s = "";
                    using (StreamReader b = File.OpenText(sf))
                    {
                        s = b.ReadToEnd();
                    }
                    return JsonSerializer.Deserialize<TLockerMapContent>(s);
                }
                else
                {
                    TLockerMapContent r = new TLockerMapContent();
                    r.LockerMaps = new Dictionary<int, TLockerMap>();
                    for (int x = 0; x < 256; x++)
                    {
                        r.LockerMaps.Add(x, new TLockerMap() { IdBox = x, Enable = false, IsUserFixed = false, IsSensorPresent = false, AlamrNro = 0, LockerType = TLockerMap.EnumLockerType.NORMAL, TempMin = 0, TempMax = 0, State="Libre",Size=0,IdFisico=null}); ;
                    }
                    r.Save(Path);
                    return r;
                }
            }
            catch(Exception er)
            {
                Console.WriteLine(er.Message);
                throw;
            }

        }
    }

}
