using Microsoft.AspNetCore.SignalR;

namespace Bkl.StreamServer.Hubs
{
    public class DeviceStateHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine(this.Context.ConnectionId + " Connected ");
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine(this.Context.ConnectionId + " OnDisconnectedAsync ");

            return base.OnDisconnectedAsync(exception);
        }
        public async Task SubscribeDevice(long deviceId)
        {
            await this.Clients.Client(this.Context.ConnectionId)
                  .SendAsync("result", deviceId);
        }
    }
}
