using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.DependencyInjection;
using DCMLocker.Server.Background;
using DCMLocker.Server.Hubs;
using DCMLocker.Server.Controllers;

namespace DCMLocker.Server
{
    public class Program
    {
        

        public static void Main(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSystemd()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseConsoleLifetime() // Enable Ctrl + C handling
                .ConfigureServices(services=> 
                {

                    services.AddHostedService<DCMLockerController>();

                    services.AddHostedService<AppInitializationService>();
                    services.AddSingleton<AppInitializationService>();

                    services.AddSingleton<QRReaderHub>();
                    services.AddSingleton<ServerHub>();

                    services.AddSingleton<IDCMLockerController, DCMLockerConector>();
                    services.AddHostedService<DCMServerConnection>();

                    
                });
    }
}
