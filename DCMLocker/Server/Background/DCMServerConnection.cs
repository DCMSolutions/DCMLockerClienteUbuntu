using DCMLocker.Server.BaseController;
using DCMLocker.Server.Controllers;
using DCMLocker.Server.Hubs;
using DCMLocker.Server.Webhooks;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace DCMLocker.Server.Background
{
    public class DCMServerConnection : BackgroundService
    {
        private readonly IHubContext<ServerHub> _hubContext;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TBaseLockerController _base;
        private readonly IDCMLockerController _driver;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WebhookService _webhookService;

        private HttpClient _http; // instancia que obtenemos de la factory

        public DCMServerConnection(IHubContext<ServerHub> hubContext, IHttpClientFactory httpClientFactory, TBaseLockerController Base, IDCMLockerController driver,
            IConfiguration configuration, IServiceScopeFactory scopeFactory, WebhookService webhookService)
        {
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _base = Base;
            _driver = driver;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _webhookService = webhookService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool estaConectado = true;
            int contadorFallos = 0;
            int delayStatus = 1000;
            int tewerID = 0;
            bool _cerrConectadasPrev = false;

            _http = _httpClientFactory.CreateClient("ServerStatusClient");

            Dictionary<int, (bool Puerta, bool Ocupacion)> previousStates = new();

            async Task checkFail()
            {
                if (stoppingToken.IsCancellationRequested) return;

                using var scope = _scopeFactory.CreateScope();
                var evento = scope.ServiceProvider.GetRequiredService<LogController>();

                if (GetIP() == "")
                {
                    evento.AddEvento(new Evento("Se desconectó de red", "conexión falla"));
                    await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion de red", stoppingToken);
                }
                else
                {
                    try
                    {
                        using var response = await _http.GetAsync("https://www.google.com/generate_204", stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            evento.AddEvento(new Evento("Se desconectó del servidor", "conexión falla"));
                            await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion del servidor", stoppingToken);
                        }
                        else
                        {
                            evento.AddEvento(new Evento("Se desconectó de internet", "conexión falla"));
                            await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion de internet", stoppingToken);
                        }
                    }
                    catch (OperationCanceledException) { /* frenado a mano */ }
                    catch
                    {
                        evento.AddEvento(new Evento("Se desconectó de internet", "conexión falla"));
                        await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion de internet", stoppingToken);
                    }
                }

                estaConectado = false;
            }

            List<TLockerMapDTO> GetLockerStatus(Dictionary<int, (bool Puerta, bool Ocupacion)> previousStates, bool cerrConectadas)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var evento = scope.ServiceProvider.GetRequiredService<LogController>();
                    var newList = new List<TLockerMapDTO>();

                    bool fireEvents = cerrConectadas && _cerrConectadasPrev;

                    if (!cerrConectadas) //si esta desconectado pasa rapido
                    {
                        foreach (var locker in _base.LockerMap.LockerMaps.Values.Where(x => x.IdFisico != null))
                        {
                            var hasPrev = previousStates.TryGetValue(locker.IdBox, out var prev);
                            var puerta = hasPrev && prev.Puerta;
                            var ocup = hasPrev && prev.Ocupacion;

                            newList.Add(new TLockerMapDTO
                            {
                                Id = locker.IdBox,
                                Enable = locker.Enable,
                                AlamrNro = locker.AlamrNro,
                                Size = locker.Size,
                                TempMax = locker.TempMax,
                                TempMin = locker.TempMin,
                                Puerta = puerta,
                                Ocupacion = ocup
                            });
                        }

                        _cerrConectadasPrev = false;
                        return newList;
                    }

                    foreach (var locker in _base.LockerMap.LockerMaps.Values.Where(x => x.IdFisico != null))
                    {
                        var idFisico = locker.IdFisico.GetValueOrDefault();
                        var _CU = idFisico / 16;
                        var _Box = idFisico % 16;

                        var status = _driver.GetCUState(_CU);

                        bool _puerta = status.DoorStatus[_Box];
                        bool _ocupacion = status.SensorStatus[_Box];

                        if (fireEvents && previousStates.TryGetValue(locker.IdBox, out var previousState))
                        {
                            if (previousState.Puerta != _puerta)
                            {
                                if (_puerta)
                                {
                                    evento.AddEvento(new Evento($"Se cerró la puerta del box {locker.IdBox}", "cerraduras"));
                                    _webhookService.SendWebhook("LockerCerrado", $"Se cerró la puerta del box {locker.IdBox}", new { Box = locker.IdBox });
                                }
                                else
                                {
                                    evento.AddEvento(new Evento($"Se abrió la puerta del box {locker.IdBox}", "cerraduras"));
                                    _webhookService.SendWebhook("LockerAbierto", $"Se abrió la puerta del box {locker.IdBox}", new { Box = locker.IdBox });
                                }
                            }
                            if (previousState.Ocupacion != _ocupacion)
                            {
                                if (_ocupacion)
                                {
                                    evento.AddEvento(new Evento($"Se detectó presencia en el box {locker.IdBox}", "sensores"));
                                    _webhookService.SendWebhook("SensorOcupado", $"Se detectó presencia en el box {locker.IdBox}", new { Box = locker.IdBox });
                                }
                                else
                                {
                                    evento.AddEvento(new Evento($"Se liberó presencia en el box {locker.IdBox}", "sensores"));
                                    _webhookService.SendWebhook("SensorLiberado", $"Se liberó presencia en el box {locker.IdBox}", new { Box = locker.IdBox });
                                }
                            }
                        }

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

                    _cerrConectadasPrev = true;
                    return newList;
                }
                catch
                {
                    _cerrConectadasPrev = false;
                    return new();
                }
            }

            while (true)
            {
                try
                {
                    //parece troll que esté arriba pero da tiempo a que arranquen los drivers y no nos de desconectado todo el primer status
                    await Task.Delay(Math.Max(100, delayStatus), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                try
                {
                    string cerraduras;
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var system = scope.ServiceProvider.GetRequiredService<SystemController>();
                        cerraduras = system.GetEstadoCerraduras();
                    }

                    try
                    {
                        using var stream = new FileStream("configjson.json", FileMode.Open, FileAccess.Read, FileShare.Read);
                        var node = System.Text.Json.Nodes.JsonNode.Parse(stream)!;
                        delayStatus = node["DelayStatus"]?.GetValue<int>() ?? 1000;
                        tewerID = node["TewerID"]?.GetValue<int>() ?? 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al leer configjson: {ex.Message}");
                        delayStatus = 1000;
                        tewerID = 0;
                    }

                    var serverCommunication = new ServerStatus
                    {
                        NroSerie = _base.Config.LockerID,
                        Version = _configuration["Version"],
                        IP = GetIP(),
                        EstadoCerraduras = cerraduras,
                        DelayStatus = delayStatus,
                        TewerID = tewerID,
                        LastUpdateTime = DateTime.Now,
                        Locker = GetLockerStatus(previousStates, cerraduras == "Conectadas")
                    };

                    using var response = await _http.PostAsJsonAsync(
                        $"{_base.Config.UrlServer}api/locker/status",
                        serverCommunication,
                        stoppingToken);

                    if (response.IsSuccessStatusCode)
                    {
                        if (estaConectado != true)
                        {
                            estaConectado = true;
                            contadorFallos = 0;
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var evento = scope.ServiceProvider.GetRequiredService<LogController>();
                                evento.AddEvento(new Evento("Se conectó al servidor", "conexión"));
                            }
                            _webhookService.SendWebhook("Conexion", "El locker se reconectó", new { Accion = "Conexión" });
                            await _hubContext.Clients.All.SendAsync("STATUS", "Conexion al servidor", stoppingToken);
                        }
                    }
                    else
                    {
                        if (estaConectado != false || contadorFallos % 10 == 9)
                        {
                            await checkFail();
                        }
                        else
                        {
                            contadorFallos++;
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)// esto es el caso de apagar la app a mano
                {
                    break;
                }
                catch (Exception)
                {
                    if (estaConectado != false || contadorFallos % 10 == 9)
                    {
                        await checkFail();
                    }
                    else
                    {
                        contadorFallos++;
                    }
                    _http = _httpClientFactory.CreateClient("ServerStatusClient");
                }
            }

        }

        string GetIP()
        {
            try
            {
                // 1) Preferir Tailscale
                var tsIp = GetTailscaleIPv4();
                if (!string.IsNullOrWhiteSpace(tsIp))
                    return tsIp;

                // 2) Fallback: IP de red (Ethernet/WiFi)
                var lanIp = GetLanIPv4();
                return lanIp ?? "";
            }
            catch
            {
                return "";
            }
        }

        static string? GetTailscaleIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Tailscale suele aparecer como "Tailscale", "Tailscale Tunnel", "tailscale0", etc.
                if (!IsTailscaleInterface(ni))
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var ip = ua.Address.ToString();

                    // Tailscale IPv4 normalmente es 100.64.0.0/10
                    if (IsTailscaleIPv4(ua.Address))
                        return ip;
                }
            }

            return null;
        }

        static string? GetLanIPv4()
        {
            var ips = new List<string>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (((ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) ||
                     (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) &&
                    ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var ip = ua.Address.ToString();

                            // Excluir Tailscale por si alguna vez cae acá, loopback y APIPA
                            if (ip.StartsWith("127.")) continue;
                            if (ip.StartsWith("169.254.")) continue;
                            if (IsTailscaleIPv4(ua.Address)) continue;

                            ips.Add(ip);
                        }
                    }
                }
            }

            return ips.FirstOrDefault(ip => !ip.EndsWith(".2.3"));
        }

        static bool IsTailscaleInterface(NetworkInterface ni)
        {
            var n = (ni.Name ?? "").ToLowerInvariant();
            var d = (ni.Description ?? "").ToLowerInvariant();
            return n.Contains("tailscale") || d.Contains("tailscale");
        }

        static bool IsTailscaleIPv4(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return b.Length == 4 && b[0] == 100 && b[1] >= 64 && b[1] <= 127;
        }

    }
}