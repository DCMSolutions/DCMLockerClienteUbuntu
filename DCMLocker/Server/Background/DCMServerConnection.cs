using DCMLocker.Server.BaseController;
using DCMLocker.Server.Controllers;
using DCMLocker.Server.Hubs;
using DCMLocker.Server.Interfaces;
using DCMLocker.Server.Webhooks;
using DCMLocker.Shared;
using DCMLocker.Shared.Locker;
using DCMLockerCommunication;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        private readonly IConfig2Store _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WebhookService _webhookService;

        private HttpClient _http;
        private HubConnection? _lockerHub;

        private volatile bool _configSynced = false;
        private TaskCompletionSource<bool>? _configAckTcs;

        private readonly SemaphoreSlim _sendMutex = new(1, 1);
        private DateTime _nextConfigUtc = DateTime.MinValue;

        private bool _cerrConectadasPrev = false;

        // Estado anterior por IdBox
        private readonly Dictionary<int, (bool Puerta, bool Ocupacion)> _previousStates = new();

        private class AckDto
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
        }

        public DCMServerConnection(
            IHubContext<ServerHub> hubContext,
            IHttpClientFactory httpClientFactory,
            TBaseLockerController Base,
            IDCMLockerController driver,
            IConfiguration configuration,
            IConfig2Store config,
            IServiceScopeFactory scopeFactory,
            WebhookService webhookService)
        {
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _base = Base;
            _driver = driver;
            _configuration = configuration;
            _config = config;
            _scopeFactory = scopeFactory;
            _webhookService = webhookService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool estaConectado = true;
            int contadorFallos = 0;

            // defaults
            int delayStatus = 1000;
            int tewerID = 0;

            _http = _httpClientFactory.CreateClient("ServerStatusClient");
            _lockerHub = BuildLockerHubConnection();

            // ACK Config
            _lockerHub.On<AckDto>("AckConfig", ack =>
            {
                if (ack?.Ok == true)
                    _configAckTcs?.TrySetResult(true);
                else
                    _configAckTcs?.TrySetException(new Exception(ack?.Error ?? "AckConfig Ok=false"));
            });

            _lockerHub.Reconnected += _ =>
            {
                // por seguridad: al reconectar, obligamos config nuevamente antes de seguir
                _configSynced = false;
                return Task.CompletedTask;
            };

            _lockerHub.Closed += async _ =>
            {
                _configSynced = false;
                try { await Task.Delay(1000, stoppingToken); } catch { }
            };

            async Task EnsureHubConnectedAsync(CancellationToken ct)
            {
                if (_lockerHub == null) return;

                if (_lockerHub.State == HubConnectionState.Connected ||
                    _lockerHub.State == HubConnectionState.Connecting ||
                    _lockerHub.State == HubConnectionState.Reconnecting)
                    return;

                await _lockerHub.StartAsync(ct);
            }

            async Task CheckFailAsync(CancellationToken ct)
            {
                if (ct.IsCancellationRequested) return;

                using var scope = _scopeFactory.CreateScope();
                var evento = scope.ServiceProvider.GetRequiredService<LogController>();

                if (GetIP() == "")
                {
                    evento.AddEvento(new Evento("Se desconectó de red", "conexión falla"));
                    await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion de red", ct);
                }
                else
                {
                    try
                    {
                        using var response = await _http.GetAsync("https://www.google.com/generate_204", ct);
                        if (response.IsSuccessStatusCode)
                        {
                            evento.AddEvento(new Evento("Se desconectó del servidor", "conexión falla"));
                            await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion del servidor", ct);
                        }
                        else
                        {
                            evento.AddEvento(new Evento("Se desconectó de internet", "conexión falla"));
                            await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion de internet", ct);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch
                    {
                        evento.AddEvento(new Evento("Se desconectó de internet", "conexión falla"));
                        await _hubContext.Clients.All.SendAsync("STATUS", "Desconexion de internet", ct);
                    }
                }

                estaConectado = false;
            }

            // ---------- Sync de Config: obligatorio al iniciar ----------
            async Task SyncConfigOrRetryAsync(CancellationToken ct)
            {
                if (_lockerHub == null) return;

                var backoff = TimeSpan.FromSeconds(1);
                var maxBackoff = TimeSpan.FromSeconds(30);

                while (!ct.IsCancellationRequested && !_configSynced)
                {
                    try
                    {
                        await EnsureHubConnectedAsync(ct);

                        if (_lockerHub.State != HubConnectionState.Connected)
                            throw new Exception("Hub no conectado.");

                        // refrescar config local ANTES de construir el snapshot
                        (delayStatus, tewerID) = await ReadLocalConfigAsync(ct);

                        var status = BuildFullConfig(delayStatus, tewerID);

                        _configAckTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                        await _sendMutex.WaitAsync(ct);
                        try
                        {
                            await _lockerHub.InvokeAsync("Config", status, ct);
                        }
                        finally
                        {
                            _sendMutex.Release();
                        }

                        // Esperar ACK (timeout)
                        var timeout = Task.Delay(TimeSpan.FromSeconds(10), ct);
                        var done = await Task.WhenAny(_configAckTcs.Task, timeout);

                        if (done == _configAckTcs.Task)
                        {
                            await _configAckTcs.Task; // propaga error si hubo

                            _configSynced = true;
                            _nextConfigUtc = DateTime.UtcNow.AddDays(1);

                            if (estaConectado != true)
                            {
                                estaConectado = true;
                                contadorFallos = 0;

                                using (var scope = _scopeFactory.CreateScope())
                                {
                                    var evento = scope.ServiceProvider.GetRequiredService<LogController>();
                                    evento.AddEvento(new Evento("Se conectó al servidor (Config OK)", "conexión"));
                                }

                                await _hubContext.Clients.All.SendAsync("STATUS", "Conexion al servidor", ct);
                            }

                            return;
                        }

                        Console.WriteLine("[Locker->Hub] Config ACK timeout. Reintentando...");
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Locker->Hub] Falló SyncConfig. HubState={_lockerHub?.State}. Error={ex}");

                        if (estaConectado != false || contadorFallos % 10 == 9)
                            await CheckFailAsync(ct);
                        else
                            contadorFallos++;

                        _http = _httpClientFactory.CreateClient("ServerStatusClient");
                    }

                    try { await Task.Delay(backoff, ct); } catch { }
                    backoff = TimeSpan.FromSeconds(Math.Min(maxBackoff.TotalSeconds, backoff.TotalSeconds * 2));
                }
            }

            // ---------- arranque: esperar config OK ----------
            (delayStatus, tewerID) = await ReadLocalConfigAsync(stoppingToken);
            await SyncConfigOrRetryAsync(stoppingToken);

            // ---------- loop principal ----------
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    (delayStatus, tewerID) = await ReadLocalConfigAsync(stoppingToken);

                    await Task.Delay(Math.Max(100, delayStatus), stoppingToken);

                    // si perdimos sync por reconexión, re-sync antes de seguir
                    if (!_configSynced)
                    {
                        await SyncConfigOrRetryAsync(stoppingToken);
                        continue;
                    }

                    // config diario
                    if (DateTime.UtcNow >= _nextConfigUtc)
                    {
                        _configSynced = false;
                        await SyncConfigOrRetryAsync(stoppingToken);

                        if (!_configSynced)
                            continue;
                    }

                    await EnsureHubConnectedAsync(stoppingToken);

                    if (_lockerHub == null || _lockerHub.State != HubConnectionState.Connected)
                        throw new Exception("No conectado al Hub del servidor.");

                    // leer estado cerraduras
                    string cerraduras;
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var system = scope.ServiceProvider.GetRequiredService<SystemController>();
                        cerraduras = system.GetEstadoCerraduras();
                    }

                    bool cerrConectadas = (cerraduras == "Conectadas");

                    // detectar cambios + loguear eventos (SIN webhooks)
                    var changedBoxes = DetectChangedBoxesAndLog(cerrConectadas);

                    // si hay cambios -> Boxes, si no -> Heartbeat
                    await _sendMutex.WaitAsync(stoppingToken);
                    try
                    {
                        if (changedBoxes.Count > 0)
                            await _lockerHub.InvokeAsync("Boxes", _base.Config.LockerID, changedBoxes, stoppingToken);
                        else
                            await _lockerHub.InvokeAsync("Heartbeat", _base.Config.LockerID, stoppingToken);
                    }
                    finally
                    {
                        _sendMutex.Release();
                    }

                    // éxito => conectado
                    if (estaConectado != true)
                    {
                        estaConectado = true;
                        contadorFallos = 0;

                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var evento = scope.ServiceProvider.GetRequiredService<LogController>();
                            evento.AddEvento(new Evento("Se conectó al servidor", "conexión"));
                        }

                        // si querés eliminar 100% webhooks del locker, sacá esto también
                        _webhookService.SendWebhook("Conexion", "El locker se reconectó", new { Accion = "Conexión" });
                        await _hubContext.Clients.All.SendAsync("STATUS", "Conexion al servidor", stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Locker->Hub] Loop error. HubState={_lockerHub?.State}. Error={ex}");

                    if (estaConectado != false || contadorFallos % 10 == 9)
                        await CheckFailAsync(stoppingToken);
                    else
                        contadorFallos++;

                    _http = _httpClientFactory.CreateClient("ServerStatusClient");
                }
            }
        }

        private async Task<(int DelayStatus, int TewerID)> ReadLocalConfigAsync(CancellationToken ct)
        {
            try
            {
                var cfg = await _config.GetAsync(ct);
                var delay = (cfg.DelayStatus > 0) ? cfg.DelayStatus : 2000;
                delay = Math.Clamp(delay, 250, 30000);

                return (delay, cfg.TewerID);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return (2000, 0);
            }
        }

        /// <summary>
        /// Devuelve SOLO los boxes que cambiaron (Puerta u Ocupación).
        /// Además, registra eventos en LogController como antes, pero SIN webhooks.
        /// </summary>
        private List<TLockerMapDTO> DetectChangedBoxesAndLog(bool cerrConectadas)
        {
            var changed = new List<TLockerMapDTO>();

            // Igual que antes: solo “dispara eventos” si veníamos conectados y seguimos conectados
            bool fireEvents = cerrConectadas && _cerrConectadasPrev;

            using var scope = _scopeFactory.CreateScope();
            var evento = scope.ServiceProvider.GetRequiredService<LogController>();

            if (!cerrConectadas)
            {
                _cerrConectadasPrev = false;
                return changed;
            }

            foreach (var locker in _base.LockerMap.LockerMaps.Values.Where(x => x.IdFisico != null))
            {
                var idFisico = locker.IdFisico.GetValueOrDefault();
                var cu = idFisico / 16;
                var box = idFisico % 16;

                var st = _driver.GetCUState(cu);

                bool puerta = st.DoorStatus[box];
                bool ocup = st.SensorStatus[box];

                if (_previousStates.TryGetValue(locker.IdBox, out var prev))
                {
                    bool puertaChanged = prev.Puerta != puerta;
                    bool ocupChanged = prev.Ocupacion != ocup;

                    if (puertaChanged || ocupChanged)
                    {
                        if (fireEvents && puertaChanged)
                        {
                            if (puerta)
                                evento.AddEvento(new Evento($"Se cerró la puerta del box {locker.IdBox}", "cerraduras"));
                            else
                                evento.AddEvento(new Evento($"Se abrió la puerta del box {locker.IdBox}", "cerraduras"));
                        }

                        if (fireEvents && ocupChanged)
                        {
                            if (ocup)
                                evento.AddEvento(new Evento($"Se detectó presencia en el box {locker.IdBox}", "sensores"));
                            else
                                evento.AddEvento(new Evento($"Se liberó presencia en el box {locker.IdBox}", "sensores"));
                        }

                        changed.Add(new TLockerMapDTO
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
                }

                _previousStates[locker.IdBox] = (puerta, ocup);
            }

            _cerrConectadasPrev = true;
            return changed;
        }

        private ServerStatus BuildFullConfig(int delayStatus, int tewerID)
        {
            string cerraduras;
            using (var scope = _scopeFactory.CreateScope())
            {
                var system = scope.ServiceProvider.GetRequiredService<SystemController>();
                cerraduras = system.GetEstadoCerraduras();
            }

            bool cerrConectadas = (cerraduras == "Conectadas");

            var full = new List<TLockerMapDTO>();
            foreach (var locker in _base.LockerMap.LockerMaps.Values.Where(x => x.IdFisico != null))
            {
                bool puerta = false;
                bool ocup = false;

                if (cerrConectadas)
                {
                    var idFisico = locker.IdFisico.GetValueOrDefault();
                    var cu = idFisico / 16;
                    var box = idFisico % 16;

                    var st = _driver.GetCUState(cu);
                    puerta = st.DoorStatus[box];
                    ocup = st.SensorStatus[box];
                }

                full.Add(new TLockerMapDTO
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

            return new ServerStatus
            {
                NroSerie = _base.Config.LockerID,
                Version = _configuration["Version"],
                IP = GetIP(),
                EstadoCerraduras = cerraduras,
                DelayStatus = delayStatus,
                TewerID = tewerID,
                LastUpdateTime = DateTime.Now,
                Locker = full
            };
        }

        private HubConnection BuildLockerHubConnection()
        {
            var hubUrl = $"{_base.Config.UrlServer}".TrimEnd('/') + "/hubs/locker";

            return new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(20),
                })
                .Build();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_lockerHub != null)
            {
                try { await _lockerHub.StopAsync(cancellationToken); } catch { }
                try { await _lockerHub.DisposeAsync(); } catch { }
            }

            await base.StopAsync(cancellationToken);
        }

        // ------------------------------
        // IP helpers (igual que antes)
        // ------------------------------
        private string GetIP()
        {
            try
            {
                var tsIp = GetTailscaleIPv4();
                if (!string.IsNullOrWhiteSpace(tsIp))
                    return tsIp;

                var lanIp = GetLanIPv4();
                return lanIp ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string? GetTailscaleIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (!IsTailscaleInterface(ni))
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IsTailscaleIPv4(ua.Address))
                        return ua.Address.ToString();
                }
            }
            return null;
        }

        private static string? GetLanIPv4()
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

        private static bool IsTailscaleInterface(NetworkInterface ni)
        {
            var n = (ni.Name ?? "").ToLowerInvariant();
            var d = (ni.Description ?? "").ToLowerInvariant();
            return n.Contains("tailscale") || d.Contains("tailscale");
        }

        private static bool IsTailscaleIPv4(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return b.Length == 4 && b[0] == 100 && b[1] >= 64 && b[1] <= 127;
        }
    }
}
