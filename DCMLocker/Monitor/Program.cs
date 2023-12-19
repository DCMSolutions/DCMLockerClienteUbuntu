using DCMLocker.Monitor.Authentication;
using DCMLocker.Monitor.Cliente;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Monitor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // ----------- AUTENTICACION -----------------
            // Se desarrollo CustomAuthorizationStateProvider
            // Se desarrollo funciones.js
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<MOFAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(op => op.GetRequiredService<MOFAuthenticationStateProvider>());
            //---------------------------------------------


            builder.RootComponents.Add<App>("#app");
            
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<ConfigLocal>();
            builder.Services.AddScoped<DCMLocker.Monitor.Cliente.TLockerCliente>();
            builder.Services.AddScoped<DCMLocker.Monitor.Authentication.TAuthCliente>();
            await builder.Build().RunAsync();
        }
    }
}
