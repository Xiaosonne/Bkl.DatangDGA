using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Channels;
using static Bkl.Models.BklConfig;
using static Bkl.Models.DGAModel;
public class DistributeService : BackgroundService
{
    private IConfiguration _config;
    private ILogger<DistributeService> _logger;
    private Channel<ChannelData<HubChannelData, HubChannelData>> _hubChannelWriter;
    private IServiceScope _scope;
    private DbContextOptions<BklDbContext> _option;
    private IRedisClient _redis;

    public DistributeService(ILogger<DistributeService> logger,
        IServiceProvider service,
        Channel<ChannelData<HubChannelData, HubChannelData>> hubcontext,
        ServiceLibOption<DGAService> config)
    {
        _config = config.MainSection;
        _logger = logger;
        _hubChannelWriter = hubcontext;
        _scope = service.CreateScope();
        _option = _scope.ServiceProvider.GetService<DbContextOptions<BklDbContext>>();
        _redis = _scope.ServiceProvider.GetService<IRedisClient>();
    }
    JsonSerializerOptions webjson = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    JsonSerializerOptions opt = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };


    private void WriteToRedis(DgaPushData? data)
    {
        var ss = data.ToStatus();
        _redis.SetRangeInHash($"DeviceStatus:{data.DeviceId}", ss.ToDictionary(s => s.Name, s => (RedisValue)s.ToJson(opt)));
        _redis.SetEntryInHash($"DeviceUnnormal", $"{data.DeviceId}.short", data.RealUnnormal.ToJson(opt));
        _redis.SetEntryInHash($"DeviceUnnormal", $"{data.DeviceId}.long", data.HistoryUnnormal.ToJson(opt));
    }
    private async Task WriteState(DgaPushData data)
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


    List<DgaAlarmResult> _results = new List<DgaAlarmResult>();
    DateTime _lastProcess = DateTime.MinValue;
    private void WriteAlarms(IList<DgaAlarmResult> lis)
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
                    _hubChannelWriter.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                    {
                        Data = new HubChannelData
                        {
                            Action = "OnDgaAlarms",
                            Data = JsonSerializer.Serialize(currentAlarmResult, webjson)
                        }
                    });
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

                                _hubChannelWriter.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                                {
                                    Data = new HubChannelData
                                    {
                                        Action = "OnDgaAlarms",
                                        Data = JsonSerializer.Serialize(currentAlarmResult, webjson)
                                    }
                                });



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

                            _hubChannelWriter.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                            {
                                Data = new HubChannelData
                                {
                                    Action = "OnDgaAlarms",
                                    Data = JsonSerializer.Serialize(currentAlarmResult, webjson)
                                }
                            });


                        }

                        var mini = dbAlarmResult.HandleTimes >= 1 ? 120 : 30;
                        //已经确认过 两个钟头内不再生成同样类型的告警 未确认的30分钟提醒一次

                        if (DateTime.Now.Subtract(dbAlarmResult.OffsetEnd.UnixEpochBack()).TotalMinutes > mini)
                        {
                            //重新推送告警的最后时间
                            dbAlarmResult.OffsetEnd = DateTime.Now.UnixEpoch();

                            _hubChannelWriter.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                            {
                                Data = new HubChannelData
                                {
                                    Action = "OnDgaAlarms",
                                    Data = JsonSerializer.Serialize(new DgaAlarmResult
                                    {
                                        FacilityName = currentAlarmResult.FacilityName,
                                        FactoryName = currentAlarmResult.FactoryName,
                                        ErrorReason = $"请尽快处理当前设备{currentAlarmResult.ErrorType}告警",
                                        ErrorType = "告警未恢复"
                                    }, webjson)
                                }
                            });
                        }
                    }
                }

            }

            context.SaveChanges();
        }
    }
    private void WriteAlarms(DgaAlarmResult data)
    {
        _results.Add(data);
        if (DateTime.Now.Subtract(_lastProcess).TotalSeconds < 10)
            return;
        _lastProcess = DateTime.Now;
        try
        {
            WriteAlarms(_results);
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
        SubscriberSocket sub = new SubscriberSocket();
        var url = _config.GetSection("AppSetting:DGAStateServer").Value;
        var urlAlarm = _config.GetSection("AppSetting:DGAAlarmServer").Value;
        var connectionService = _config.GetSection("AppSetting:DeviceConnection").Value;
        while (true)
        {
            try
            {
                sub.Connect(url);
                sub.Connect(urlAlarm);
                sub.Connect(connectionService);
                sub.SubscribeToAnyTopic();
                _logger.LogInformation("connected " + url);
                _logger.LogInformation("connected " + urlAlarm);
                _logger.LogInformation("connected " + connectionService);
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

        while (stoppingToken.IsCancellationRequested == false)
        {
            await Task.Delay(1);
            try
            {
                var msg = sub.ReceiveMultipartStrings(2);
                switch (msg[0])
                {

                    case "dga":
                        {
                            _logger.LogInformation(msg[1]);
                            var data = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<DgaPushData>(str), msg[1], msg[1]);
                            if (data == null)
                            {
                                _logger.LogError("ParsError " + msg[1]);
                            }
                            else
                            {
                                await _hubChannelWriter.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                                {
                                    Data = new HubChannelData
                                    {
                                        Action = "OnDgaStates",
                                        Data = JsonSerializer.Serialize(new DeviceWebStatus<DeviceUpdateStatusBase>
                                        {
                                            meta = new DeviceWebMeta { FactoryId = data.FactoryId, FacilityId = data.FacilityId, DeviceId = data.DeviceId },
                                            status = data.GasData.Select(s => s.ToStatus()).SelectMany(s => s)
                                        }, webjson)
                                    }
                                });
                                WriteToRedis(data);
                                await WriteState(data);
                            }

                        }
                        break;
                    case "dgaalarm":
                        {
                            _logger.LogInformation(msg[1]);
                            var data = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<DgaAlarmResult>(str), msg[1], msg[1]);
                            if (data == null)
                            {
                                _logger.LogError("ParsError " + msg[1]);
                            }
                            else
                            {
                                WriteAlarms(data);
                            }
                        }
                        break;
                    case "devconn":
                        _logger.LogInformation(msg[1]);

                        await _hubChannelWriter.Writer.WriteAsync(new ChannelData<HubChannelData, HubChannelData>
                        {
                            Data = new HubChannelData
                            {
                                Action = "OnDgaCon",
                                Data = msg[1]
                            }
                        });
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
        //var influxclient = new InfluxDBClient("http://127.0.0.1:8086", "eHk5ebRJ1FP0nI35rqOL91PBQquXtegfuWVNYBDG8QZ9vcteZWRyQU-Xlj7A7vCi5J9CL9DiaI8V6aWCZ8BhIg==");

        //var writeapi = influxclient.GetWriteApi();
        //while (stoppingToken.IsCancellationRequested == false)
        //{
        //    try
        //    {
        //        var msgs = sub.ReceiveMultipartStrings(2);
        //        Console.WriteLine($"topic:{msgs[0]} content:{msgs[1]}");
        //        var resps = JsonSerializer.Deserialize<ThermalTemperatureResponse[]>(msgs[1]);
        //        writeapi.WriteRecords(resps.Select(s => $"thermaltemperature,deviceId={s.deviceId},factoryId={s.factoryId},facilityId={s.facilityId},ruleId={s.ruleId} max={s.max},min={s.min},average={s.average}").ToArray(), bucket: "bucket1", org: "hn");
        //    }
        //    catch
        //    {

        //    }
        //}
    }

}


