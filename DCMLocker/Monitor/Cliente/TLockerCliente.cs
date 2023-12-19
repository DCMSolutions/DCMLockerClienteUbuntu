using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using DCMLocker.Shared.Locker;
using DCMLocker.Monitor.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using DCMLocker.Shared;

namespace DCMLocker.Monitor.Cliente
{
    public class TLockerCliente
    {
        public event EventHandler OnChange;

        private HubConnection hubConnection;
        private readonly HttpClient _cliente;
        private readonly MOFAuthenticationStateProvider _auth;
        
        private readonly NavigationManager _nav;
        public TLockerCliente(HttpClient http, MOFAuthenticationStateProvider Auth,  NavigationManager Nav)
        {
            _cliente = http;
            _auth = Auth;
            _nav = Nav;
        }

        /// <summary>---------------------------------------------------------------------
        ///  Solicitud de estado actual
        /// </summary>
        /// <returns></returns>-----------------------------------------------------------
        public async Task<LockerCU[]> GetState()
        {
            var token = await _auth.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _cliente.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            try
            {
                return null;
            }
            catch (HttpRequestException er)
            {
                if ((er.StatusCode == System.Net.HttpStatusCode.Forbidden) ||
                   (er.StatusCode == System.Net.HttpStatusCode.Unauthorized))
                {
                    return null;
                }
                else throw;
            }
            catch
            {
                throw;
            }

        }

        
    }
}
