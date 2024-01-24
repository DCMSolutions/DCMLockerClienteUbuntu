using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Server.BaseController
{
    public class TLockerBoxUsed
    {
        public int IdBox { get; set; }
        public string User { get; set; }
        public DateTime DTCreate { get; set; }
    }

    public class TLockerBoxUsedContent : ILockerStore
    {
        public const string FileName = "LoackerBoxUsed.map";
        public const string FileNamebkc = "BckLoackerBoxUsed";

        public Dictionary<int, TLockerBoxUsed> BoxUsed { get; set; }
        public void Save(string Path)
        {
            try
            {
                string dirbck = System.IO.Path.Combine(Path, "bck");

                if (!Directory.Exists(dirbck)) Directory.CreateDirectory(dirbck);

                string sf = System.IO.Path.Combine(Path, FileName);
                string sfb = System.IO.Path.Combine(dirbck, FileNamebkc + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bck");
                string s = JsonSerializer.Serialize<TLockerBoxUsedContent>(this);
                if (File.Exists(sf)) File.Move(sf, sfb);

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
        public static TLockerBoxUsedContent Create(string Path)
        {
            string sf = System.IO.Path.Combine(Path, FileName);
            try
            {
                if (File.Exists(sf))
                {
                    string s = "";
                    using (StreamReader b = File.OpenText(sf))
                    {
                        s = b.ReadToEnd();
                    }
                    return JsonSerializer.Deserialize<TLockerBoxUsedContent>(s);
                }
                else
                {
                    TLockerBoxUsedContent r = new TLockerBoxUsedContent();
                    r.BoxUsed = new Dictionary<int, TLockerBoxUsed>();
                    r.BoxUsed.Add(15, new TLockerBoxUsed() { IdBox=15, User="INVITADO", DTCreate= DateTime.Now });
                    r.Save(Path);
                    return r;
                }
            }
            catch
            {
                throw;
            }

        }
    }
}
