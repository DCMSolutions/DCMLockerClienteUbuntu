using DCMLocker.Kiosk.Pages;
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
using static System.Net.WebRequestMethods;

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

            async Task checkFail()
            {
                if (GetIP() == "")
                {
                    _evento.AddEvento(new Evento("Se desconectó de red", "conexión falla"));
                    await _chatHub.UpdateStatus("Desconexion de red");
                }
                else
                {
                    try
                    {
                        using var response = await _httpClient.GetAsync("https://www.google.com");
                        if (response.IsSuccessStatusCode)
                        {
                            _evento.AddEvento(new Evento($"Se desconectó del servidor", "conexión falla"));
                            await _chatHub.UpdateStatus("Desconexion del servidor");
                        }
                        else
                        {
                            _evento.AddEvento(new Evento($"Se desconectó de internet", "conexión falla"));
                            await _chatHub.UpdateStatus("Desconexion de internet");
                        }
                    }
                    catch
                    {
                        _evento.AddEvento(new Evento($"Se desconectó de internet", "conexión falla"));
                        await _chatHub.UpdateStatus("Desconexion de internet");
                    }
                }
                estaConectado = false;
            }

            Dictionary<int, (bool Puerta, bool Ocupacion)> previousStates = new();

            while (true)    //!stoppingToken.IsCancellationRequested dio problemas
            {
                try
                {

                    var serverCommunication = new ServerStatus
                    {
                        NroSerie = _base.Config.LockerID,
                        Version = _configuration["Version"],
                        IP = GetIP(),
                        EstadoCerraduras = _system.GetEstadoCerraduras(),
                        Locker = GetLockerStatus(previousStates) // Function to optimize locker status retrieval
                    };

                    var response = await _httpClient.PostAsJsonAsync($"{_base.Config.UrlServer}api/locker/status", serverCommunication);

                    if (response.IsSuccessStatusCode)
                    {
                        if (estaConectado != true)
                        {
                            _evento.AddEvento(new Evento($"Se conectó al servidor", "conexión"));
                            await _chatHub.UpdateStatus("Conexion al servidor");
                            estaConectado = true;
                        }
                    }
                    else
                    {
                        if (estaConectado != false)
                        {
                            await checkFail();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (estaConectado != false)
                    {
                        await checkFail();
                    }
                    // Handle other unexpected exceptions
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }

                await Task.Delay(1000);
            }

            List<TLockerMapDTO> GetLockerStatus(Dictionary<int, (bool Puerta, bool Ocupacion)> previousStates)
            {
                var newList = new List<TLockerMapDTO>();

                foreach (var locker in _base.LockerMap.LockerMaps.Values.Where(x => x.IdFisico != null))
                {
                    var idFisico = locker.IdFisico.GetValueOrDefault();
                    var _CU = idFisico / 16;
                    var _Box = idFisico % 16;

                    var status = _driver.GetCUState(_CU);
                    bool _puerta = status.DoorStatus[_Box];
                    bool _ocupacion = status.SensorStatus[_Box];

                    // Check if state changed
                    if (previousStates.TryGetValue(locker.IdBox, out var previousState))
                    {
                        if (previousState.Puerta != _puerta)
                        {
                            _evento.AddEvento(new Evento($"Se {(_puerta ? "abrió" : "cerró")} la puerta del box {locker.IdBox}", "cerraduras"));
                        }
                        if (previousState.Ocupacion != _ocupacion)
                        {
                            _evento.AddEvento(new Evento($"Se {(_ocupacion ? "detectó" : "liberó")} presencia en el box {locker.IdBox}", "sensores"));
                        }
                    }

                    // Update state tracking
                    previousStates[locker.IdBox] = (_puerta, _ocupacion);

                    newList.Add(new TLockerMapDTO
                    {
                        Id = locker.IdBox,
                        Enable = locker.Enable,
                        AlamrNro = locker.AlamrNro,
                        Size = locker.Size,
                        TempMax = locker.TempMax,
                        TempMin = locker.TempMin,
                        Puerta = _puerta,
                        Ocupacion = _ocupacion
                    });
                }
                return newList;
            }
        }

        string GetIP()
        {
            try
            {
                List<string> retorno = new List<string>() { };
                var netinters = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface item in netinters)
                {
                    if (((item.NetworkInterfaceType == NetworkInterfaceType.Ethernet) ||
                        (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) && item.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ipe in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ipe.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                retorno.Add(ipe.Address.ToString());
                            }
                        }
                    }

                }
                string ip = retorno.Where(ip => !ip.EndsWith(".2.3")).FirstOrDefault();

                if (ip != null) return ip;
                return "";
            }
            catch
            {
                return "";
            }
        }

    }
}
