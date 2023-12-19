using DCMLocker.Server.Background;
using DCMLocker.Server.BaseController;
using DCMLocker.Server.Hubs;
using DCMLocker.Shared;
using DCMLocker.Shared.Locker;
using DCMLockerCommunication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DCMLocker.Server.Controllers
{

    [Route("ClientApp/[controller]")]
    [Route("KioskApp/[controller]")]
    [ApiController]
    public class LockerController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IDCMLockerController _driver;
        private readonly IHubContext<LockerHub, ILockerHub> _hubContext;
        private readonly TBaseLockerController _base;
        private readonly HttpClient _http;

        /// <summary> -----------------------------------------------------------------------
        /// Constructor
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="context2"></param>
        /// <param name="logger"></param>
        /// <param name="Base"></param>------------------------------------------------------
        public LockerController(IDCMLockerController driver, IHubContext<LockerHub, ILockerHub> context2, ILogger<LockerController> logger, TBaseLockerController Base, HttpClient http)
        {
            _driver = driver;
            _log = logger;
            _hubContext = context2;
            _base = Base;
            _http = http;
        }
        //public LockerController(ILogger<LockerController> logger)
        //{

        //    _log = logger;
        //}
        /// <summary> -----------------------------------------------------------------------
        ///  Retorna el estado actual del locker
        /// </summary>
        /// <returns></returns> -------------------------------------------------------------
        [HttpGet]
        //[Authorize(Roles = "Admin")]
        public IEnumerable<LockerCU> Get()
        {
            _hubContext.Clients.All.AddClientGrupoLocker();
            LockerCU[] s = new LockerCU[16];

            for (int x = 0; x < 16; x++)
            {
                CU status = _driver.GetCUState(x);
                s[x] = new LockerCU() { CU = x, Box = new LockerBox[16] };
                for (int y = 0; y < 16; y++)
                {
                    s[x].Box[y] = new LockerBox();
                    s[x].Box[y].Box = y;
                    s[x].Box[y].Reservado = false;
                    s[x].Box[y].Door = status.DoorStatus[y];
                    s[x].Box[y].Sensor = status.SensorStatus[y];
                    s[x].Box[y].Id = x * 16 + y;


                }
            }

            return s;
        }

        [HttpGet("id")]
        //[Authorize(Roles = "Admin")]
        public IEnumerable<LockerCU> GetById(int id)
        {
            _hubContext.Clients.All.AddClientGrupoLocker();
            var y = id % 16;
            var x = (id - y) / 16;

            LockerCU[] s = new LockerCU[1];

            CU status = _driver.GetCUState(x);
            s[0] = new LockerCU() { CU = x, Box = new LockerBox[1] };

            s[0].Box[0] = new LockerBox();
            s[0].Box[0].Box = y;
            s[0].Box[0].Reservado = false;
            s[0].Box[0].Door = status.DoorStatus[y];
            s[0].Box[0].Sensor = status.SensorStatus[y];
            s[0].Box[0].Id = x * 16 + y;




            return s;
        }

        [HttpPost("token")]
        //[Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> Test([FromBody] string Token)
        {
            try
            {
                // preguntar al servidor si abro algo con el token que recibo


                try
                {
                    int _CU;
                    int _Box;
                    Uri uri = new Uri(_base.Config.UrlServer, "api/locker");

                    ServerToken serverComunication = new();



                    serverComunication.NroSerie = _base.Config.LockerID;
                    serverComunication.Token = Token;
                    var response = await _http.PostAsJsonAsync(uri, serverComunication);

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var serverResponse = await response.Content.ReadFromJsonAsync<ServerToken>();


                            if (serverResponse.Locker != null)
                            {
                                //recibo el id de mentira y tengo que abrir un box
                                var _boxAddr = _base.LockerMap.LockerMaps.Where(b => b.Value.Id == Convert.ToInt32(serverResponse.Locker)).First().Value.BoxAddr;

                                _CU = _boxAddr / 16;
                                _Box = _boxAddr % 16;

                                _driver.SetBox(_CU, _Box);
                                return Ok(serverResponse.Locker);
                            }
                            else
                            {
                                return Ok(response.StatusCode);

                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            return Ok(response.StatusCode);
                        }

                        //await _chatHub.UpdateStatus("Connected");
                    }
                    else
                    {
                        // Handle non-successful status codes, e.g., response.StatusCode, response.ReasonPhrase, etc.
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                        return StatusCode((int)response.StatusCode);

                        //await _chatHub.UpdateStatus("Disonnected");

                    }

                }
                catch (HttpRequestException ex)
                {
                    // Maneja errores de solicitud HTTP (por ejemplo, problemas de red, servidor inaccesible, etc.)
                    Console.WriteLine("Error de solicitud HTTP: " + ex.Message);
                    return StatusCode(403);

                }
                catch (Exception ex)
                {
                    // Maneja otros errores no esperados
                    Console.WriteLine("Error inesperado: " + ex.Message);
                    return StatusCode(403);

                }


            }
            catch (Exception er)
            {
                return StatusCode(403);
            }
        }


        //Cosas de tamaños
        [HttpGet("tamaño")]
        public IActionResult GetDelTxtTamaños()
        {
            try
            {
                string sf = Path.Combine(_base.PathBase, "tamaños.ans");

                if (System.IO.File.Exists(sf))
                {
                    string content = System.IO.File.ReadAllText(sf);
                    return Ok(content);
                }
                else
                {
                    List<Tamaño> emptyList = new List<Tamaño>();
                    string emptyListJson = JsonSerializer.Serialize<List<Tamaño>>(emptyList);
                    return Ok(emptyListJson); // devolver lista vacia si el archivo no existe.
                }
            }
            catch
            {
                return StatusCode(500); // En caso de un error, devolver un código de estado 500 (Internal Server Error).
            }
        }

        [HttpPost("tamaño")]
        public bool AgregarTamaño([FromBody] Tamaño tamaño)
        {
            try
            {
                string sf = Path.Combine(_base.PathBase, "tamaños.ans");
                if (System.IO.File.Exists(sf))
                {
                    string content = System.IO.File.ReadAllText(sf);
                    List<Tamaño> listaDeTamaños = JsonSerializer.Deserialize<List<Tamaño>>(content);
                    if (listaDeTamaños.Count > 0)
                    {
                        tamaño.Id = listaDeTamaños.Max(t => t.Id) + 1;
                    }
                    else
                    {
                        tamaño.Id = 1;
                    }
                    listaDeTamaños.Add(tamaño);
                    listaDeTamaños = listaDeTamaños.OrderBy(x => (x.Alto * x.Ancho * x.Profundidad)).ToList();
                    string s = JsonSerializer.Serialize<List<Tamaño>>(listaDeTamaños);

                    using (StreamWriter b = System.IO.File.CreateText(sf))
                    {
                        b.Write(s);
                    }

                    return true;
                }
                else
                {
                    tamaño.Id = 1;
                    List<Tamaño> listaDeTamaños = new List<Tamaño> { tamaño };
                    string s = JsonSerializer.Serialize<List<Tamaño>>(listaDeTamaños);

                    using (StreamWriter b = System.IO.File.CreateText(sf))
                    {
                        b.Write(s);
                    }
                    return true;
                }
            }
            catch (Exception er)
            {
                throw;
            }
        }

        [HttpPost("listaTamaños")]
        public bool SubirTamaños([FromBody] List<Tamaño> listaDeTamaños)
        {
            try
            {
                string sf = Path.Combine(_base.PathBase, "tamaños.ans");

                listaDeTamaños = listaDeTamaños.OrderBy(x => (x.Alto * x.Ancho * x.Profundidad)).ToList();

                string s = JsonSerializer.Serialize<List<Tamaño>>(listaDeTamaños);

                using (StreamWriter b = System.IO.File.CreateText(sf))
                {
                    b.Write(s);
                }

                return true;
            }
            catch (Exception er)
            {
                throw;
            }
        }


        [HttpPost("editTamaño")]
        public bool EditTamaño([FromBody] Tamaño tamaño)
        {
            try
            {
                string sf = Path.Combine(_base.PathBase, "tamaños.ans");
                if (System.IO.File.Exists(sf))
                {
                    string content = System.IO.File.ReadAllText(sf);
                    List<Tamaño> listaDeTamaños = JsonSerializer.Deserialize<List<Tamaño>>(content);

                    listaDeTamaños = listaDeTamaños.Where(x => x.Id != tamaño.Id).ToList();
                    listaDeTamaños.Add(tamaño);

                    listaDeTamaños = listaDeTamaños.OrderBy(x => (x.Alto * x.Ancho * x.Profundidad)).ToList();
                    string s = JsonSerializer.Serialize<List<Tamaño>>(listaDeTamaños);

                    using (StreamWriter b = System.IO.File.CreateText(sf))
                    {
                        b.Write(s);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception er)
            {
                throw;
            }
        }

        [HttpPost("deletetamaño")]
        public bool DeleteTamaño([FromBody] Tamaño tamaño)
        {
            try
            {
                string sf = Path.Combine(_base.PathBase, "tamaños.ans");
                if (System.IO.File.Exists(sf))
                {
                    string content = System.IO.File.ReadAllText(sf);
                    List<Tamaño> listaDeTamaños = JsonSerializer.Deserialize<List<Tamaño>>(content);

                    listaDeTamaños = listaDeTamaños.Where(x => x.Id != tamaño.Id).ToList();

                    string s = JsonSerializer.Serialize<List<Tamaño>>(listaDeTamaños);

                    using (StreamWriter b = System.IO.File.CreateText(sf))
                    {
                        b.Write(s);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception er)
            {
                throw;
            }
        }

        /// <summary> -----------------------------------------------------------------------
        ///  Apertura de un Box, el mismo es verificado si el usuario lo posee como 
        ///  pertenencia
        /// </summary>
        /// <param name="Box"></param>-------------------------------------------------------
        [HttpPost("OpenBox")]
        //[Authorize(Roles = "Admin, User")]
        public ActionResult Set([FromBody] BoxAccessToken Box)
        {
            try
            {
                //Controlo que el usuario posee al Box
                if (User.IsInRole("Admin"))
                {
                    _driver.SetBox(Box.CU, Box.Box);
                    return Ok();
                }
                else
                {
                    int boxAddr = (Box.CU << 4) + Box.Box;
                    // verifico si el Box pertenece al usuario
                    if (!_base.LockerUser.Users[User.Identity.Name].IsLocked)
                    {
                        bool Pertenese = _base.IsBoxUser(boxAddr, User.Identity.Name);
                        if (Pertenese)
                        {
                            if (!_base.LockerMap.LockerMaps[boxAddr].IsUserFixed)
                            {
                                _base.BoxRemoveUser(boxAddr, User.Identity.Name);

                                DeleteToken(User.Identity.Name, _base.TokenBox(boxAddr.ToString()).ToString());



                            }
                            _driver.SetBox(Box.CU, Box.Box);


                            return Ok();
                        }
                        else return BadRequest("Caja no esta asignada al Usuario.");
                    }
                    else return BadRequest("Usuario bloqueado.");
                }
            }
            catch (Exception er)
            {
                return BadRequest($"Error: {er.Message}");
            }

        }
        [HttpGet("OpenBox")]
        //[Authorize(Roles = "Admin, User")]
        public ActionResult Open(int _CU, int _Box)
        {
            try
            {
                CU status = _driver.GetCUState(_CU);
                if (status.DoorStatus[_Box])
                {
                    _driver.SetBox(_CU, _Box);
                    return Ok("Locker abierto");
                }
                else
                {
                    return BadRequest("Locker no disponible para su apertura");
                }
            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }

        }
        /// <summary>-----------------------------------------------------------------------
        /// Retorna una llave para la apertura del Box
        /// </summary>
        /// <param name="BoxAddr"></param>
        /// <returns></returns>-------------------------------------------------------------
        [HttpGet("GenerateTokenKey")]
        //[Authorize(Roles = "User")]
        public string[] GenearateTokenKey(int BoxAddr)
        {
            string Token = _base.GetTokenBox(BoxAddr, "a@.");
            //string Token = _base.GetTokenBox(BoxAddr, User.Identity.Name);
            Console.WriteLine($"El token es {Token}");
            return new string[] { Token };
        }

        [HttpGet("GetMyTokens")]
        [Authorize(Roles = "User")]
        public TokenAccessBox[] GetMyTokens()
        {
            List<TokenAccessBox> retorno = new List<TokenAccessBox>();
            BoxTokenKey[] bt = _base.GetTokenFromUser(User.Identity.Name);
            foreach (BoxTokenKey t in bt)
            {
                retorno.Add(new TokenAccessBox()
                {
                    User = User.Identity.Name,
                    DTExpiration = t.DTExpiration,
                    Box = t.Box,
                    Token = t.Token,
                    Tag = t.Tag,
                    IsBoxTemporal = !_base.LockerMap.LockerMaps[t.Box].IsUserFixed
                });
            }
            return retorno.ToArray();
        }

        /// <summary>------------------------------------------------------------------------
        /// 
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>--------------------------------------------------------------
        [HttpPost("TokenKey")]
        public int TokenKey([FromBody] string Token)
        {

            try
            {
                BoxTokenKey AccessToken = _base.TokenBox(Token);
                int cu = (AccessToken.Box & 0xf0) >> 4;
                int box = AccessToken.Box & 0x0f;

                if (_base.LockerMap.LockerMaps[AccessToken.Box].State == "Esperando abrir")
                {
                    _base.LockerMap.LockerMaps[AccessToken.Box].State = "Entregado";
                }
                if (_base.LockerMap.LockerMaps[AccessToken.Box].State == "Esperando retiro")
                {

                    _base.LockerMap.LockerMaps[AccessToken.Box].State = "Libre";
                    _base.BoxRemoveUser(AccessToken.Box, AccessToken.User);
                    DeleteToken(User.Identity.Name, Token);

                }



                _base.Config.Save(_base.PathBase);
                _driver.SetBox(cu, box);
                return AccessToken.Box;
            }
            catch (Exception er)
            {
                Console.WriteLine("[TOKENKEY] " + er.Message);
                throw;
            }
        }

        [HttpGet("ChangeAdminPassword")]
        [Authorize(Roles = "Admin")]
        public bool ChangeAdminPassword(string newPass, string oldPass)
        {

            Console.WriteLine("-----------------------------------------------------------");
            Console.WriteLine(newPass);
            try
            {
                string filePath = Path.Combine(_base.PathBase, "adminHash.ans");
                System.IO.File.WriteAllText(filePath, TLockerUser.GetPassHash(newPass));
                return true;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        /// <summary> -----------------------------------------------------------------------
        ///  Retorna todos los Box que le pertenecen a un usuario
        /// </summary>
        /// <returns></returns> -------------------------------------------------------------
        [HttpGet("GetMyPerfil")]
        [Authorize(Roles = "Admin, User")]
        public LockerUserPerfil GetMyPerfil()
        {
            if (User.IsInRole("Admin"))
            {
                LockerUserPerfil retorno = new LockerUserPerfil() { Enable = true, Boxses = new int[0] };
                return retorno;
            }

            if (_base.LockerUser.Users.ContainsKey(User.Identity.Name))
            {
                LockerUserPerfil retorno = new LockerUserPerfil();
                retorno.Boxses = _base.GetUserBox(User.Identity.Name);
                retorno.IsLocked = _base.LockerUser.Users[User.Identity.Name].IsLocked;
                retorno.Enable = _base.LockerUser.Users[User.Identity.Name].Enable;
                return retorno;
            }
            return null;
        }

        [HttpGet("GetBoxSelfManagement")]
        [Authorize(Roles = "User")]
        public int[] GetBoxSelfManagement()
        {
            List<int> retorno = new List<int>();
            // Busco la canfiguracion de los BOX
            var r = from p in _base.LockerMap.LockerMaps where p.Value.Enable == true && p.Value.IsUserFixed == false select p.Key;
            // Verifico si estan sindo utilizados o no
            foreach (int box in r)
            {
                if (_base.BoxSelfManagement_IsFree(box))
                {
                    retorno.Add(box);
                }
            }
            return retorno.ToArray();
        }
        //[HttpPost("BoxSelfManagementReserve")]
        //[Authorize(Roles = "User")]
        //public ActionResult BoxSelfManagementReserve([FromBody] int boxadrr)
        //{
        //    _base.LockerMap.LockerMaps[boxadrr].State = "Asignado";
        //    bool x = _base.BoxSelfManagement_Reserve(boxadrr, User.Identity.Name);
        //    _base.LockerMap.Save(_base.PathBase);


        //    if (x)
        //    {
        //        //_driver.SetBox(((boxadrr & 0xf0) >> 4), boxadrr & 0x0f);
        //        return Ok();
        //    }

        //    else return BadRequest("No se pudo reservar");
        //}
        [HttpGet("BoxSelfManagementReserve")]
        //[Authorize(Roles = "User")]
        public ActionResult BoxSelfManagementReserve(int boxadrr, string user)
        {
            _base.LockerMap.LockerMaps[boxadrr].State = "Asignado";
            bool x = _base.BoxSelfManagement_Reserve(boxadrr, user);
            _base.LockerMap.Save(_base.PathBase);


            if (x)
            {
                //_driver.SetBox(((boxadrr & 0xf0) >> 4), boxadrr & 0x0f);
                return Ok();
            }

            else return BadRequest("No se pudo reservar");
        }


        /// <summary> -----------------------------------------------------------------------
        ///  Retorna todos los Box que le pertenecen a un usuario
        /// </summary>
        /// <returns></returns> -------------------------------------------------------------
        [HttpGet("GetPerfilFromUser")]
        //[Authorize(Roles = "Admin")]
        public LockerUserPerfil GetPerfilFromUser(string iduser)
        {
            string user = iduser.Replace("%40", "@").Replace("%_", ".");
            if (_base.LockerUser.Users.ContainsKey(user))
            {
                LockerUserPerfil retorno = new LockerUserPerfil();
                retorno.Boxses = _base.GetUserBox(user);
                retorno.IsLocked = _base.LockerUser.Users[user].IsLocked;
                retorno.Enable = _base.LockerUser.Users[user].Enable;

                return retorno;
            }

            return null;
        }
        /// <summary>--------------------------------------------------------------
        /// Retorna los usuarios de un box
        /// </summary>
        /// <param name="boxaddr"></param>
        /// <returns></returns>----------------------------------------------------
        [HttpGet("GetUserFromBox")]
        [Authorize(Roles = "Admin")]
        public string[] GetUserFromBox(int boxaddr)
        {
            return _base.UserFromBox(boxaddr);
        }
        /// <summary>------------------------------------------------------------------------
        /// Buscador de usuarios
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>--------------------------------------------------------------
        [HttpGet("Search")]
        [Authorize(Roles = "Admin")]
        public string[] Search(string keyword)
        {
            var r = from p in _base.LockerUser.Users where p.Key.StartsWith(keyword) select p.Key;
            return r.ToArray();
        }
        [HttpGet("GetAllUsers")]
        //[Authorize(Roles = "Admin")]
        public List<TLockerUser> GetAllUsers(string keyword)
        {
            //var r = from p in _base.LockerUser.Users where p.Key.StartsWith(keyword) select p.Key;
            return _base.LockerUser.Users.Values.ToList();
        }



        #region CONFIGURACION %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        /// <summary>---------------------------------------------------------------------
        /// 
        /// </summary>
        /// <returns></returns>-----------------------------------------------------------
        [HttpGet("GetLockerConfig")]
        //[Authorize(Roles = "Admin")]
        public LockerConfig GetLockerConfig()
        {
            LockerConfig retorno = _base.Config;
            return retorno;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("SetLockerConfig")]
        [Authorize(Roles = "Admin")]
        public ActionResult SetLockerConfig([FromBody] LockerConfig data)
        {
            try
            {
                _base.Config.LockerID = data.LockerID;
                _base.Config.LockerMode = data.LockerMode;
                _base.Config.LockerType = data.LockerType;
                _base.Config.IsConfirmarEmail = data.IsConfirmarEmail;
                _base.Config.SmtpServer.Asunto = data.SmtpServer.Asunto;
                _base.Config.SmtpServer.From = data.SmtpServer.From;
                _base.Config.SmtpServer.Host = data.SmtpServer.Host;
                _base.Config.SmtpServer.Port = data.SmtpServer.Port;
                _base.Config.SmtpServer.UserName = data.SmtpServer.UserName;
                _base.Config.SmtpServer.Password = data.SmtpServer.Password;
                _base.Config.SmtpServer.EnableSSL = data.SmtpServer.EnableSSL;
                _base.Config.UrlServer = data.UrlServer;
                _base.Config.Save(_base.PathBase);
                return Ok();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return BadRequest();
        }

        /// <summary>--------------------------------------------------------------------
        ///  GetBoxConfig: Retorna la configuracion del Box
        /// </summary>
        /// <param name="BoxAddr"></param>
        /// <returns></returns>----------------------------------------------------------
        [HttpGet("GetBoxConfig")]
        public TLockerMap GetBoxConfig(int BoxAddr)
        {
            if (_base.LockerMap.LockerMaps.ContainsKey(BoxAddr))
            {
                return _base.LockerMap.LockerMaps[BoxAddr];
            }
            return null;
        }
        [HttpGet("GetBoxConfigPorId")]
        public TLockerMap GetBoxConfigPorId(int idBox)
        {
            TLockerMap lockerVacio = new();
            if (idBox == 0) return lockerVacio;
            foreach (var lockerMap in _base.LockerMap.LockerMaps.Values)
            {
                if (lockerMap.Id == idBox)
                {
                    return lockerMap;
                }
            }
            return lockerVacio;
        }

        [HttpGet("GetAllBoxConfig")]
        public List<TLockerMap> GetAllBoxConfig()
        {
            return _base.LockerMap.LockerMaps.Values.Where(x => x.Id != 0).OrderBy(x => x.Id).ToList();
        }



        /// <summary>----------------------------------------------------------------------
        /// SetBoxConfig: Modifica la configuracion del Box
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>------------------------------------------------------------
        [HttpPost("SetBoxConfig")]
        public ActionResult SetBoxConfig([FromBody] TLockerMap data)
        {
            if (_base.LockerMap.LockerMaps.ContainsKey(data.BoxAddr))
            {
                _base.LockerMap.LockerMaps[data.BoxAddr].Enable = data.Enable;
                _base.LockerMap.LockerMaps[data.BoxAddr].AlamrNro = data.AlamrNro;
                _base.LockerMap.LockerMaps[data.BoxAddr].IsSensorPresent = data.IsSensorPresent;
                _base.LockerMap.LockerMaps[data.BoxAddr].IsUserFixed = data.IsUserFixed;
                _base.LockerMap.LockerMaps[data.BoxAddr].LockerType = data.LockerType;
                _base.LockerMap.LockerMaps[data.BoxAddr].TempMax = data.TempMax;
                _base.LockerMap.LockerMaps[data.BoxAddr].TempMin = data.TempMin;
                _base.LockerMap.LockerMaps[data.BoxAddr].State = data.State;
                _base.LockerMap.LockerMaps[data.BoxAddr].Size = data.Size;
                _base.LockerMap.LockerMaps[data.BoxAddr].Id = data.Id;
                _base.LockerMap.Save(_base.PathBase);
                return Ok();
            }
            return BadRequest();
        }
        /// <summary>----------------------------------------------------------------------
        /// DeleteBoxConfig: Elimina la configuracion del Box
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>------------------------------------------------------------
        [HttpPost("DeleteBoxConfig")]
        public ActionResult DeleteBoxConfig([FromBody] TLockerMap data)
        {
            if (_base.LockerMap.LockerMaps.ContainsKey(data.BoxAddr))
            {
                _base.LockerMap.LockerMaps[data.BoxAddr].Enable = false;
                _base.LockerMap.LockerMaps[data.BoxAddr].AlamrNro = data.AlamrNro;
                _base.LockerMap.LockerMaps[data.BoxAddr].IsSensorPresent = data.IsSensorPresent;
                _base.LockerMap.LockerMaps[data.BoxAddr].IsUserFixed = data.IsUserFixed;
                _base.LockerMap.LockerMaps[data.BoxAddr].LockerType = data.LockerType;
                _base.LockerMap.LockerMaps[data.BoxAddr].TempMax = data.TempMax;
                _base.LockerMap.LockerMaps[data.BoxAddr].TempMin = data.TempMin;
                _base.LockerMap.LockerMaps[data.BoxAddr].State = data.State;
                _base.LockerMap.LockerMaps[data.BoxAddr].Size = "";
                _base.LockerMap.LockerMaps[data.BoxAddr].Id = 0;
                _base.LockerMap.Save(_base.PathBase);
                return Ok();
            }
            return BadRequest();
        }
        /// <summary>-----------------------------------------------------------------------
        /// Asigna Usuario a una Caja
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>-------------------------------------------------------------
        [HttpPost("BoxToUser")]
        [Authorize(Roles = "Admin")]
        public ActionResult BoxToUser([FromBody] LockerUserBox data)
        {
            try
            {
                _base.LockerMap.LockerMaps[data.Box].State = "Asignado";

                _base.BoxToUser(data.Box, data.User);
                return Ok();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return BadRequest();
        }
        /// <summary>----------------------------------------------------------------------
        /// Desasigna una caja del usuario
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>------------------------------------------------------------
        [HttpPost("BoxRemoveUser")]
        [Authorize(Roles = "Admin")]
        public ActionResult BoxRemoveUser([FromBody] LockerUserBox data)
        {
            try
            {

                if (_base.GetTokenFromUser(data.User).Where(x => x.Box == data.Box).FirstOrDefault() != null)
                {
                    string token = _base.GetTokenFromUser(data.User).Where(x => x.Box == data.Box).FirstOrDefault().Token;
                    DeleteToken(User.Identity.Name, token);
                }
                _base.BoxRemoveUser(data.Box, data.User);
                return Ok();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return BadRequest();
        }
        /// <summary>----------------------------------------------------------------------
        /// Bloquea o desbloquea a un usuario
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>------------------------------------------------------------
        [HttpPost("SetUserLockstate")]
        [Authorize(Roles = "Admin")]
        public ActionResult SetUserLockstate([FromBody] string user)
        {
            try
            {
                if (_base.LockerUser.Users.ContainsKey(user))
                {
                    _base.LockerUser.Users[user].IsLocked = !_base.LockerUser.Users[user].IsLocked;
                    _base.LockerUser.Save(_base.PathBase);
                    return Ok();
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return BadRequest("No se pudo cambiar el estado del usuario.");
        }

        /// <summary>----------------------------------------------------------------------
        /// Retorna las cajas con configuracion Fijas
        /// </summary>
        /// <returns></returns>------------------------------------------------------------
        [HttpGet("GetBoxesFixedState")]
        [Authorize(Roles = "Admin")]
        public BoxState[] GetBoxesFixedState()
        {
            List<BoxState> lista = new List<BoxState>();
            try
            {
                foreach (int box in _base.LockerMap.LockerMaps.Keys)
                {
                    //Busco todas las cajas fijas
                    if (_base.LockerMap.LockerMaps[box].Enable)
                    {
                        lista.Add(new BoxState() { Box = _base.LockerMap.LockerMaps[box].BoxAddr, IsAssigned = false, IsFixed = true });
                    }
                    //Verifico si estan ocupadas mediante el indice

                }
                foreach (BoxState s in lista)
                {
                    if (_base.IndexBox.ContainsKey(s.Box))
                    {
                        s.IsAssigned = _base.IndexBox[s.Box].Count > 0;
                    }
                }
                return lista.ToArray();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return new BoxState[0];
        }

        [HttpGet("GetIdBoxAddrList")]
        public List<int> GetIdBoxAddrList()
        {
            List<int> listaLibres = new List<int>();

            try
            {
                for (var i = 1; i < _base.LockerMap.LockerMaps.Count; i++)
                {
                    listaLibres.Add(i);
                }
                foreach (var locker in _base.LockerMap.LockerMaps.Values)
                {
                    if (locker.Id != 0)
                    {
                        listaLibres.Remove(locker.BoxAddr);
                    }
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return listaLibres;
        }




        #endregion %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        [HttpPost("DeleteToken")]
        //[Authorize(Roles = "Admin")]
        public ActionResult deletetoken([FromBody] string[] data)
        {
            try
            {
                string token = data[0];
                string user = data[1];

                DeleteToken(user, token);


            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return Ok();
        }
        private bool DeleteToken(string user, string token)
        {

            try
            {
                BoxTokenKey AccessToken = _base.TokenBox(token);

                if (_base.LockerMap.LockerMaps[AccessToken.Box].State == "Esperando abrir")
                {
                    _base.LockerMap.LockerMaps[AccessToken.Box].State = "Asignado";
                }
                if (_base.LockerMap.LockerMaps[AccessToken.Box].State == "Esperando retiro")
                {
                    _base.LockerMap.LockerMaps[AccessToken.Box].State = "Entregado";
                }

                _base.Config.Save(_base.PathBase);
                string uhash = TLockerUser.GetPassHash(user);
                string fBox = $"token{token}.key";
                string Dir = Path.Combine(_base.PathBase, $"DK{uhash}", fBox);
                System.IO.File.Delete(Dir);
                return true;



            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return false;
            }
        }


    }
}
