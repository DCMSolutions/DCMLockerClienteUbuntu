﻿using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DCMLockerCommunication;
using DCMLocker;
using Microsoft.AspNetCore.SignalR;
using DCMLocker.Server.Hubs;
using DCMLocker.Shared;
using DCMLocker.Server.Controllers;
using DCMLocker.Server.Webhooks;
using System.Text.Json;

namespace DCMLocker.Server.Background
{
    public interface IDCMLockerController
    {
        CU GetCUState(int CUid);
        void SetBox(int CUid, int Box);
    }

    public class DCMLockerConector : IDCMLockerController
    {
        public CU GetCUState(int CUid)
        {
            return DCMLockerController.GetCUState(CUid);
        }
        public void SetBox(int CUid, int Box)
        {
            DCMLockerController.SetBox(CUid, Box);
        }
    }


    /// <summary> %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%555
    /// Servicio Backgroud para atender al Hardware del locker
    /// </summary> %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    public class DCMLockerController : IHostedService, IDisposable
    {
        private readonly IHubContext<LockerHub, ILockerHub> _hubContext;
        private readonly LogController _evento;
        private readonly SystemController _system;
        private readonly ServerHub _chatHub;
        private readonly WebhookService _webhookService;


        static DCMLockerTCPDriver driver = new DCMLockerTCPDriver();
        //----------------------------------------------------------------------------
        /// <summary>
        /// CONSTRUCTOR
        /// Configura los eventos del driver
        /// </summary>
        // --------------------------------------------------------------------------
        public DCMLockerController(IHubContext<LockerHub, ILockerHub> context2, LogController logController, SystemController system, ServerHub chatHub, WebhookService webhookService)
        {
            _hubContext = context2;
            _evento = logController;
            _system = system;
            _webhookService = webhookService;

            driver.OnConnection += Driver_OnConnection;
            driver.OnDisConnection += Driver_OnDisConnection;
            //driver.Change += Driver_Change;
            driver.OnError += Driver_OnError;
            driver.OnCUChange += Driver_OnCUChange;
            _chatHub = chatHub;
        }

        //---------------------------------------------------------------------------
        /// <summary>
        /// Libera todos los recursos
        /// </summary>
        //---------------------------------------------------------------------------
        public void Dispose()
        {
            driver = null;
        }
        //---------------------------------------------------------------------------
        /// <summary>
        /// Atiende al evento Start de los servicios.
        /// Activa la comunicacion con el locker
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        //---------------------------------------------------------------------------
        public Task StartAsync(CancellationToken cancellationToken)
        {
            driver.IP = "192.168.2.178";
            driver.Port = 4001;

            driver.Start();

            //printear la version y esperar a que tenga config para la ip del webhook
            string version = GetVersion();

            _evento.AddEvento(new Evento($"El sistema se ha iniciado con versión {version}", "sistema"));
            _webhookService.SendWebhook("Sistema", $"El locker se ha iniciado con versión {version}", new { Accion = "Inicio" });

            //_system.OpenChromium();

            return Task.CompletedTask;
        }
        //-----------------------------------------------------------------------------
        /// <summary>
        /// Atiende al evento Stop de los servicios
        /// Detiene la comunicacion con el locker
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        //-----------------------------------------------------------------------------
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                driver.Stop();
            }
            catch (Exception er)
            {
                Console.WriteLine($"Error en STOP de servicio Background: {er.Message}");
            }
            return Task.CompletedTask;
        }
        //-----------------------------------------------------------------------------
        /// <summary>
        /// EVENTO: ERROR
        /// FUENTE: Driver de locker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">EvtArgError</param>
        //-----------------------------------------------------------------------------
        private async void Driver_OnError(object sender, EventArgs e)
        {
            Console.WriteLine("ERROR:" + ((EvtArgError)e).Er.Message); 
            _system.ChangeEstado("Desconectadas");
            _evento.AddEvento(new Evento("Se desconectaron las cerraduras con error", "cerraduras falla"));
            _webhookService.SendWebhook("Cerraduras", "Se desconectaron las cerraduras con un error", new { Accion = "Desconexion con error" });
            await _chatHub.UpdateCerraduras("Desconectadas");
        }
        //------------------------------------------------------------------------------
        /// <summary>
        /// EVENTO: DISCONNECTION
        /// FUENTE: Driver de locker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //------------------------------------------------------------------------------
        private async void Driver_OnDisConnection(object sender, EventArgs e)
        {
            _system.ChangeEstado("Desconectadas");
            _evento.AddEvento(new Evento("Se desconectaron las cerraduras", "cerraduras falla"));
            _webhookService.SendWebhook("Cerraduras", "Se desconectaron las cerraduras", new { Accion = "Desconexion" });
            await _chatHub.UpdateCerraduras("Desconectadas");
        }
        //------------------------------------------------------------------------------
        /// <summary>
        /// EVENTO: CONNECTION
        /// FUENTE: Driver de locker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //------------------------------------------------------------------------------
        private async void Driver_OnConnection(object sender, EventArgs e)
        {
            _system.ChangeEstado("Conectadas");
            _evento.AddEvento(new Evento("Se conectaron las cerraduras", "cerraduras"));
            _webhookService.SendWebhook("Cerraduras", "Se conectaron las cerraduras", new { Accion = "Conexion" });
            await _chatHub.UpdateCerraduras("Conectadas");
        }
        //------------------------------------------------------------------------------
        /// <summary>
        /// EVENTO: CHANGE
        /// FUENTE: Driver de locker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //------------------------------------------------------------------------------
        //private async void Driver_Change(object sender, EventArgs e)
        //{
        //    //hubo un change
        //    //EvtArgInfo v = (EvtArgInfo)e;

        //    //_evento.AddEvento(new Evento($"Change, {v.Info}", "test"));
        //    //await _chatHub.UpdateCerraduras("Conectadas");     aca irá otro hub para cosillas
        //}
        //------------------------------------------------------------------------------
        /// <summary>
        /// EVENTO: CUCHANGE
        /// FUENTE: Driver de locker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">EvtArgCUChange</param>
        //------------------------------------------------------------------------------
        private void Driver_OnCUChange(object sender, EventArgs e)
        {
            EvtArgCUChange v = (EvtArgCUChange)e;
            _hubContext.Clients.All.LockerUpdated(v.CU.ADDR, "Cambio");


            //Console.WriteLine("CAMBIO:");
            //for (int x = 0; x < 16; x++)
            //{
            //    Console.WriteLine($"PUERTAS [{x}]: {v.CU.DoorStatus[x]}");
            //    Console.WriteLine($"SENSOR [{x}]: {v.CU.SensorStatus[x]}");
            //}


        }

        public static CU GetCUState(int CUid)
        {
            return driver.GetStatus(CUid);
        }

        public static void SetBox(int CU, int Box)
        {
            driver.OpenBox(CU, Box);
        }

        private string GetVersion()
        {
            try
            {
                var configPath = "appsettings.json";
                if (!System.IO.File.Exists(configPath))
                {
                    return "";
                }

                var json = System.IO.File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("Version", out JsonElement versionElement))
                {
                    return versionElement.GetString() ?? "";
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
