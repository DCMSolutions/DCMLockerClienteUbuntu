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


        public WebhookService(HttpClient httpClient , TBaseLockerController Base)
        {
            _httpClient = httpClient;
            _base = Base;
        }

        public async Task<bool> SendWebhookAsync(string evento, object data)
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
                Uri webhookUrl = new Uri(config.UrlServer, "WebhookLocker");

                // Create webhook payload (serialization happens in the constructor)
                var payload = new Webhook(evento, config.LockerID, data);

                // Send POST request
                var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending webhook: {ex.Message}");
                return false;
            }
        }
    }
}
