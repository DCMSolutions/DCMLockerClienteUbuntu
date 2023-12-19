using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace DCMLocker.Server.BaseController
{
    public class TLockerUser
    {
        /// <summary>
        /// Usuario
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Password despues del hash
        /// </summary>
        public string PassHash { get; set; }
        /// <summary>
        /// Indica si el usuario puede operar o no
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// Indica si el usuario esta bloqueado
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Box reservados para el usuario
        /// </summary>
        //public List<int> LockersReservados { get; set; }

        public static string GenerateNroConfirmation(string Path, string user)
        {
            try
            {
                string uhash = TLockerUser.GetPassHash(user);
                string Dir = $"DK{uhash}";
                string sDir = System.IO.Path.Combine(Path, Dir);
                if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);

                string fn = System.IO.Path.Combine(sDir, "ver" + uhash + ".confirmar");

                int r = (new Random()).Next(10000, 99999);
                TNroVerify registro = new TNroVerify() { Nro = r, Expiration = DateTime.Now.AddMinutes(10) };
                string reg = JsonSerializer.Serialize<TNroVerify>(registro);

                using (StreamWriter b = System.IO.File.CreateText(fn))
                {
                    b.Write(reg);
                }
                return r.ToString();
            }
            catch
            {
                throw new Exception("Error al generar Verificador");
            }
        }
        public static bool ConfirmNro(string Path,string user, string nro)
        {
            string uhash = TLockerUser.GetPassHash(user);
            string Dir = $"DK{uhash}";
            string sDir = System.IO.Path.Combine(Path, Dir);
            if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);

            string fn = System.IO.Path.Combine(sDir, "ver" + uhash + ".confirmar");

            string s = "";
            if (!File.Exists(fn)) return false;
            try
            {
                using (StreamReader b = File.OpenText(fn))
                {
                    s = b.ReadToEnd();
                }
                TNroVerify nroregistro = JsonSerializer.Deserialize<TNroVerify>(s);

                // Verifico condiciones

                if (nroregistro.Nro == Convert.ToInt32(nro))
                {
                    if (DateTime.Now <= nroregistro.Expiration) return true;
                }
            }
            catch { }

            return false;
        }


        public static string GetPassHash(string pass)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();
            ASCIIEncoding encoding = new();
            byte[] stream = null;
            StringBuilder sb = new();
            stream = md5.ComputeHash(encoding.GetBytes(pass));
            for (int i = 0; i < stream.Length; i++) sb.AppendFormat("{0:x2}", stream[i]);
            return sb.ToString();
        }

    }

    public class TLockerUserContent : ILockerStore
    {
        public const string FileName = "LoackerUser.user";
        public const string FileNamebkc = "BckLoackerUser";
        public Dictionary<string, TLockerUser> Users { get; set; }

        public void Save(string Path)
        {
            try
            {
                string dirbck = System.IO.Path.Combine(Path, "bck");

                if (!Directory.Exists(dirbck)) Directory.CreateDirectory(dirbck);

                string sf = System.IO.Path.Combine(Path, FileName);
                string sfb = System.IO.Path.Combine(dirbck, FileNamebkc + DateTime.Now.ToString("yyyyMMddHHmmss")+".bck");
                string s = JsonSerializer.Serialize<TLockerUserContent>(this);
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
        public static TLockerUserContent Create(string Path)
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
                    return JsonSerializer.Deserialize<TLockerUserContent>(s);
                }
                else
                {
                    TLockerUserContent r = new TLockerUserContent();
                    r.Users = new Dictionary<string, TLockerUser>();
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


    public class TNroVerify
    {
        public int Nro { get; set; }
        public DateTime Expiration { get; set; }
    }
   
}
