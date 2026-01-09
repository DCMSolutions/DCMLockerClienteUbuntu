using DCMLocker.Server.Background;
using DCMLocker.Server.Controllers;
using DCMLocker.Server.Hubs;
using DCMLocker.Server.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

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
                .ConfigureServices(services=> 
                {

                    services.AddHostedService<DCMLockerController>();

                    services.AddHostedService<AppInitializationService>();
                    services.AddSingleton<IConfig2Store, Config2Store>();

                    services.AddSingleton<QRReaderHub>();
                    services.AddSingleton<ServerHub>();

                    services.AddSingleton<IDCMLockerController, DCMLockerConector>();
                    services.AddHostedService<DCMServerConnection>();

                    
                });
    }
}
