using DCMLocker.Server.Background;
using DCMLocker.Server.BaseController;
using DCMLocker.Server.Hubs;
using DCMLocker.Server.Webhooks;
using DCMLocker.Shared;
using DCMLocker.Shared.Locker;
using DCMLockerCommunication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DCMLocker.Server.Controllers
{

    [Route("ClientApp/[controller]")]
    [Route("KioskApp/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly TBaseLockerController _base;
        private readonly HttpClient _http;
        private readonly LogController _evento;
        private readonly WebhookService _webhookService;

        /// <summary> -----------------------------------------------------------------------
        /// Constructor
        /// </summary>
        public AssetsController(TBaseLockerController Base, HttpClient http, LogController logController, WebhookService webhookService)
        {
            _base = Base;
            _http = http;
            _evento = logController;
            _webhookService = webhookService;
        }



        [HttpPost("SendRequest")]
        public async Task<IActionResult> SendRequest([FromBody] AssetsRequest req)
        {
            try
            {
                //_evento.AddEvento(new Evento($"Pedido de validación token {Token}", "token"));
                //var ortaWebhook = await _webhookService.SendWebhookAsync("PeticionToken", $"Se envió al servidor el token {Token}", new { Token = Token });

                req.NroSerieLocker = _base.Config.LockerID;

                Uri uri = new Uri(_base.Config.UrlServer, "api/AssetsLocker");
                _http.Timeout = TimeSpan.FromSeconds(5);

                var response = await _http.PostAsJsonAsync(uri, req);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var serverResponse = await response.Content.ReadFromJsonAsync<AssetsResponse>();
                        return Ok(serverResponse);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode((int)response.StatusCode);
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(403);
            }
        }

    }
}
