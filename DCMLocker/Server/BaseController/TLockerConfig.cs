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
                string parentDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".."));
                string sf2 = System.IO.Path.Combine(parentDir, FileName);

                //string sf = System.IO.Path.Combine(Path, FileName);
                string s = JsonSerializer.Serialize<LockerConfig>(this);

                using (StreamWriter b = File.CreateText(sf2))
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
            string parentDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".."));
            string sf2 = System.IO.Path.Combine(parentDir, FileName);
            try
            {
                if (File.Exists(sf2))
                {
                    string s = "";
                    using (StreamReader b = File.OpenText(sf2))
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
                    r.LockerID = DateTime.Now.ToString();
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
