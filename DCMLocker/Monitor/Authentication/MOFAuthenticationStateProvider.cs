using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Monitor.Authentication
{
    public class MOFAuthenticationStateProvider : AuthenticationStateProvider
    {
        enum StorageType { token, expiry }
        string TokenGeneral = "";
        private readonly IJSRuntime jSRuntime;

        public MOFAuthenticationStateProvider(IJSRuntime runtime)
        {
            jSRuntime = runtime;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            Console.WriteLine($"Get de autenticacion");
            var token = await GetTokenAsync();
            Console.WriteLine($"Token {token}");
            var identity = string.IsNullOrEmpty(token) ? new ClaimsIdentity() :
                new ClaimsIdentity(ParseClaimsFromJWT(token), "JWT");
            AuthenticationState retorno = new AuthenticationState(new ClaimsPrincipal(identity));

            return retorno;
        }


        public async Task SetTokenAsync(string token, string expiry)
        {
            Console.WriteLine($"Set Token {token}");
            if (token == null)
            {
                var a = await jSRuntime.InvokeAsync<object>("RemoveData", StorageType.token.ToString());
                Console.WriteLine(a + " eliminado");
                var b = await jSRuntime.InvokeAsync<object>("RemoveData", StorageType.expiry.ToString());
                Console.WriteLine(b + " eliminado");
            }
            else
            {
                Console.WriteLine("token valido");
                TokenGeneral = token;
                var a = await jSRuntime.InvokeAsync<object>("SaveData", StorageType.token.ToString(), token);
                Console.WriteLine(a + " agregado");
                var b = await jSRuntime.InvokeAsync<object>("SaveData", StorageType.expiry.ToString(), expiry);
                Console.WriteLine(b + " agregado");

            }
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        public async Task<string> GetTokenAsync()
        {
            var expiry = await jSRuntime.InvokeAsync<string>("GetData", StorageType.expiry.ToString());
            Console.WriteLine($"expiry {expiry}");
            if (expiry != null)
            {
                if (DateTime.Parse(expiry.ToString()) > DateTime.Now)
                {
                    return await jSRuntime.InvokeAsync<string>("GetData", StorageType.token.ToString());

                }
                else
                {
                    await SetTokenAsync(null, null);
                }
            }
            return null;

        }

        private IEnumerable<Claim> ParseClaimsFromJWT(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64Withoutpadding(payload);
            var keyValuesPairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            List<Claim> result = new List<Claim>();
            Claim aux;
            foreach (var data in keyValuesPairs)
            {
                aux = new Claim(data.Key, data.Value.ToString());
                /*
                if (aux.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    string[] r = aux.Value.Replace('[', ' ').Replace(']', ' ').Replace('"', ' ').Trim().Split(',');
                    for (int x = 0; x < r.Count(); x++)
                    {
                        Claim y = new Claim(ClaimTypes.Role, r[x]);
                        result.Add(y);
                    }
                }
                else
                {
                    result.Add(aux);
                }
                */
                result.Add(aux);

            }
            return result;
        }

        private byte[] ParseBase64Withoutpadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

    }
}
