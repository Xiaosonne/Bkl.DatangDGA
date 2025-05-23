using Bkl.Infrastructure;
using Bkl.Models;
using Bkl.StreamServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Channels;
using static Bkl.Models.DGAModel;
namespace Bkl.StreamServer.Services
{
    public class PersistentService : BackgroundService
    {
        private DbContextOptions<BklDbContext> _option;
        private IConfiguration _config;
        private ILogger<PersistentService> _logger;
        private IServiceScope _scope;
        private IRedisClient _redis;
        private IHubContext<DeviceStateHub> _hubcontext;
        private Channel<DgaPushData> _channelPush;
        private Channel<DgaAlarmResult> _channelAlarm;

        public PersistentService(ILogger<PersistentService> logger,
            Channel<DgaPushData> channel,
            Channel<DgaAlarmResult> channel2,
            IHubContext<DeviceStateHub> hubcontext,

            IServiceProvider service,
            IConfiguration config)
        {
            _hubcontext = hubcontext;
            _channelPush = channel;
            _channelAlarm = channel2;
            _config = config;
            _logger = logger;
            _scope = service.CreateScope();
            _option = _scope.ServiceProvider.GetService<DbContextOptions<BklDbContext>>();
            _redis = _scope.ServiceProvider.GetService<IRedisClient>();
        }

        JsonSerializerOptions opt = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        JsonSerializerOptions webjson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        List<DgaAlarmResult> _results = new List<DgaAlarmResult>();
        DateTime _lastProcess = DateTime.MinValue;

        private void WriteToRedis(DgaPushData? data)
        {
            var ss = data.ToStatus();
            _redis.SetRangeInHash($"DeviceStatus:{data.DeviceId}", ss.ToDictionary(s => s.Name, s => (RedisValue)s.ToJson(opt)));
            _redis.SetEntryInHash($"DeviceUnnormal", $"{data.DeviceId}.short", data.RealUnnormal.ToJson(opt));
            _redis.SetEntryInHash($"DeviceUnnormal", $"{data.DeviceId}.long", data.HistoryUnnormal.ToJson(opt));
        }
        private async Task WriteToDatabase(DgaPushData data)
        {
            var maxIntSec = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:WriteInterval").Value, out var a) ? a : 3600, 3600);
            var time = data.GasData.First().UtcTime.ToLocalTime().UnixEpoch();
            var start = ((long)(time * 1.0 / (maxIntSec))) * maxIntSec;
            var end = start + maxIntSec;
            try
            {
                using (BklDbContext context = new BklDbContext(_option))
                {

                    if (false == context.BklDGAGasProduction.Any(s => s.DeviceRelId == data.DeviceId && s.Time >= start && s.Time < end))
                    {
                        var prods = data.GasData.Select(s => new BklDGAGasProduction
                        {
                            DeviceRelId = data.DeviceId,
                            FacilityRelId = data.FacilityId,
                            FactoryRelId = data.FactoryId,
                            Createtime = s.UtcTime,
                            Time = start,
                            GasName = s.GasName,
                            Id = SnowId.NextId(),
                            Rate = s.AGPR,
                            RateType = "AGPR",
                            TaskId = "sys",

                        }).ToList();
                        prods.AddRange(data.GasData.Select(s => new BklDGAGasProduction
                        {
                            DeviceRelId = data.DeviceId,
                            FacilityRelId = data.FacilityId,
                            FactoryRelId = data.FactoryId,
                            Createtime = s.UtcTime,
                            Time = start,
                            GasName = s.GasName,
                            Id = SnowId.NextId(),
                            Rate = s.RGPR,
                            RateType = "RGPR",
                            TaskId = "sys",
                        }).ToList());
                        context.BklDGAGasProduction.AddRange(prods);
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            try
            {

                using (BklDbContext context = new BklDbContext(_option))
                {
                    var status = context.BklDGAStatus.FirstOrDefault(s => s.DeviceRelId == data.DeviceId && s.Time >= start && s.Time < end);
                    if (status == null)
                    {
                        DeviceUpdateStatusBase[] lis = data.ToStatus();
                        var jsonstr = lis.ToDictionary(s => s.Name, s => double.TryParse(s.Value, out var a) ? a : 0).ToJson();
                        status = jsonstr.JsonToObj<BklDGAStatus>();
                        status.DeviceRelId = data.DeviceId;
                        status.FacilityRelId = data.FacilityId;
                        status.FactoryRelId = data.FactoryId;
                        status.Time = start;
                        status.Id = SnowId.NextId();
                        status.Createtime = DateTime.Now.ToUniversalTime();
                        status.ThreeTatio_Code = data.ThreeRatio.ThreeTatio_Code;
                        context.BklDGAStatus.Add(status);
                        await context.SaveChangesAsync();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            try
            {
                var t1 = long.Parse(start.UnixEpochBack().ToString("yyyyMMddHHmmss"));
                var t2 = long.Parse(end.UnixEpochBack().ToString("yyyyMMddHHmmss"));
                using (BklDbContext context = new BklDbContext(_option))
                {
                    var status = context.BklDeviceStatus.Where(s => s.DeviceRelId == data.DeviceId && s.Time >= t1 && s.Time < t2).Count();
                    if (status == 0)
                    {
                        BklDeviceStatus[] lis = data.GasData.Select(t =>
                        {
                            return new BklDeviceStatus
                            {
                                Id = SnowId.NextId(),
                                DeviceRelId = data.DeviceId,
                                FacilityRelId = data.FacilityId,
                                FactoryRelId = data.FactoryId,
                                StatusName = t.GasName,
                                StatusValue = t.GasValue,
                                GroupName = "null",
                                Time = t1,
                                TimeType = "s",
                                Createtime = t.UtcTime,
                            };
                        }).ToArray();
                        context.BklDeviceStatus.AddRange(lis);
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        private void WriteToDb(IList<DgaAlarmResult> lis)
        {
            using (BklDbContext context = new BklDbContext(_option))
            {
                var logs = lis.GroupBy(s => $"{s.DeviceId}{s.RuleId}")
                    .Select(gp => gp.OrderByDescending(s => s.AlarmTime).First())
                    .ToList();
                foreach (var currentAlarmResult in logs)
                {
                    var dbAlarmResult = context.BklAnalysisLog
                                    .Where(s => s.DeviceId == currentAlarmResult.DeviceId && s.RuleId == currentAlarmResult.RuleId)
                                    .OrderByDescending(s => s.Id)
                                    .FirstOrDefault();
                    //当前告警 最新一条和当前的告警级别不一致或者不存在
                    if (dbAlarmResult == null || dbAlarmResult.Level != currentAlarmResult.LevelCN)
                    {
                        var log = ToLog(currentAlarmResult);
                        context.BklAnalysisLog.Add(log);
                        currentAlarmResult.StartTime = log.StartTime;
                        _hubcontext.Clients.All.SendAsync("OnDgaAlarms", JsonSerializer.Serialize(currentAlarmResult, webjson));
                        WriteToRedis(currentAlarmResult);
                    }
                    else
                    {
                        currentAlarmResult.StartTime = dbAlarmResult.StartTime;
                        WriteToRedis(currentAlarmResult);
                        //告警变为正常 只更新 不记录多次
                        if (dbAlarmResult.Level == MatchRuleLevelCN.正常.ToString())
                        {
                            dbAlarmResult.EndTime = currentAlarmResult.AlarmTime.ToUniversalTime();
                            dbAlarmResult.RecordedData = currentAlarmResult.AlarmValue;
                            //变为正常 十分钟内推送 推送10次
                            if (DateTime.Now.Subtract(dbAlarmResult.StartTime).TotalMinutes < 10)
                            {
                                //三十秒推送一次告警
                                if (DateTime.Now.Subtract(dbAlarmResult.OffsetStart.UnixEpochBack()).TotalSeconds > 60)
                                {
                                    dbAlarmResult.OffsetStart = DateTime.Now.UnixEpoch();
                                    _hubcontext.Clients.All.SendAsync("OnDgaAlarms", JsonSerializer.Serialize(currentAlarmResult, webjson));
                                }
                            }
                        }
                        else
                        {

                            //三十秒推送一次告警
                            if (DateTime.Now.Subtract(dbAlarmResult.OffsetStart.UnixEpochBack()).TotalSeconds > 30)
                            {
                                //更新告警的最后时间
                                dbAlarmResult.OffsetStart = DateTime.Now.UnixEpoch();
                                dbAlarmResult.EndTime = currentAlarmResult.AlarmTime.ToUniversalTime();
                                dbAlarmResult.RecordedData = currentAlarmResult.AlarmValue;
                                dbAlarmResult.AlarmTimes += 1;
                                _hubcontext.Clients.All.SendAsync("OnDgaAlarms", JsonSerializer.Serialize(currentAlarmResult, webjson));
                            }

                            var mini = dbAlarmResult.HandleTimes >= 1 ? 120 : 30;
                            //已经确认过 两个钟头内不再生成同样类型的告警 未确认的30分钟提醒一次

                            if (DateTime.Now.Subtract(dbAlarmResult.OffsetEnd.UnixEpochBack()).TotalMinutes > mini)
                            {
                                //重新推送告警的最后时间
                                dbAlarmResult.OffsetEnd = DateTime.Now.UnixEpoch();
                                _hubcontext.Clients.All.SendAsync("OnDgaAlarms", JsonSerializer.Serialize(new DgaAlarmResult
                                {
                                    FacilityName = currentAlarmResult.FacilityName,
                                    FactoryName = currentAlarmResult.FactoryName,
                                    ErrorReason = $"请尽快处理当前设备{currentAlarmResult.ErrorType}告警",
                                    ErrorType = "告警未恢复"
                                }, webjson));
                            }
                        }
                    }

                }

                context.SaveChanges();
            }
        }
        private void WriteToDb(DgaAlarmResult data)
        {
            _results.Add(data);
            if (DateTime.Now.Subtract(_lastProcess).TotalSeconds < 10)
                return;
            _lastProcess = DateTime.Now;
            try
            {
                WriteToDb(_results);
                _results = new List<DgaAlarmResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
        private void WriteToRedis(DgaAlarmResult data)
        {
            try
            {
                _redis.SetEntryInHash($"DeviceAlarms:{data.DeviceId}", data.RuleId.ToString(), data.ToJson());

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        BklAnalysisLog ToLog(DgaAlarmResult data)
        {
            return new BklAnalysisLog
            {
                Title = data.ErrorType,
                Content = data.ErrorReason,
                Createtime = DateTime.Now.ToUniversalTime(),
                StartTime = data.AlarmTime.ToUniversalTime(),
                EndTime = data.AlarmTime.ToUniversalTime(),
                DeviceId = data.DeviceId,
                FacilityId = data.FacilityId,
                RuleId = data.RuleId,
                Level = data.LevelCN,
                Id = SnowId.NextId(),
                RecordedData = data.AlarmValue,
                OffsetStart = DateTime.Now.UnixEpoch(),
                RecordedPicture = "#",
                RecordedVideo = "#",
                AlarmTimes = 1,
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                await Task.Delay(10);

                try
                {
                    if (_channelPush.Reader.TryRead(out var data))
                    {
                        WriteToRedis(data);
                        await WriteToDatabase(data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                try
                {
                    if (_channelAlarm.Reader.TryRead(out var data))
                    {
                        WriteToDb(data);
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
