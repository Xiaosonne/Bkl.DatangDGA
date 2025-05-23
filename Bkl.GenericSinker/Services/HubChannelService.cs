using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;

public class HubChannelService : BackgroundService
{
    private IConfiguration _config;
    private Channel<ChannelData<HubChannelData, HubChannelData>> _hubChannelData;

    public HubChannelService(Channel<ChannelData<HubChannelData, HubChannelData>> hubdata, IConfiguration conf)
    {
        _config = conf;
        _hubChannelData = hubdata;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1);
        var pubsockt = _config.GetSection("AppSetting:HubRouting").Value;
        PublisherSocket pub = new PublisherSocket();
        pub.Bind(pubsockt);

        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                var data = await _hubChannelData.Reader.ReadAsync();
                pub.SendMoreFrame(data.Data.Action).SendFrame(data.Data.Data);
                Console.WriteLine("send hub " + data.Data.Action + " " + data.Data.Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
