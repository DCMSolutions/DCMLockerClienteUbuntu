using DCMLocker.Shared.Locker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace DCMLocker.Monitor.Cliente
{
    public class ConfigLocal
    {
        public event EventHandler OnChange;
        public event EventHandler OnRepaint;
        public event EventHandler OnChangeMsn;


        private readonly IJSRuntime jSRuntime;
        private TLockerCliente _cliente;
        public string[] Box = new string[] { 
            "xx 123","xx 453","xx 789","xx 342","xx 123",
            "xx 45F","xx E12","xx 213","xx 345","xx 555",
            "xx 808","xx 564","xx 345","xx 456","xx 666",
            "xx 607","xx 236","xx 543","xx 654","xx 777",
            "xx 405","xx 874","xx 342","xx 876","xx 888",
            "xx 308","xx 457","xx 234","xx 678","xx 999",
        };
        public ConfigLocal (TLockerCliente Cliente, IJSRuntime runtime)
        {
            _cliente = Cliente;
            jSRuntime = runtime; 
        }

        public async void VideoMute()
        {
            await jSRuntime.InvokeVoidAsync("VideoMute");

        }
        public async void PlaySound(string id)
        {
            await jSRuntime.InvokeVoidAsync("PlaySound", id);
        }
        public async void StopSound(string id)
        {
            await jSRuntime.InvokeVoidAsync("StopSound", id);
        }

        public async Task<bool> IsTextToSpeech(string texto)
        {
            bool t = await jSRuntime.InvokeAsync<bool>("IsTextToSpeech", texto);
            return t;
        }
    }
}
