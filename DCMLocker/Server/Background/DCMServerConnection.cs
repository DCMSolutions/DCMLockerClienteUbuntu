using DCMLocker.Server.BaseController;
using DCMLocker.Server.Controllers;
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
        private readonly SystemController _system;
        private readonly LogController _evento;


        public DCMServerConnection(ILogger<DCMServerConnection> logger, IHubContext<ServerHub> hubContext, ServerHub chatHub, HttpClient httpClient, TBaseLockerController Base, IDCMLockerController driver, IConfiguration configuration, SystemController system, LogController evento)
        {
            _logger = logger;
            _chatHub = chatHub;
            _httpClient = httpClient;
            _base = Base;
            _driver = driver;
            _configuration = configuration;
            _system = system;
            _evento = evento;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool estaConectado = true;
            while (true)
            {
                try
                {
                    ServerStatus serverCommunication = new();
                    serverCommunication.NroSerie = _base.Config.LockerID;
                    serverCommunication.Version = _configuration["Version"];
                    serverCommunication.IP = GetIP();
                    serverCommunication.EstadoCerraduras = _system.GetEstadoCerraduras();

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
                        if (estaConectado != response.IsSuccessStatusCode)
                        {
                            estaConectado = response.IsSuccessStatusCode;
                            _evento.AddEvento(new Evento($"Conexión al servidor", "conexión"));
                            await _chatHub.UpdateStatus("Conexión al servidor");
                        }
                    }
                    else
                    {
                        if (estaConectado != response.IsSuccessStatusCode)
                        {
                            estaConectado = response.IsSuccessStatusCode;

                            _evento.AddEvento(new Evento($"Desconexión de red del locker", "conexión falla 1"));
                            await _chatHub.UpdateStatus("Desconexión de red");

                        }
                        // Handle non-successful status codes, e.g., response.StatusCode, response.ReasonPhrase, etc.
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
                {
                    if (estaConectado != false)
                    {
                        estaConectado = false;
                        if (socketEx.SocketErrorCode == SocketError.NetworkDown ||
                            socketEx.SocketErrorCode == SocketError.HostNotFound)
                        {
                            _evento.AddEvento(new Evento($"Desconexión de red del locker", "conexión falla 2"));
                            await _chatHub.UpdateStatus("Desconexión de red");
                        }
                        else
                        {
                            _evento.AddEvento(new Evento($"Desconexión del servidor", "conexión falla"));
                            await _chatHub.UpdateStatus("Desconexión del servidor");
                        }
                    }
                    // Handle exceptions that occur during the HTTP request
                    Console.WriteLine($"HTTP request failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    if (estaConectado != false)
                    {
                        estaConectado = false;
                        _evento.AddEvento(new Evento($"Error inesperado de conexión", "conexión falla"));
                        await _chatHub.UpdateStatus("Error inesperado");
                    }
                    // Handle other unexpected exceptions
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }

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

                var ips = netinters.Last();                                                             //rng
                UnicastIPAddressInformation ip = ips.GetIPProperties().UnicastAddresses.First();        //rng

                //la otra opcion es ver de todos los netinters y todos los ips.GetIPProperties().UnicastAddresses cual arranca con lo que corresponda, pero eso varía, o sino hacerlo por descarte

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
