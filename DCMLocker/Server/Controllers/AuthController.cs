using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using DCMLocker.Shared.MOFLogin;
using Microsoft.IdentityModel.Tokens;
using DCMLocker.Server.BaseController;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Net.Mail;
using System.Net;
using DCMLocker.Shared.Locker;

/*
 * MOFLocker
 * ID: 483331905486-mdm53l6fe27ddfosrok46jkkprf3mnrj.apps.googleusercontent.com
 * Secreto: sm4H9HkOKiqTieenNUWYdQLv
 */

namespace DCMLocker.Server.Controllers
{

    [Route("ClientApp/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        IConfiguration _Configuration;
        //DbUserContext _dcContext;
        private readonly TBaseLockerController _base;
        public AuthController(IConfiguration conf, TBaseLockerController Base)
        {
            _Configuration = conf;
            //_dcContext = dbcontext;
            _base = Base;
        }
        /// <summary>------------------------------------------------------------------------
        ///  LOGIN
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>--------------------------------------------------------------
        [HttpGet("login")]
        public TMOFAuthentication Login(string user, string pass)
        {
            try
            {
                List<TMOFRole> Roles;
                DateTime clienteexpire;
                string jwt = GenerarToken(Autenticar(user, pass, out Roles), Roles, out clienteexpire);
                Response.StatusCode = 200;
                return new TMOFAuthentication() { Token = jwt, ClienteExpire = clienteexpire };
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.Message);
                Response.StatusCode = 500;
                Response.WriteAsync("Usuario o password no son validos");
                return null;
            }

        }
        [HttpGet("GetUsersLocker")]
        public List<LockerUserPerfil> GetUsersLocker(string user, string pass)
        {
            try
            {
                Console.WriteLine(_base.PathBase);
                return new List<LockerUserPerfil>();
            }
            catch
            {
                return null;
            }

        }

        /// <summary>------------------------------------------------------------------------
        ///  CREATEUSER
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>--------------------------------------------------------------
        [HttpPost("CreateUser")]
        public async Task<ActionResult> CreateUser([FromBody] TMOFUserCreate data)
        {
            try
            {
                if (!data.user.Contains("@")) throw new Exception("usuario no es un mail");
                if (!data.user.Contains(".")) throw new Exception("usuario no es un mail");
                if (data.pass == data.confirmpass)
                {

                    if (!_base.LockerUser.Users.ContainsKey(data.user))
                    {
                        bool iseneble = !_base.Config.IsConfirmarEmail;

                        _base.LockerUser.Users.Add(data.user, new TLockerUser() { Enable = iseneble, User = data.user, PassHash = TLockerUser.GetPassHash(data.pass), IsLocked = false });
                        _base.LockerUser.Save(_base.PathBase);

                        if (_base.Config.IsConfirmarEmail)
                        {
                            //generamos el archivo testigo
                            try
                            {
                                string nro = TLockerUser.GenerateNroConfirmation(_base.PathBase, data.user);
                                //enviamos el Email
                                await SendEmail(data.user, nro);
                            }
                            catch { }
                        }

                        return Ok();
                    }
                }
            }
            catch
            {

            }
            return BadRequest();
        }
        

        /// <summary>------------------------------------------------------------------------
        /// Confirmacion de email
        /// </summary>
        /// <param name="Nro"></param>
        /// <returns></returns>--------------------------------------------------------------
        [HttpPost("Confirmar")]
        [Authorize]
        public ActionResult Confirmar([FromBody] string Nro)
        {
            if (TLockerUser.ConfirmNro(_base.PathBase, User.Identity.Name, Nro))
            {
                _base.LockerUser.Users[User.Identity.Name].Enable = true;
                _base.LockerUser.Save(_base.PathBase);
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
        /// <summary>---------------------------------------------------------------------
        /// 
        /// </summary>
        /// <returns></returns>-----------------------------------------------------------
        [HttpGet("SendConfirmation")]
        [Authorize]
        public async Task<ActionResult> SendConfirmation()
        {
            if (_base.Config.IsConfirmarEmail)
            {
                //generamos el archivo testigo
                try
                {
                    string nro = TLockerUser.GenerateNroConfirmation(_base.PathBase, User.Identity.Name);
                    //enviamos el Email
                    await SendEmail(User.Identity.Name, nro);
                    return Ok();
                }
                catch { }
            }
            return BadRequest();
        }




        #region PRIVADA %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        /// <summary>------------------------------------------------------------------------
        /// Autenticar
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="Roles"></param>
        /// <returns></returns>--------------------------------------------------------------
        private TMOFUser Autenticar(string user, string pass, out List<TMOFRole> Roles)
        {
            string filePath = Path.Combine(_base.PathBase, "adminHash.ans");
            string adminHash;
            if (!System.IO.File.Exists(filePath))
            {
                // Crear el archivo y escribir el contenido en él
                System.IO.File.WriteAllText(filePath, TLockerUser.GetPassHash("admin"));

                // Establecer el atributo de archivo oculto (opcional)
                System.IO.File.SetAttributes(filePath, System.IO.File.GetAttributes(filePath) | FileAttributes.Hidden);
                adminHash = TLockerUser.GetPassHash("admin");
            }
            else
            {
                adminHash = System.IO.File.ReadAllText(filePath);
            }
            Console.WriteLine(user);
            Console.WriteLine(adminHash);
            Console.WriteLine(TLockerUser.GetPassHash(pass));
            Console.WriteLine(TLockerUser.GetPassHash("anselmo"));

            if (user == "Admin" && TLockerUser.GetPassHash(pass) == adminHash)
            {
                TMOFUser u = new TMOFUser()
                {
                    Name = "Admin",
                    Id = Guid.NewGuid(),
                    Email = "Admin@dcm.com.ar",
                    Pass = "sssss",
                };
                Roles = new List<TMOFRole>() { new TMOFRole() { Id = 1, Dsc = "Admin", Name = "Admin" } };
                return u;
            }
            else
            {
                // busco en base de usuarios
                if (_base.LockerUser.Users.ContainsKey(user))
                {
                    if (_base.LockerUser.Users[user].PassHash == TLockerUser.GetPassHash(pass))
                    {
                        TMOFUser u = new TMOFUser()
                        {
                            Name = user,
                            Id = Guid.NewGuid(),
                            Email = user,
                            Pass = "****",
                        };
                        Roles = new List<TMOFRole>() { new TMOFRole() { Id = 2, Dsc = "User", Name = "User" } };
                        return u;
                    }
                    else throw new Exception("User no existe");
                }
                else throw new Exception("User no existe");
            }
        }
        /// <summary>------------------------------------------------------------------------
        ///  Generador de token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Roles"></param>
        /// <param name="clientexpire"></param>
        /// <returns></returns>--------------------------------------------------------------
        private string GenerarToken(TMOFUser user, List<TMOFRole> Roles, out DateTime clientexpire)
        {

            string tokenAsString = "";
            string key = _Configuration["AuthSettings:key"];


            // Creo los claim para el token
            int m = 2 + Roles.Count;
            Claim[] claims = new Claim[m];
            claims[0] = new Claim(ClaimTypes.Email, user.Email);
            claims[1] = new Claim(ClaimTypes.Name, user.Name);
            // Agregar  los roles
            for (int x = 0; x < Roles.Count; x++)
            {
                claims[x + 2] = new Claim(ClaimTypes.Role, Roles[x].Name);
            }


            IdentityModelEventSource.ShowPII = true;
            var keybuffer = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            DateTime dtexpiretime = DateTime.Now.AddDays(1);
            clientexpire = DateTime.Now.AddDays(1);
            var token = new JwtSecurityToken(
                issuer: _Configuration["AuthSettings:Issuer"],
                audience: _Configuration["AuthSettings:Audience"],
                claims, null, dtexpiretime,
                new SigningCredentials(keybuffer, SecurityAlgorithms.HmacSha256));

            tokenAsString = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenAsString;
        }

        /// <summary>-----------------------------------------------------------------------
        /// Genera un MD5
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>------------------------------------------------------------
        private static string GetMD5(string str)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();
            ASCIIEncoding encoding = new();
            byte[] stream = null;
            StringBuilder sb = new();
            stream = md5.ComputeHash(encoding.GetBytes(str));
            for (int i = 0; i < stream.Length; i++) sb.AppendFormat("{0:x2}", stream[i]);
            return sb.ToString();
        }



        private async Task<bool> SendEmail(string user, string Nro)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                {

                    mail.From = new MailAddress(_base.Config.SmtpServer.From);
                    mail.To.Add(user);
                    mail.Subject = _base.Config.SmtpServer.Asunto + " Confirmar email";
                    mail.Body = "Locker Código de verificación: " + Nro;

                    mail.IsBodyHtml = false;

                    SmtpClient smtp = new SmtpClient
                    {
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_base.Config.SmtpServer.UserName, _base.Config.SmtpServer.Password),
                        Host = _base.Config.SmtpServer.Host,
                        Port = _base.Config.SmtpServer.Port,
                        EnableSsl = _base.Config.SmtpServer.EnableSSL
                    };
                    await smtp.SendMailAsync(mail);

                }
                return true;
            }
            catch (Exception er)
            {
                Console.WriteLine("SEND CONFIRM EMAIL :" + er.Message);
            }
            return false;
        }
        #endregion %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    }
}
