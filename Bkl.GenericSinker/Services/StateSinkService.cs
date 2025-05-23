using Bkl.GenericSinker;
using Bkl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Orleans.Runtime;
using System.Text.Json;
using System.Threading.Channels;
using static ConnectionMonitorService;
public class StateSinkService<TService> : BackgroundService where TService : BackgroundService
{
    private Channel<ChannelData<ConnectionMonitorService, HBData>> _hbChannel;
    private ILogger<StateSinkService<TService>> _logger;
    private IConfigurationSection _config;
    private Channel<ChannelData<TService, DeviceState[]>> _serviceMsgChannel;
    private IServiceScope _scope;
    private IServiceProvider _serviceProvider;
    private BklDbContext _context;

    public StateSinkService(IServiceProvider serviceProvider,
        Channel<ChannelData<TService, DeviceState[]>> dgaChannel,
        Channel<ChannelData<ConnectionMonitorService, HBData>> hbChannel,
        ServiceLibOption<TService> opt,
        ILogger<StateSinkService<TService>> logger)
    {

        _hbChannel = hbChannel;
        _logger = logger;
        _config = opt.MainSection;
        _serviceMsgChannel = dgaChannel;
        _scope = serviceProvider.CreateScope();
        _serviceProvider = _scope.ServiceProvider;
        _context = _scope.ServiceProvider.GetService<BklDbContext>();

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sink = _config.GetSection("AppSetting:StateSinkServer").Value;
        ResponseSocket recv = new ResponseSocket(sink);
        while (stoppingToken.IsCancellationRequested == false)
        {
            await Task.Delay(1);
            try
            {
                var str = recv.ReceiveMultipartStrings(2);
                _logger.Debug(DateTime.Now.ToString() + " state " + str[0]);
                _logger.Debug(DateTime.Now.ToString() + " content " + str[1]);
                recv.SendFrame(str[0] + "-resp " + DateTime.Now.ToUniversalTime().ToString());

                if (str[0] == "heartbeat")
                {

                    try
                    {
                        await _hbChannel.Writer.WriteAsync(new ChannelData<ConnectionMonitorService, HBData>
                        {
                            Data = new HBData
                            {
                                HeartBeat = str[1]
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
                else
                {
                    await _serviceMsgChannel.Writer.WriteAsync(new ChannelData<TService, DeviceState[]>
                    {
                        Topic = str[0],
                        Data = JsonSerializer.Deserialize<DeviceState[]>(str[1])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}
