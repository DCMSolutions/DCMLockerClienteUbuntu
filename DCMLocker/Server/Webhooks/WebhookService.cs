using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DCMLocker.Server.BaseController;
using DCMLocker.Server.Controllers;
using DCMLocker.Shared;

namespace DCMLocker.Server.Webhooks
{
    public class WebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly TBaseLockerController _base;
        private readonly LogController _evento;


        public WebhookService(HttpClient httpClient , TBaseLockerController Base, LogController evento)
        {
            _httpClient = httpClient;
            _base = Base;
            _evento = evento;
        }

        public bool SendWebhookAsync(string evento, object data)
        {
            try
            {
                // Get webhook URL from locker config
                var config = _base.Config;
                if (config?.UrlServer == null)
                {
                    Console.WriteLine("Fallo en la URL server.");
                    return false;
                }

                // Append endpoint path for webhooks
                Uri webhookUrl = new Uri(config.UrlServer, "api/WebhookLocker");

                // Create webhook payload (serialization happens in the constructor)
                var payload = new Webhook(evento, config.LockerID, data);

                // mando el post en otro lado para que no tenga que ser awaited
                Task.Run(async () =>
                {
                    try
                    {
                        await _httpClient.PostAsJsonAsync(webhookUrl, payload).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending webhook: {ex.Message}");
                        _evento.AddEvento(new Evento($"Error inesperado en el webhook: {ex.Message}", "webhook"));
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending webhook: {ex.Message}");
                _evento.AddEvento(new Evento($"Error inesperado en el webhook: {ex.Message}", "webhook"));
                return false;
            }
        }
    }
}
