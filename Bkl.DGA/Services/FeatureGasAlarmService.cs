using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tmds.Linux;
using static Bkl.Models.BklConfig;
using static Bkl.Models.DGAModel;
using static DGAAnalysis;
using static NetMQ.NetMQSelector;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Formats.Asn1.AsnWriter;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bkl.DGA.Services
{
    public enum WarningSettingType
    {
        三比值法,
        特征气体法,
        故障气体注意值,
        产气速率,
        其它辅助方法
    }
    public enum WarningType
    {
        正常 = 40,
        预警 = 50,
        告警 = 60,
        联动 = 70,
    }

    public enum GasMethod
    {
        biggerThan = 0,
        lessThan = 1,
        equalTo = 2,
        inRange = 3,
        increase = 4,
        decrease = 5
    }
    public class FeatureGasAlarmService : BackgroundService
    {
        private IServiceScope _scope;
        private IServiceProvider _serviceProvider;
        private DbContextOptions<BklDbContext> _contextOpt;
        private Channel<ChannelData<FeatureGasAlarmService, AlarmServiceFeatureGas[]>> _channel;
        private IConfiguration _config;
        private ILogger<FeatureGasAlarmService> _logger;
        Timer _timer;
        static IEnumerable<BklAnalysisRule> _analysisRules;
        static IEnumerable<BklDeviceMetadata> _deviceList;
        private readonly object _lock = new object();

        public FeatureGasAlarmService(IConfiguration config, ILogger<FeatureGasAlarmService> logger, IServiceProvider serviceProvider, Channel<ChannelData<FeatureGasAlarmService, AlarmServiceFeatureGas[]>> channel)
        {
            _scope = serviceProvider.CreateScope();
            _serviceProvider = _scope.ServiceProvider;
            _contextOpt = _scope.ServiceProvider.GetService<DbContextOptions<BklDbContext>>();
            _channel = channel;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PublisherSocket pubAlarmsocket = new PublisherSocket(_config.GetSection("AppSetting:DGAAlarmService").Value);

            DateTime lastLoadData = DateTime.MinValue;
            while (stoppingToken.IsCancellationRequested == false)
            {
                await Task.Delay(1000);
                if (DateTime.Now.Subtract(lastLoadData).TotalSeconds > 120)
                {
                    lastLoadData = DateTime.Now;
                    try
                    {
                        using (var _context = new BklDbContext(_contextOpt))
                        {
                            _analysisRules = _context.BklAnalysisRule.ToList();
                            _deviceList = _context.BklDeviceMetadata.ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
                try
                {

                    if (_analysisRules == null)
                        _analysisRules = new List<BklAnalysisRule>();

                    var rules = _analysisRules.Where(x => x.DeviceType.StartsWith("DGA")).ToList();
                    var recive = await _channel.Reader.ReadAsync();
                    var allGasFeatureData = recive.Data.ToList();
                    //var allGasFeatureData = GetData().ToList();
                    //设备信息
                    DeviceMeta[] deviceMetas = allGasFeatureData.GroupBy(x => x.DeviceMeta.DeviceId)
                        .Select(g => g.First().DeviceMeta)
                        .ToArray();
                    List<DgaAlarmResult> bklList = new List<DgaAlarmResult>();
                    foreach (var deviceMeta in deviceMetas)
                    {
                        //按时间升序排序
                        var devGasFeatureData = allGasFeatureData.Where(x => x.DeviceMeta.DeviceId == deviceMeta.DeviceId)
                            .Select(x => new AlarmServiceFeatureGas
                            {
                                DeviceMeta = x.DeviceMeta,
                                GasName = x.GasName,
                                GasData = x.GasData.Where(s => s.UtcTime != DateTime.MinValue).OrderBy(y => y.UtcTime).ToArray(),
                            })
                            .ToArray();
                        var device = _deviceList.FirstOrDefault(x => x.Id == deviceMeta.DeviceId);
                        if (device == null)
                            continue;
                        //故障气体注意值
                        var resultList2 = GetAlarm(devGasFeatureData, rules, device, WarningSettingType.故障气体注意值);
                        bklList.AddRange(resultList2);
                        //其它辅助方式
                        var resultList3 = GetAlarm(devGasFeatureData, rules, device, WarningSettingType.其它辅助方法);
                        bklList.AddRange(resultList3);
                        //特征气体法
                        var resultList = GetAlarm(devGasFeatureData, rules, device, WarningSettingType.特征气体法);
                        bklList.AddRange(resultList);

                    }
                    foreach (var item in bklList)
                    {
                        pubAlarmsocket.SendMoreFrame("dgaalarm").SendFrame(item.ToJson());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                // await Task.Delay(1000);
            }


        }

        public AlarmServiceFeatureGas[] GetData()
        {
            DateTime now = DateTime.Now.ToUniversalTime();
            AlarmServiceFeatureGas[] data = new AlarmServiceFeatureGas[]{
                new AlarmServiceFeatureGas
                    {
                        GasName = "C2H2",
                        DeviceMeta=new DeviceMeta{ DeviceId=228936674099205,FacilityId=228928668512261,FactoryId=228844691623942 },
                        GasData = new GasDayData[]
                        {
                            new GasDayData
                            {
                                Day=1,
                                UtcTime=now.AddDays(-3),
                                Value=160
                            } ,
                            new GasDayData
                            {
                                Day=2,
                                UtcTime=now.AddDays(-2),
                                Value=165
                            } ,
                            new GasDayData
                            {
                                Day=3,
                                UtcTime=now.AddDays(-1),
                                Value=170
                            } ,
                            new GasDayData
                            {
                                Day=4,
                                UtcTime=now,
                                Value=175
                            } ,
                            new GasDayData
                            {
                                Day=5,
                                UtcTime=now.AddDays(1),
                                Value=180
                            },
                            new GasDayData
                            {
                                Day=6,
                                UtcTime=now.AddDays(2),
                                Value=321
                            }
                        }
                    },
                new AlarmServiceFeatureGas
                    {
                        GasName = "CH2/H2",
                        DeviceMeta=new DeviceMeta{ DeviceId=228936674099205,FacilityId=228928668512261,FactoryId=228844691623942 },
                        GasData = new GasDayData[]
                        {
                            new GasDayData
                            {
                                Day=1,
                                UtcTime=now.AddDays(-3),
                                Value=100
                            } ,
                            new GasDayData
                            {
                                Day=2,
                                UtcTime=now.AddDays(-2),
                                Value=165
                            } ,
                            new GasDayData
                            {
                                Day=3,
                                UtcTime=now.AddDays(-1),
                                Value=170
                            } ,
                            new GasDayData
                            {
                                Day=4,
                                UtcTime=now,
                                Value=175
                            } ,
                            new GasDayData
                            {
                                Day=5,
                                UtcTime=now.AddDays(1),
                                Value=180
                            },
                            new GasDayData
                            {
                                Day=6,
                                UtcTime=now.AddDays(2),
                                Value=321
                            }
                        }
                    },
            };
            return data;

        }

        Dictionary<GasMethod, Func<GasDayData[], BklAnalysisRule, bool>> GasCompareMethod = new Dictionary<GasMethod, Func<GasDayData[], BklAnalysisRule, bool>>
        {
            { GasMethod.biggerThan,(gasdata,rule)=>gasdata.OrderByDescending(s=>s.UtcTime).Take(1).All(x=>x.Value>Convert.ToDouble(rule.Max) )},
            { GasMethod.lessThan,(gasdata,rule)=>gasdata.OrderByDescending(s=>s.UtcTime).Take(1).All(x=>x.Value<(Convert.ToDouble(rule.Min)) )},
            { GasMethod.equalTo,(gasdata,rule)=>gasdata.OrderByDescending(s=>s.UtcTime).Take(1).All(x=>x.Value==(Convert.ToDouble(rule.Max)) )},
            { GasMethod.inRange,(gasdata,rule)=>gasdata.OrderByDescending(s=>s.UtcTime).Take(1).All(x=>x.Value>=(Convert.ToDouble(rule.Min)) &&x.Value<=(Convert.ToDouble(rule.Max)) )},
            { GasMethod.increase,(gasdata,rule)=>gasdata.OrderBy(s=>s.UtcTime).Select(x => x.Value).Aggregate(0.0, (prev, next) => prev+=(next - prev))>0 },
            { GasMethod.decrease,(gasdata,rule)=>gasdata.OrderBy(s=>s.UtcTime).Select(x => x.Value).Aggregate(0.0, (prev, next) =>prev+=( next - prev))<0 }

        };
        public void SetAlarm(BklAnalysisRule bklAnalysis, BklDbContext dbContext)
        {
            var model = dbContext.BklAnalysisRule.FirstOrDefault(x => x.Id == bklAnalysis.Id);
            if (model != null) dbContext.BklAnalysisRule.Update(bklAnalysis);
            else dbContext.BklAnalysisRule.AddRange(bklAnalysis);
            dbContext.SaveChanges();
        }

        /// <summary>
        /// 告警
        /// </summary>
        /// <param name="gasData"></param>
        /// <param name="database">设备-检测规则</param>
        /// <param name="warnSet">告警设置类型</param>
        /// <returns></returns>
        public List<DgaAlarmResult> GetAlarm(AlarmServiceFeatureGas[] gasData, IEnumerable<BklAnalysisRule> database, BklDeviceMetadata device, WarningSettingType warnSet)
        {
            string settingType = warnSet.ToString();
            List<DgaAlarmResult> results = new List<DgaAlarmResult>();
            var alarmrules = database
               .Where(x => x.DeviceId == device.Id && x.ProbeName.Equals(settingType))
               .ToList()
               .Where(x => gasData.Any(d => d.GasName.Equals(x.StatusName)));
            foreach (var rule in alarmrules)
            {
                if (gasData.Where(x => x.GasName.Equals(rule.StatusName)).Count() <= 0) break;
                var currentData = gasData.Where(x => x.GasName.Equals(rule.StatusName)).FirstOrDefault().GasData.OrderByDescending(x => x.UtcTime).ToArray();
                var method = Enum.Parse<GasMethod>(rule.Method);
                if (GasCompareMethod.TryGetValue(method, out var compareFunc) == false || compareFunc == null)
                    continue;
                if (currentData.Count() == 0)
                    continue;

                var gasdata = gasData.FirstOrDefault(x => x.GasName.Equals(rule.StatusName))?.GasData.OrderByDescending(s => s.UtcTime).FirstOrDefault();
                string recorddata = gasdata.Value.ToString("0.00");

                string warning = ((MatchRuleLevelCN)rule.Level).ToString();
                string title = $"{rule.StatusName}{warnSet.ToString()}{warning}";

                var isAlarm = compareFunc(currentData, rule);
                if (isAlarm)
                {
                    _logger.LogInformation($"{warning} {recorddata} {title} {rule.StatusName}气体{gasdata.Value.ToString("0.00")}超出预设区间[{rule.Min},{rule.Max}]");

                    results.Add(new DgaAlarmResult()
                    {
                        Level = rule.Level,
                        LevelCN = warning,
                        AlarmValue = recorddata,
                        ErrorType = title,
                        ErrorReason = $"{rule.StatusName}气体{gasdata.Value.ToString("0.00")}超出预设区间[{rule.Min},{rule.Max}]",
                        ErrorCode = "#",
                        AlarmTime = DateTime.Now,
                        RuleId = rule.Id,
                        RuleName = rule.RuleName,
                        DeviceId = rule.DeviceId,
                        FacilityId = device.FacilityId,
                        FactoryId = device.FactoryId,
                        DeviceName = device.DeviceName,
                        FactoryName = device.FactoryName,
                        FacilityName = device.FacilityName,
                        RuleProbeName = rule.ProbeName,
                        RuleStatusName = rule.StatusName
                    });
                }
                else
                {
                    _logger.LogInformation($"{warning} {recorddata} {title}恢复正常 {rule.StatusName}气体{gasdata.Value.ToString("0.00")}当前正常");

                    results.Add(new DgaAlarmResult()
                    {
                        Level = (int)MatchRuleLevel.Normal,
                        LevelCN = "正常",
                        AlarmValue = recorddata,
                        ErrorType = $"{title}恢复",
                        ErrorReason = $"{rule.StatusName}气体{gasdata.Value.ToString("0.00")}当前正常",
                        ErrorCode = "#",
                        AlarmTime = DateTime.Now,
                        RuleId = rule.Id,
                        RuleName = rule.RuleName,
                        DeviceId = rule.DeviceId,
                        FacilityId = device.FacilityId,
                        FactoryId = device.FactoryId,
                        DeviceName = device.DeviceName,
                        FactoryName = device.FactoryName,
                        FacilityName = device.FacilityName,
                        RuleProbeName = rule.ProbeName,
                        RuleStatusName = rule.StatusName
                    });
                }
            }

            return results;
        }

    }
    public class TableElement
    {
        public string device { set; get; }
        public string air { set; get; }
        public string vol { set; get; }
        public int threshold { set; get; }

    }
}
