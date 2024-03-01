using DCMLocker.Server.Background;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace DCMLocker.Server.Hubs
{
    public class QRReaderHub : Hub
    {
        public async Task SendToken(string token)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveToken", token);
            }
        }

        //private readonly AppInitializationService _app;

        //public QRReaderHub(AppInitializationService app)
        //{
        //    _app = app;
        //}

        //public async Task COMChanges(string serialPort)
        //{
        //    _app.StopAsync(cancellationToken: System.Threading.CancellationToken.None);
        //    _app.Start(serialPort);

        //    await Clients.All.SendAsync("COMChanges", serialPort);
        //}

    }
}

