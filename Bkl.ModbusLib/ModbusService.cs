using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Bkl.ModbusLib
{
    public class ModbusService : BackgroundService
    {
        private IServiceScope _scope;
        private IRedisClient? _redis;
        private Channel<ChannelData<ModbusService, DeviceState[]>> _deviceInqueue;
        private Channel<ChannelData<HubChannelData, HubChannelData>> _hubChannel;
        private DbContextOptions<BklDbContext> _option;
        public ModbusService(
            Channel<ChannelData<ModbusService, DeviceState[]>> devicequeue,
            Channel<ChannelData<HubChannelData, HubChannelData>> hubcontext,
          DbContextOptions<BklDbContext> option,
        IServiceProvider service)
        {

            _scope = service.CreateScope();
            _redis = _scope.ServiceProvider.GetService<IRedisClient>();
            _deviceInqueue = devicequeue;
            _hubChannel = hubcontext;
            _option = option;
        }

        JsonSerializerOptions opt = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1);
            List<BklDeviceStatus> status = new List<BklDeviceStatus>();
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var data = await _deviceInqueue.Reader.ReadAsync();
                    _redis.SetRangeInHash($"DeviceStatus:{data.Data.First().DeviceId}", data.Data.ToDictionary(s => s.Name, s => (RedisValue)s.ToJson(opt)));
                    var first = data.Data.First();
                    await _hubChannel.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                    {

                        Data = new HubChannelData
                        {
                            Action = "onStates",
                            Data = new DeviceWebStatus<DeviceState>
                            {
                                meta = new DeviceWebMeta { DeviceId = first.DeviceId, FacilityId = first.FacilityId, FactoryId = first.FactoryId },
                                status = data.Data.ToArray(),
                            }.ToJson(opt)
                        }
                    });

                    status.AddRange(data.Data.Select(s => new BklDeviceStatus
                    {
                        Id = SnowId.NextId(),
                        DeviceRelId = s.DeviceId,
                        FacilityRelId = s.FacilityId,
                        FactoryRelId = s.FactoryId,
                        StatusName = s.Name,
                        StatusValue = double.Parse(s.Value),
                        GroupName = "null",
                        Time = s.CreateTime.UnixEpoch(),
                        TimeType = "s",
                        Createtime = s.CreateTime,
                    }));

                    if (status.Count > 50)
                    {
                        try
                        {
                            using (BklDbContext context = new BklDbContext(_option))
                            {
                                context.BklDeviceStatus.AddRange(status);
                                context.SaveChanges();
                                status = new List<BklDeviceStatus>();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
