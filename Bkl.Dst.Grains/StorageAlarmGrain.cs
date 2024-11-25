using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Bkl.Dst.Grains
{
    public class StorageAlarmGrain : Grain, IAlarmDbStorageSync, IRemindable
    {
        private ILogger<StorageAlarmGrain> _logger;
        IPersistentState<Queue<DeviceAlarmResult>> _state;
        private IRedisClient _redis;

        public StorageAlarmGrain(
            IRedisClient redis,
            [PersistentState("alarmResults", BklConstants.RedisProvider)] IPersistentState<Queue<DeviceAlarmResult>> state,
            ILogger<StorageAlarmGrain> logger)
        {
            _logger = logger;
            _state = state;
            _redis = redis;
        }
        public override async Task OnActivateAsync()
        {
            if (!_state.RecordExists)
            {
                _state.State = new Queue<DeviceAlarmResult>();
                await _state.WriteStateAsync();
                await _state.ReadStateAsync();
            }
            await this.RegisterOrUpdateReminder("reminder1", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        }


        private async Task SaveAlarm(object state)
        {
            var lis = _state.State.ToList();
            var added = new List<DeviceAlarmResult>();
            try
            {
                var json = _redis.DequeueItemFromList($"Orleans.AlarmStorage:{this.GetPrimaryKeyString()}");
                while (json.NotEmpty())
                {
                    var ret = json.JsonToObj<DeviceAlarmResult>();
                    added.Add(ret);
                    json = _redis.DequeueItemFromList($"Orleans.AlarmStorage:{this.GetPrimaryKeyString()}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SaveAlarmTimerError {ex}");
            }


            foreach (var gs in lis.GroupBy(s => s.DeviceId))
            {
                foreach (var sameStatus in gs.GroupBy(s => s.PairId))
                {
                    foreach (var sameAlarn in sameStatus.GroupBy(s => s.AlarmId))
                    {
                        var arr = sameAlarn.OrderBy(s => s.CreateTime).ToArray();
                        var last = 0;
                        added.Add(arr[0]);
                        for (int i = 1; i < arr.Length; i++)
                        {
                            if (arr[last].AlarmLevel != arr[i].AlarmLevel)
                            {
                                added.Add(arr[i]);
                                last = i;
                            }
                        }
                    }
                }
            }
            var alarms = default(List<BklAnalysisLog>);
            try
            {
                alarms = added.Select(s => new BklAnalysisLog
                {
                    Title = $"{s.DeviceStatusNameCN}{s.AlarmLevel}",
                    Content = s.AlarmName,
                    RecordedData = s.AlarmValue.ToString(),
                    RecordedPicture = "",
                    RecordedVideo = "",
                    EndTime = DateTime.MinValue, 
                    DeviceId = s.DeviceId,
                    FacilityId = s.FacilityId,
                    Level = s.AlarmLevel.ToString(),
                    RuleId = s.AlarmId,
                    StartTime = s.CreateTime,
                    AlarmTimes = 1,
                    HandleTimes = 0,
                    OffsetStart = 0,
                    OffsetEnd = 0,
                    Year = s.CreateTime.Year,
                    Day = s.CreateTime.DayOfYear,
                    DayOfMonth = s.CreateTime.Day,
                    HourOfDay = s.CreateTime.Hour,
                    DayOfWeek = s.CreateTime.GetDayOfWeek(),
                    Week = s.CreateTime.WeekOfYear(),
                    Month = s.CreateTime.Month,
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return;
            }
            using (BklDbContext context = new BklDbContext(this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>().Options))
            {
                using (var tran = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.BklAnalysisLog.AddRange(alarms);
                        await context.SaveChangesAsync();
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                        tran.Rollback();
                        return;
                    }
                }
            }
            foreach(var alarm in alarms)
            {
                try
                {
                    BklAnalysisLog.SetStatisticCount(alarm, _redis);

                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
        }
        IDisposable timerHandle = null;
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (timerHandle != null)
            {
                timerHandle.Dispose();
                timerHandle = null;
            }
            timerHandle = this.RegisterTimer(SaveAlarm, "", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }


        public Task StoreAlarm(DeviceAlarmResult deviceStatus)
        {
            try
            {
                _redis.EnqueueItemOnList($"Orleans.AlarmStorage:{this.GetPrimaryKeyString()}", deviceStatus.ToJson());
            }
            catch (Exception ex)
            {
                _logger.LogError($"StorageAlarmError {ex}");
            }
            return Task.CompletedTask;
        }
    }
}
