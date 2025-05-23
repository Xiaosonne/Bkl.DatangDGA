using Bkl.StreamServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using NetMQ;
using NetMQ.Sockets;
using System.Text.Json;
namespace Bkl.StreamServer.Services
{
    public class HubRoutingService : BackgroundService
    {
        private IHubContext<DeviceStateHub> _hubcontext;
        private ILogger<HubRoutingService> _logger;
        private IConfiguration _config;
        public HubRoutingService(IConfiguration conf, IHubContext<DeviceStateHub> hub, ILogger<HubRoutingService> logger)
        {
            _hubcontext = hub;
            _logger = logger;
            _config = conf;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1);
            SubscriberSocket sub = new SubscriberSocket();
            var url = _config.GetSection("AppSetting:HubRouting").Value;


            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    sub.Connect(url);
                    sub.SubscribeToAnyTopic();
                    _logger.LogInformation("connected " + url);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "connecterror");
                    await Task.Delay(500);
                    try
                    {
                        sub.Dispose();
                    }
                    catch (Exception ex1)
                    {
                        _logger.LogError(ex1, "Dispose Error ");
                    }
                    sub = new SubscriberSocket();
                }
            }
            _logger.LogInformation("Start  ReceiveMultipartStrings" + url);

            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var msg = sub.ReceiveMultipartStrings(2);
                    Console.WriteLine(msg[0] + " " + msg[1]);
                    await _hubcontext.Clients.All.SendAsync(msg[0], msg[1]);

                }
                catch (Exception ex)
                {

                }
            }

        }
    }
}
