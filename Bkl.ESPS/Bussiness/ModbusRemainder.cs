using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Internal;
using StackExchange.Redis;

public class ModbusRemainder:BackgroundService
{
    private IServiceProvider _serviceProvider;
    private IServiceScope _scope;
    private ILogger<ModbusRemainder> _logger;

    public ModbusRemainder(ILogger<ModbusRemainder> logger, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _scope = serviceProvider.CreateScope();
        _logger = logger;

    }
    public override Task StartAsync(CancellationToken cancellationToken)
    { 
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IClusterClient cluster = _serviceProvider.GetService<IClusterClient>();
        var context = _scope.ServiceProvider.GetService<BklDbContext>();
        var connections = context.ModbusConnInfo.ToList();
        var devices = context.BklDeviceMetadata.ToList();
        var redis = _scope.ServiceProvider.GetService<IRedisClient>();
        while (!cluster.IsInitialized)
        {
            _logger.LogInformation($"init...");
            await Task.Delay(1000);
        }
        foreach(var device in devices)
        {
            try
            {
                var deviceGrain = cluster.GetGrain<IDeviceGrain>(new DeviceGrainId(device.Id));
                _logger.LogInformation($"{device.FacilityName} deviceinit {DateTime.Now}");
                var status = deviceGrain.GetDevice().WithTimeout(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{device.FacilityName}  DeviceInitError {ex}");
            }
        }
        //devices.AsParallel().ForAll(device =>
        //{
        //    try
        //    {
        //        var deviceGrain = cluster.GetGrain<IDeviceGrain>(new DeviceGrainId(device.Id));
        //        var status = deviceGrain.GetDevice().WithTimeout(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.ToString());
        //    }
        //});

        while (!stoppingToken.IsCancellationRequested)
        {

            foreach(var item in connections)
            {
                try
                {
                    _logger.LogInformation($"{item.ConnStr} {item.Uuid} ReadStatus {DateTime.Now}");
                    var modbusGrain = cluster.GetGrain<IModbusGrain>(new ModbusGrainId(item.Uuid));
                  
                    var status = modbusGrain.ReadStatus().WithTimeout(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                    foreach (var samedev in status.GroupBy(s => s.DeviceId))
                    {
                        redis.SetRangeInHash($"DeviceRemainder:{samedev.Key}",
                            samedev.ToDictionary(s => s.Index.ToString(),
                            s => (RedisValue)JsonSerializer.Serialize(s)));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{item.ConnStr}  {item.Uuid}  ReadStatusError {ex}");
                }
            }
            //connections.AsParallel().ForAll(item =>
            //{
            //    try
            //    {
            //        var modbusGrain = cluster.GetGrain<IModbusGrain>(new ModbusGrainId(item.Uuid));
            //        var status =  modbusGrain.ReadStatus().WithTimeout(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            //        foreach (var samedev in status.GroupBy(s => s.DeviceId))
            //        {
            //            redis.SetRangeInHash($"DeviceRemainder:{samedev.Key}", 
            //                samedev.ToDictionary(s => s.Index.ToString(), 
            //                s => (RedisValue)JsonSerializer.Serialize(s)));
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex.ToString());
            //    }
            //});
            await Task.Delay(50);
            //foreach (var item in connections)
            //{
            //    try
            //    {
            //        var modbusGrain = cluster.GetGrain<IModbusGrain>(new ModbusGrainId(item.Uuid));
            //        var status = await modbusGrain.ReadStatus();
            //        foreach (var samedev in status.GroupBy(s => s.DeviceId))
            //        {
            //            redis.SetRangeInHash($"DeviceRemainder:{samedev.Key}",
            //                samedev.ToDictionary(s => s.Index.ToString(), s => (RedisValue)JsonSerializer.Serialize(s)));
            //        }
            //    }
            //    catch(Exception ex)
            //    {
            //        _logger.LogError(ex.ToString());
            //    }
            //}
        } 
    }
}
