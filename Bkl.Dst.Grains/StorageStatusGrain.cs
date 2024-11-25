using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using static Bkl.Models.CaculateContext;

namespace Bkl.Dst.Grains
{
    public class StorageStatusGrain : Grain, IStatusDbStorageSync, IRemindable
    {

        private ILogger<StorageStatusGrain> _logger;
        private IRedisClient _redis;

        public StorageStatusGrain(
            IRedisClient redis,
            ILogger<StorageStatusGrain> logger)
        {
            _logger = logger;
            _redis = redis;
        }
        public override async Task OnActivateAsync()
        {
            await this.RegisterOrUpdateReminder("reminder1", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
        }
        public override Task OnDeactivateAsync()
        {
            if (timerHandle != null)
            {
                try
                {
                    timerHandle.Dispose();

                }
                catch (Exception ex)
                {

                }
                timerHandle = null;
            }

            return base.OnDeactivateAsync();
        }
        IDisposable timerHandle = null;
        Queue<DeviceUpdateStatus> queueStatus = new Queue<DeviceUpdateStatus>();
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (timerHandle != null)
            {
                timerHandle.Dispose();
                timerHandle = null;
            }
            timerHandle = this.RegisterTimer(TimerSaveStatus, "", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }
        private async Task TimerSaveStatus(object state)
        {
            var logs = new List<BklDeviceStatus>();

            var lis = new List<DeviceUpdateStatus>();

            while (queueStatus.Count > 0 && lis.Count < 100)
            {
                lis.Add(queueStatus.Dequeue());
            }
            try
            {
                //过滤相同的设备 同一状态的数值
                foreach (var samedev in lis.GroupBy(s => s.DeviceId))
                {
                    foreach (var samestate in samedev.GroupBy(s => s.Index))
                    {
                        var startStatus = samestate.OrderByDescending(s => s.CreateTime).FirstOrDefault();
                        var logs1 = new List<DeviceUpdateStatus>();
                        int i = 0;
                        int total = samestate.Count();
                        while (i < total)
                        {
                            var tempNow = samestate.ElementAt(i);
                            if (tempNow.Value != startStatus.Value)
                            {
                                logs1.Add(tempNow);

                            }
                            startStatus = tempNow;
                            i++;
                        }
                        foreach (var tempNow in logs1)
                        {
                            double val = double.Parse(tempNow.Value);
                            val = double.IsNormal(val) ? val : 0.0;
                            logs.Add(new BklDeviceStatus
                            {
                                Createtime = tempNow.CreateTime,
                                GroupName = tempNow.GroupName,
                                StatusName = tempNow.Name,
                                DeviceRelId = tempNow.DeviceId,
                                FactoryRelId = tempNow.FactoryId,
                                FacilityRelId = tempNow.FacilityId,
                                StatusValue = val,
                                Time = long.Parse(tempNow.CreateTime.ToString("yyyyMMddHHmmss")),
                                TimeType = "s"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            using (BklDbContext context = new BklDbContext(this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>().Options))
            {
                try
                {
                    context.AddRange(logs);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    return;
                }
            }

        }

        public Task Store(DeviceUpdateStatus deviceStatus)
        {
            queueStatus.Enqueue(deviceStatus);
            return Task.CompletedTask;

        }


    }
}
