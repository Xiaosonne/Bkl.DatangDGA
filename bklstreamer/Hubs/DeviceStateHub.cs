using Microsoft.AspNetCore.SignalR;

namespace Bkl.StreamServer.Hubs
{
    public class DeviceStateHub : Hub
    {
        public async Task SubscribeDevice(long deviceId)
        {
            await this.Clients.Client(this.Context.ConnectionId)
                  .SendAsync("result", deviceId);
        }
    }
}
