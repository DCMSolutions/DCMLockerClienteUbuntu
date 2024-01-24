using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using DCMLocker.Shared.Locker;

namespace DCMLocker.Server.BaseController
{


    public class TBaseLockerController
    {
        #region PROPERTIES---------------------------------------------------
        public TLockerConfig Config { get; set; }
        public TLockerUserContent LockerUser { get; set; }
        public TLockerMapContent LockerMap { get; set; }
        public TLockerBoxUsedContent LockerBox { get; set; }
        public Dictionary<int, List<string>> IndexBox { get; private set; }
        public string PathBase { get; private set; }
        public bool IsUpgrade { get; private set; }
        #endregion-----------------------------------------------------------


        #region GESTION DE USER BOX -----------------------------------------
        public bool IsBoxUser(int box, string user)
        {
            string fBox = $"box{box.ToString().PadLeft(3, '0')}.box";
            string sf = Path.Combine(PathBase, fBox);

            if (File.Exists(sf))
            {
                using (StreamReader b = File.OpenText(sf))
                {
                    while (!b.EndOfStream)
                    {
                        string s = b.ReadLine();
                        if (s == user) return true;
                    }
                }
            }
            return false;
        }
        public string[] UserFromBox(int box)
        {
            List<string> retorno = new List<string>();
            string fBox = $"box{box.ToString().PadLeft(3, '0')}.box";
            string sf = Path.Combine(PathBase, fBox);

            if (File.Exists(sf))
            {
                using (StreamReader b = File.OpenText(sf))
                {
                    while (!b.EndOfStream)
                    {
                        string s = b.ReadLine();
                        retorno.Add(s);
                    }
                }
            }
            return retorno.ToArray();
        }
        public void BoxToUser(int box, string user)
        {
            string fBox = $"box{box.ToString().PadLeft(3, '0')}.box";
            string sf = Path.Combine(PathBase, fBox);
            if (!IsBoxUser(box, user))
            {
                File.AppendAllLines(sf, new string[] { user });
                IndexBox[box].Add(user);
            }

        }
        public void BoxRemoveUser(int box, string user)
        {
            string fBox = $"box{box.ToString().PadLeft(3, '0')}.box";
            string sf = Path.Combine(PathBase, fBox);
            if (IndexBox.ContainsKey(box))
            {
                if (IndexBox[box].Contains(user))
                {
                    //Elimino al usuario
                    IndexBox[box].Remove(user);
                    if (IndexBox[box].Count > 0)
                    {
                        // Existen otros propietarios
                        using (StreamWriter b = File.CreateText(sf))
                        {
                            foreach (string s in IndexBox[box])
                            {
                                b.WriteLine(s);
                            }
                        }
                    }
                    else
                    {
                        // No tiene propietario lo elimino
                        File.Delete(sf);
                    }
                }
            }
        }
        public int[] GetUserBox(string user)
        {
            // Consulta desde el indice
            List<int> retorno = new List<int>();
            foreach (int box in IndexBox.Keys)
            {
                if (IndexBox[box].Contains(user))
                {
                    retorno.Add(box);
                }
            }
            return retorno.ToArray();
        }
        #endregion-----------------------------------------------------------
        #region GESTION DE USER BOX AUTOGESTIONABLES ------------------------
        public bool BoxSelfManagement_IsFree(int IdBox)
        {
            string fBox = $"box{IdBox.ToString().PadLeft(3, '0')}.box";
            string sf = Path.Combine(PathBase, fBox);
            return !File.Exists(sf);
        }
        public bool BoxSelfManagement_Reserve(int IdBox, string user)
        {
            string fBox = $"box{IdBox.ToString().PadLeft(3, '0')}.box";
            string sf = Path.Combine(PathBase, fBox);
            if (!File.Exists(sf))
            {
                File.AppendAllLines(sf, new string[] { user });
                IndexBox[IdBox].Add(user);
                return true;
            }
            else return false;


        }

        #endregion-----------------------------------------------------------
        #region TOKEN BOX ---------------------------------------------------
        public string GetTokenBox(int box, string user)
        {
            string uhash = TLockerUser.GetPassHash(user);
            string k = "";
            int r1 = new Random().Next(10000, 99999);
            if ((r1 % 2) == 0)
            {
                k = "4" + r1.ToString();
            }
            else k = "5" + r1.ToString();

            string Dir = $"DK{uhash}";
            string fBox = $"token{k}.key";
            string sDir = Path.Combine(PathBase, Dir);
            string sf = Path.Combine(sDir, fBox);
            // verifico que el usuario posee al box
            try
            {
                if (IsBoxUser(box, user))
                {
                    if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
                    using (StreamWriter b = File.CreateText(sf))
                    {
                        string d = JsonSerializer.Serialize(new BoxTokenKey()
                        {
                            Token = k,
                            Tag = "",
                            Box = box,
                            User = user,
                            DTExpiration = DateTime.Now.AddDays(365)
                        });
                        b.Write(d);
                    }
                    return k;

                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er);
            }
            return null;
        }
        public BoxTokenKey TokenBox(string Token)
        {
            bool iskeyok = false;
            try
            {
                bool IsMultiplo = false;

                int r1 = 0;
                int k = Convert.ToInt32(Token);
                if (k > 500000)
                {
                    r1 = k - 500000;
                    IsMultiplo = false;
                }
                else if (k > 400000)
                {
                    r1 = k - 400000;
                    IsMultiplo = true;
                }
                if ((r1 % 2) == 0) { if (IsMultiplo) iskeyok = true; }
                else { if (!IsMultiplo) iskeyok = true; }
            }

            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                throw new Exception("Token no valido");
            }
            if (iskeyok)
            {
                string fBox = $"token{Token}.key";
                string[] Dirs = Directory.GetDirectories(PathBase, "DK*");
                foreach (string dir in Dirs)
                {
                    string sf = Path.Combine(dir, fBox);
                    if (File.Exists(sf))
                    {
                        try
                        {
                            string s = File.ReadAllText(sf);
                            BoxTokenKey d = JsonSerializer.Deserialize<BoxTokenKey>(s);
                            if (DateTime.Now <= d.DTExpiration)
                            {
                                return d;
                            }
                            else throw new Exception("Token expiro");
                        }
                        catch (Exception er)
                        {
                            Console.WriteLine(er.Message);
                            throw new Exception("Error al abrir Token");
                        }
                        finally
                        {
                            File.Delete(sf);
                        }
                    }
                }
                throw new Exception("Token no valido");
            }
            else throw new Exception("Token no valido");
        }

        public BoxTokenKey[] GetTokenFromUser(string user)
        {
            List<BoxTokenKey> retorno = new List<BoxTokenKey>();
            string uhash = TLockerUser.GetPassHash(user);
            string Dir = $"DK{uhash}";
            string sDir = Path.Combine(PathBase, Dir);

            if (Directory.Exists(sDir))
            {
                string[] files = Directory.GetFiles(sDir, "*.key");
                foreach (string sf in files)
                {
                    string s = File.ReadAllText(sf);
                    BoxTokenKey d = JsonSerializer.Deserialize<BoxTokenKey>(s);
                    if (DateTime.Now <= d.DTExpiration) retorno.Add(d);
                    else File.Delete(sf);
                }
            }
            return retorno.ToArray();
        }
        #endregion ----------------------------------------------------------


        private void TBaseLockerControllerInit()
        {
            // Verifico la existencia del directorio base
            if (!Directory.Exists(PathBase)) Directory.CreateDirectory(PathBase);

            //levanto la configuracion general 
            Config = TLockerConfig.Create(PathBase);
            //levanto la configuracion o LOCKERMAP 
            LockerMap = TLockerMapContent.Create(PathBase);
            //levanto los usuarios
            LockerUser = TLockerUserContent.Create(PathBase);
            //levanto Indice Box - User
            UpgrateIndex();



        }
        private void UpgrateIndex()
        {
            IndexBox.Clear();
            //barro todos los Box
            for (int x = 0; x < 256; x++)
            {
                IndexBox.Add(x, new List<string>());
                string fBox = $"box{x.ToString().PadLeft(3, '0')}.box";
                string sf = Path.Combine(PathBase, fBox);
                if (File.Exists(sf))
                {
                    using (StreamReader b = File.OpenText(sf))
                    {
                        while (!b.EndOfStream)
                        {
                            string s = b.ReadLine();
                            if (!string.IsNullOrEmpty(s)) IndexBox[x].Add(s);
                        }
                    }
                }
            }

        }

        public TBaseLockerController(string Path)
        {
            IsUpgrade = false;
            PathBase = Path;
            IndexBox = new Dictionary<int, List<string>>();

        }
        public void Upgrade()
        {
            TBaseLockerControllerInit();
            IsUpgrade = true;
        }

        
    }
}
