using DCMLocker.Server.BaseController;
using DCMLocker.Server.Hubs;
using DCMLocker.Shared;
using DCMLockerCommunication;
using Microsoft.AspNetCore.SignalR;
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

        public DCMServerConnection(ILogger<DCMServerConnection> logger, IHubContext<ServerHub> hubContext, ServerHub chatHub, HttpClient httpClient, TBaseLockerController Base, IDCMLockerController driver)
        {
            _logger = logger;
            _chatHub = chatHub;
            _httpClient = httpClient;
            _base = Base;
            _driver = driver;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {

                try
                {
                    ServerStatus serverCommunication = new();
                    serverCommunication.NroSerie = _base.Config.LockerID;
                    List<TLockerMapDTO> newList = new();

                    var lockers = _base.LockerMap.LockerMaps.Values.Where(x => x.Id != 0 && x.Enable).ToList();
                    foreach (var locker in lockers)
                    {
                        var cu = locker.BoxAddr / 16;
                        var box = locker.BoxAddr % 16;
                        CU status = _driver.GetCUState(cu);
                        var _puerta = status.DoorStatus[box];
                        var _ocupacion = status.SensorStatus[box];
                        TLockerMapDTO lockerDTO = new();
                        lockerDTO.Id = locker.Id;
                        lockerDTO.Enable = locker.Enable;
                        lockerDTO.AlamrNro = locker.AlamrNro;
                        lockerDTO.Size = locker.Size;
                        lockerDTO.State = locker.State;
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


    }


}
