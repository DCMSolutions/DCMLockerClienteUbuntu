using DCMLocker.Server.BaseController;
using DCMLocker.Server.Hubs;
using DCMLocker.Shared;
using DCMLocker.Shared.Locker;
using DCMLockerCommunication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DCMLocker.Server.Background
{
    public class DCMServerConnection : BackgroundService
    {
        private readonly ILogger<DCMServerConnection> _logger;
        private readonly ServerHub _chatHub;
        private readonly HttpClient _httpClient;
        private readonly TBaseLockerController _base;
        private readonly IDCMLockerController _driver;
        private readonly IConfiguration _configuration;

        public DCMServerConnection(ILogger<DCMServerConnection> logger, IHubContext<ServerHub> hubContext, ServerHub chatHub, HttpClient httpClient, TBaseLockerController Base, IDCMLockerController driver, IConfiguration configuration)
        {
            _logger = logger;
            _chatHub = chatHub;
            _httpClient = httpClient;
            _base = Base;
            _driver = driver;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    ServerStatus serverCommunication = new();
                    serverCommunication.NroSerie = _base.Config.LockerID;
                    serverCommunication.Version = _configuration["Version"];
                    serverCommunication.IP = GetIP();
                    serverCommunication.EstadoCerraduras = "";
                    List<TLockerMapDTO> newList = new();

                    var lockers = _base.LockerMap.LockerMaps.Values.Where(x => x.IdFisico != null).ToList();
                    foreach (var locker in lockers)
                    {
                        bool _puerta = false;
                        bool _ocupacion = false;
                        if (locker.IdFisico != null)
                        {
                            var _CU = locker.IdFisico.GetValueOrDefault() / 16;
                            var _Box = locker.IdFisico.GetValueOrDefault() % 16;

                            CU status = _driver.GetCUState(_CU);
                            _puerta = status.DoorStatus[_Box];
                            _ocupacion = status.SensorStatus[_Box];
                        }
                        TLockerMapDTO lockerDTO = new();
                        lockerDTO.Id = locker.IdBox;
                        lockerDTO.Enable = locker.Enable;
                        lockerDTO.AlamrNro = locker.AlamrNro;
                        lockerDTO.Size = locker.Size;
                        lockerDTO.TempMax = locker.TempMax;
                        lockerDTO.TempMin = locker.TempMin;
                        lockerDTO.Puerta = _puerta;
                        lockerDTO.Ocupacion = _ocupacion;
                        newList.Add(lockerDTO);

                    }
                    serverCommunication.Locker = newList;
                    var response = await _httpClient.PostAsJsonAsync($"{_base.Config.UrlServer}api/locker/status", serverCommunication);

                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful
                        //Console.WriteLine(response.ToString());
                        //Console.WriteLine($"Response is {await response.Content.ReadAsStringAsync()}");
                        //await _chatHub.UpdateStatus("Connected");
                    }
                    else
                    {
                        // Handle non-successful status codes, e.g., response.StatusCode, response.ReasonPhrase, etc.
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                        //await _chatHub.UpdateStatus("Disonnected");

                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle exceptions that occur during the HTTP request
                    Console.WriteLine($"HTTP request failed: {ex.Message}");
                    await _chatHub.UpdateStatus("Disonnected");

                }
                catch (Exception ex)
                {
                    // Handle other unexpected exceptions
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    //await _chatHub.UpdateStatus("Disonnected");

                }

                //await _chatHub.SendMessage("Ejemplo", "Prueba");

                await Task.Delay(1000);
            }
        }

        string GetIP()
        {
            try
            {
                var netinters = NetworkInterface.GetAllNetworkInterfaces();
                netinters = netinters.Where(item => ((item.NetworkInterfaceType == NetworkInterfaceType.Ethernet) ||
                        (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) && item.OperationalStatus == OperationalStatus.Up).ToArray();
                var ips = netinters.First();
                UnicastIPAddressInformation ip = ips.GetIPProperties().UnicastAddresses.First();
                if (ip.Address.ToString() != null) return ip.Address.ToString();
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
