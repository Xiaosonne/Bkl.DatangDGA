using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using NetMQ.Sockets;
using NetMQ;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Bkl.Infrastructure;
using static ConnectionMonitorService;
using Orleans.Runtime;
public class StateSinkService : BackgroundService
{
    private Channel<ChannelData<ConnectionMonitorService, HBData>> _hbChannel;
    private ILogger<StateSinkService> _logger;
    private DbContextOptionsBuilder _builder;
    private IConfiguration _config;
    private Channel<ChannelData<DGAService, DeviceState[]>> _dgaChannel;
    private IServiceScope _scope;
    private IServiceProvider _serviceProvider;
    private BklDbContext _context;

    public StateSinkService(IServiceProvider serviceProvider,
        Channel<ChannelData<DGAService, DeviceState[]>> dgaChannel,
             Channel<ChannelData<ConnectionMonitorService, HBData>> hbChannel,
        IConfiguration config,
        ILogger<StateSinkService> logger,
        DbContextOptionsBuilder builder)
    {
        _hbChannel = hbChannel;
        _logger = logger;
        _builder = builder;
        _config = config;
        _dgaChannel = dgaChannel;
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
                recv.SendFrame(str[0]+"-resp "+DateTime.Now.ToUniversalTime().ToString());
                if (str[0] == "dga")
                {
                    await _dgaChannel.Writer.WriteAsync(new ChannelData<DGAService, DeviceState[]> { Data = JsonSerializer.Deserialize<DeviceState[]>(str[1]) });
                }
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}
