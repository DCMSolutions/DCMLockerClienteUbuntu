using DCMLocker.Shared.Locker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Server.BaseController
{
    public class TLockerConfig : LockerConfig
    {
        public const string FileName = "LoackerConfig.config";
        public void Save(string Path)
        {
            try
            {
                string sf = System.IO.Path.Combine(Path, FileName);
                string s = JsonSerializer.Serialize<LockerConfig>(this);
             
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
        public static TLockerConfig Create(string Path)
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
                    return JsonSerializer.Deserialize<TLockerConfig>(s);
                }
                else
                {
                    TLockerConfig r = new TLockerConfig();
                    r.SmtpServer = new LockerEmail()
                    {
                        EnableSSL = false,
                        Host = "www.hotmail.com",
                        Port = 25,
                        UserName = "temp",
                        Password = "temp"
                    };
                    r.LockerID = "";
                    r.IsConfirmarEmail = false;
                    r.LockerMode = 0;
                    r.LockerType = 0;
                    r.UrlServer = new Uri("https://testing.server.dcm.com.ar/");
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
