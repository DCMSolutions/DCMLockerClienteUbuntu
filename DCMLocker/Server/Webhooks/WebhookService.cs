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

        public bool SendWebhookAsync(string evento, string descripcion, object data)
        {
            try
            {
                var config = _base.Config;
                if (config?.UrlServer == null)
                {
                    Console.WriteLine("Fallo en la URL server.");
                    return false;
                }

                Uri webhookUrl = new Uri(config.UrlServer, "api/WebhookLocker");

                var payload = new Webhook(evento, config.LockerID, descripcion, data);

                // mando el post en otro lado para que no tenga que ser awaited
                Task.Run(async () =>
                {
                    Console.WriteLine("sending");
                    try
                    {
                        var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload).ConfigureAwait(false);
                        Console.WriteLine($"Response Status Code: {response.StatusCode}");
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Webhook successfully received.");
                        }
                        else
                        {
                            Console.WriteLine($"Webhook failed with status code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending webhook: {ex.Message}");
                        _evento.AddEvento(new Evento($"Error inesperado en el webhook: {ex.Message}", "webhook"));
                    }
                    Console.WriteLine("sent");
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
