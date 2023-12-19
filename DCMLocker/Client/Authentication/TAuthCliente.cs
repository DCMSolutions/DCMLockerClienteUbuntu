using DCMLocker.Shared.MOFLogin;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DCMLocker.Client.Authentication
{
    public class TAuthCliente
    {
        private readonly HttpClient _cliente;
        private readonly MOFAuthenticationStateProvider _auth;
        private readonly NavigationManager _nav;
        public TAuthCliente(HttpClient http, MOFAuthenticationStateProvider Auth, NavigationManager Nav)
        {
            _cliente = http;
            _auth = Auth;
            _nav = Nav;
        }

        public async Task<bool> Login(string user, string pass)
        {
            try
            {
                string url = "/Auth/login?user=" + user.Replace("@", "%40") + "&pass=" + pass;
                var response = await _cliente.GetFromJsonAsync<TMOFAuthentication>(url);
                if (response.Error != null)
                {
                    //Error en login
                }
                else
                {
                    //Token y secreto
                    var token = response.Token;
                    Console.WriteLine(token);

                    await _auth.SetTokenAsync(token, response.ClienteExpire.ToString());
                    var data = await _auth.GetAuthenticationStateAsync();
                    Console.WriteLine(data.User.Identity.Name);
                    Console.WriteLine(data.User.Claims.ToList().Count);
                    return true;
                }
            }
            catch (Exception er)
            {
                //Console.WriteLine(er.Message);
            }
            return false;
        }
        public async Task<bool> CreateUser(string user, string pass, string confirmpass)
        {
            if (pass != confirmpass) return false;
            if (!user.Contains('@')) return false;
            if (!user.Contains('.')) return false;

            try
            {
                var response = await _cliente.PostAsJsonAsync<TMOFUserCreate>("/Auth/CreateUser", new TMOFUserCreate() { user = user, pass = pass, confirmpass = confirmpass });
                if (response.StatusCode== System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            return false;
        }

        public async Task<bool> Verificar(string Nro)
        {
            var token = await _auth.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _cliente.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            try
            {
                var r = await _cliente.PostAsJsonAsync<string>("/Auth/Confirmar", Nro);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else if ((r.StatusCode == System.Net.HttpStatusCode.Forbidden) ||
                   (r.StatusCode == System.Net.HttpStatusCode.Unauthorized))
                {
                    _nav.NavigateTo("/login");
                   
                }
                return false;
            }
            catch (HttpRequestException er)
            {
                Console.WriteLine("EPA" + er.Message + " " +er.StatusCode.ToString());
                if ((er.StatusCode == System.Net.HttpStatusCode.Forbidden) ||
                   (er.StatusCode == System.Net.HttpStatusCode.Unauthorized))
                {
                    Console.WriteLine("LOGIN");
                    _nav.NavigateTo("/login");
                    return false;

                }

            }
            catch(Exception er) { Console.WriteLine("ACA" + er.Message); }
            return false;
        }
        public async Task<bool> SendConfirmation()
        {
            var token = await _auth.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _cliente.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            try
            {
                var r = await _cliente.GetAsync("/Auth/SendConfirmation");
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else return false;
            }
            catch (HttpRequestException er)
            {
                Console.WriteLine(er.Message);
                if ((er.StatusCode == System.Net.HttpStatusCode.Forbidden) ||
                   (er.StatusCode == System.Net.HttpStatusCode.Unauthorized))
                {
                    _nav.NavigateTo("/login");
                    return false;

                }

            }
            catch (Exception er) { Console.WriteLine(er.Message); }
            return false;
        }
    }
}
