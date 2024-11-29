using Blazored.Modal;
using CurrieTechnologies.Razor.SweetAlert2;
using DCMLocker.Client.Authentication;
using DCMLocker.Client.Cliente;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Radzen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Client
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
            // Set culture to Spanish (Spain)
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-ES");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("es-ES");

            builder.RootComponents.Add<App>("#app");
            builder.Services.AddBlazoredModal();
            builder.Services.AddSingleton(s => new TLocker());
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<DCMLocker.Client.Cliente.TLockerCliente>();
            builder.Services.AddScoped<DCMLocker.Client.Authentication.TAuthCliente>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddSweetAlert2();
            await builder.Build().RunAsync();
        }
    }
}
