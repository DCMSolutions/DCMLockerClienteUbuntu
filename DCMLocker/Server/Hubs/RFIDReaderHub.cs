using DCMLocker.Server.Background;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace DCMLocker.Server.Hubs
{
    public class RFIDReaderHub : Hub
    {
        public async Task SendToken(string token)
        {
            if (Clients != null)
            {
                await Clients.Others.SendAsync("ReceiveRFID", token);
            }
        }

    }
}

