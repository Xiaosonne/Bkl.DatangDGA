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
using Microsoft.Extensions.Logging;
using Bkl.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

public class ConnectionMonitorService : BackgroundService
{
    private ILogger<StateSinkService> _logger;
    private IConfiguration _config;
    private Channel<ChannelData<ConnectionMonitorService, HBData>> _hbChannel;
    private IServiceScope _scope;
    private IServiceProvider _serviceProvider;
    private BklDbContext _context;

    public ConnectionMonitorService(
        IServiceProvider serviceProvider,
        Channel<ChannelData<ConnectionMonitorService, HBData>> hbChannel,
        IConfiguration config,
        ILogger<StateSinkService> logger )
    {
        _logger = logger;
        _config = config;
        _hbChannel = hbChannel;
        _scope = serviceProvider.CreateScope();
        _serviceProvider = _scope.ServiceProvider;
        _context = _scope.ServiceProvider.GetService<BklDbContext>();

    }
    public class HBData
    {
        public string HeartBeat { get; set; }
    }
   
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime offlineDetect = DateTime.MinValue;
        Dictionary<long, DgaOnlineStatus> contexts = new Dictionary<long, DgaOnlineStatus>();
        var redis = _serviceProvider.GetService<IRedisClient>();

        try
        {
            contexts = redis.GetValuesFromHash("DeviceHeartBeat")
                .Select(s =>
                {
                    var str = s.Value.ToString();
                    return TryCatchExtention.TryCatch(() => str.JsonToObj<DgaOnlineStatus>(), null);
                })
                .Where(s => s != null)
                .ToDictionary(s => s.DeviceId, s => s);
            var mdevs = _context.BklDeviceMetadata.Select(s => new { s.Id, s.FactoryId })
                  .ToList();
            foreach (var item in contexts.Keys.Except(mdevs.Select(s => s.Id)))
            {
                contexts.Remove(item);
                redis.RemoveEntryFromHash("DeviceHeartBeat", item.ToString());
            }
            var lis = mdevs.Where(s => !contexts.ContainsKey(s.Id))
                 .Select(t => new DgaOnlineStatus
                 {
                     FactoryId = t.FactoryId,
                     DeviceId = t.Id,
                     LastHeartBeat = 0,
                     ConnectNotify = 0,
                     Status = "off",
                 })
                 .ToList();
            foreach (var item in lis)
            {
                contexts.Add(item.DeviceId, item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        var offlineMaxTime = _config.GetValue<int>("DGA:OfflineMaxTimeout");
        var ConnectNotifyInterval = _config.GetValue<int>("DGA:ConnectNotifyInterval");
        var OfflineDetectInterval = _config.GetValue<int>("DGA:OfflineDetectInterval");
        var url1 = _config.GetValue<string>("AppSetting:DeviceConnection");
        PublisherSocket pubConnState = new PublisherSocket(url1);

        while (stoppingToken.IsCancellationRequested == false)
        {
            await Task.Delay(1);
            var unix = DateTime.Now.UnixEpoch();
            if (DateTime.Now.Subtract(offlineDetect).TotalSeconds > OfflineDetectInterval)
            {
                offlineDetect = DateTime.Now;
                var vals = redis.GetValuesFromHash("DeviceLoadContext");
                foreach (var hb in contexts)
                {
                    _logger.LogDebug($"device:{hb.Value.DeviceId} offline:{(unix - hb.Value.LastHeartBeat)} max:{offlineMaxTime} push:{(unix - hb.Value.ConnectNotify)} max:{ConnectNotifyInterval}");

                    if (vals.ContainsKey(hb.Value.DeviceId.ToString()))
                        hb.Value.LoadContext = int.Parse(vals[hb.Value.DeviceId.ToString()].ToString());
                    if ((unix - hb.Value.LastHeartBeat) > offlineMaxTime)
                    {

                        if (hb.Value.Status != "off" || (unix - hb.Value.ConnectNotify) > ConnectNotifyInterval)
                        {
                            hb.Value.ConnectNotify = unix;
                            pubConnState.SendMoreFrame("devconn").SendFrame($"{hb.ToJson()}");
                        }
                        hb.Value.Status = "off";
                    }
                    else
                    {
                        if (hb.Value.Status != "on" || (unix - hb.Value.ConnectNotify) > ConnectNotifyInterval)
                        {
                            hb.Value.ConnectNotify = unix;
                            pubConnState.SendMoreFrame("devconn").SendFrame($"{hb.ToJson()}");
                        }
                        hb.Value.Status = "on";
                    }

                    try
                    {
                        redis.SetEntryInHash($"DeviceHeartBeat", hb.Value.DeviceId.ToString(), hb.Value.ToJson());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
            }

            if (_hbChannel.Reader.TryRead(out var msg) && msg != null)
            {
                try
                {
                    var decdata = SecurityHelper.AESDecrypt(msg.Data.HeartBeat).Split(".");

                    long factoryId = long.Parse(decdata[0]);
                    long deviceId = long.Parse(decdata[1]);
                    int timstamp = int.Parse(decdata[2]);

                    DgaOnlineStatus outdata = null;
                    if (!contexts.ContainsKey(deviceId))
                    {
                        outdata = new DgaOnlineStatus
                        {
                            DeviceId = deviceId,
                            FactoryId = factoryId,
                            LastHeartBeat = DateTime.Now.UnixEpoch(),
                            ConnectNotify = 0,
                            Status = "on"
                        };
                        pubConnState.SendMoreFrame("devconn").SendFrame($"{outdata.ToJson()}");
                        contexts.Add(deviceId, outdata);
                    }
                    outdata = contexts[deviceId];
                    outdata.LastHeartBeat = DateTime.Now.UnixEpoch();
                    redis.SetEntryInHash($"DeviceHeartBeat", deviceId.ToString(), outdata.ToJson());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
        }
    }
}
