using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bkl.Dst.Grains
{

    public class AnalysisDGAGrain : Grain, IAnalysisDGAGrain, IRemindable
    {
        public class State
        {
            public DateTime AGPRLastTime { get; set; }

            public DateTime RGPRLastTime { get; set; }

            public HashSet<long> BindingDevices { get; set; }

            public Dictionary<string, double> AGPRMap { get; set; }

            public Dictionary<string, double> RGPRMap { get; set; }

            public BklDGAStatus DGAGasStatus { get; set; }
        }

        private BklDGAConfig _config;
        //private IRedisClient _redisClient;
        private ILogger<AnalysisDGAGrain> _logger;
        private DbContextOptionsBuilder<BklDbContext> _option;
        private long deviceId;
        private IPersistentState<State> _state;

        public AnalysisDGAGrain(DbContextOptionsBuilder<BklDbContext> option, ILogger<AnalysisDGAGrain> logger, IRedisClient redisClient, BklDGAConfig config, [PersistentState("dgagpr", BklConstants.RedisProvider)] IPersistentState<State> state)
        {
            _config = config;
            _option = option;
            //_redisClient = redisClient;
            _logger = logger;
            _state = state;
        }
        public override async Task OnActivateAsync()
        {
            if (!_state.RecordExists)
            {
                _state.State = new State
                {
                    BindingDevices = new HashSet<long>(),
                    RGPRLastTime = DateTime.Now,
                    AGPRLastTime = DateTime.Now
                };
                await _state.WriteStateAsync();
            }
            deviceId = long.Parse(this.GetPrimaryKeyString().TrimStart("DGAGPR".ToCharArray()));
            await this.RegisterOrUpdateReminder("agprProcess", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(_config.AbsoluteRateReminderInterval));
            await this.RegisterOrUpdateReminder("rgprProcess", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(_config.RelativeRateReminderInterval));
            await this.RegisterOrUpdateReminder("ttProcess", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(_config.GasProductionInterval));
        }

        async Task ProcessAGPR()
        {
            try
            {


                var stop = DateTime.Now;

                if (DateTime.Now.Subtract(_state.State.AGPRLastTime).TotalSeconds < _config.AbsoluteRateCalculateInterval)
                    return;
                _logger.LogInformation($"RelativeRate from:{_state.State.RGPRLastTime} ");
                var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                _logger.LogInformation($"AbsoluteRate from:{start},{start.UnixEpoch()} to:{stop},{stop.UnixEpoch()} ");
                using (BklDbContext context = new BklDbContext(_option.Options))
                {
                    var prods = await DGAGPRHelper.CaculateAGPR(context, _config, start, stop, deviceId);
                    _state.State.AGPRMap = prods.ToDictionary(s => s.GasName, s => s.Rate);
                    _logger.LogInformation($"AbsoluteRate from:{start},{start.UnixEpoch()} to:{stop},{stop.UnixEpoch()} count:{prods.Count}");
                }
                _state.State.AGPRLastTime = DateTime.Now;
                await _state.WriteStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

        }

        async Task ProcessRGPR()
        {

            try
            {
                var stop = DateTime.Now;

                if (DateTime.Now.Subtract(_state.State.RGPRLastTime).TotalSeconds < _config.RelativeRateCalculateInterval)
                    return;
                _logger.LogInformation($"RelativeRate from:{_state.State.RGPRLastTime} ");
                var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                using (BklDbContext context = new BklDbContext(_option.Options))
                {
                    var prods = await DGAGPRHelper.CalculateRGPR(context, _config, start, stop, deviceId);
                    _state.State.RGPRMap = prods.ToDictionary(s => s.GasName, s => s.Rate);
                    _logger.LogInformation($"RelativeRate from:{start},{start.UnixEpoch()} to:{stop},{stop.UnixEpoch()} count:{prods.Count}");
                }
                _state.State.RGPRLastTime = DateTime.Now;
                await _state.WriteStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
        async Task ProcessTT()
        {
            try
            {
                var devicegrain = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(deviceId));
                var status = await devicegrain.GetStatus();
                var device = await devicegrain.GetDevice();
                using (BklDbContext context = new BklDbContext(_option.Options))
                {
                    var newStatus = DGATTHelper.CalculateThreeTatio(context, new DeviceStatus
                    {
                        did = device.Id,
                        faid = device.FacilityId,
                        fid = device.FactoryId,
                        time = DateTime.Now.UnixEpoch(),
                        status = status.Select(s => new DeviceStatusItem
                        {
                            name = s.Name,
                            value = s.Value,
                        }).ToArray()
                    });
                    if (_state.State.DGAGasStatus == null || _state.State.DGAGasStatus.Id != newStatus.Id)
                        _state.State.DGAGasStatus = newStatus;
                }
                await _state.WriteStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
        /// <summary>
        /// 计算相对产气率和绝对产气率
        /// </summary> 



        static void RateSaveToRedis(IRedisClient redisClient, List<BklDGAGasProduction> prods)
        {
            if (prods.Count == 0)
                return;
            foreach (var item in prods.GroupBy(s => s.DeviceRelId))
            {
                Dictionary<string, double> dicTatio = new Dictionary<string, double>();
                foreach (var rts in item.GroupBy(s => s.RateType))
                {
                    redisClient.SetRangeInHash($"{rts.Key}:{item.Key}", rts.ToDictionary(q => q.GasName, q => (RedisValue)q.Rate));
                }
            }
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case "agprProcess":
                    await ProcessAGPR();
                    break;
                case "rgprProcess":
                    await ProcessRGPR();
                    break;
                case "ttProcess":
                    await ProcessTT();
                    break;
                default:
                    break;
            }
        }

        public async Task Subscribe(long deviceId)
        {
            _state.State.BindingDevices.Add(deviceId);
            await _state.WriteStateAsync();
        }

        public Task<DeviceStatusItem[]> GetStatus()
        {
            var state = _state.State.AGPRMap;
            string errorReason = "";
            string errorSample = "";
            if (_state.State.DGAGasStatus != null)
            {
                foreach (var item in DGATTHelper.threeTatioCode)
                {
                    bool b1 = item.Key.Item1 == _state.State.DGAGasStatus.C2H2_C2H4_Code || item.Key.Item1 < 0 && _state.State.DGAGasStatus.C2H2_C2H4_Code <= Math.Abs(item.Key.Item1);
                    bool b2 = item.Key.Item2 == _state.State.DGAGasStatus.CH4_H2_Code || item.Key.Item2 < 0 && _state.State.DGAGasStatus.CH4_H2_Code <= Math.Abs(item.Key.Item2);
                    bool b3 = item.Key.Item3 == _state.State.DGAGasStatus.C2H4_C2H6_Code || item.Key.Item3 < 0 && _state.State.DGAGasStatus.C2H4_C2H6_Code <= Math.Abs(item.Key.Item3);

                    if (b1 && b2 && b3)
                    {
                        errorReason = item.Value.Item1;
                        errorSample = item.Value.Item2;
                    }
                }
            }


            return Task.FromResult(new DeviceStatusItem[] {
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.C2H2_C2H4_Code),value=_state.State.DGAGasStatus.C2H2_C2H4_Code.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.C2H2_C2H4_Tatio),value=_state.State.DGAGasStatus.C2H2_C2H4_Tatio.ToString() },

                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.C2H4_C2H6_Code),value=_state.State.DGAGasStatus.C2H4_C2H6_Code.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.C2H4_C2H6_Tatio),value=_state.State.DGAGasStatus.C2H4_C2H6_Tatio.ToString() },

                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.CH4_H2_Code),value=_state.State.DGAGasStatus.CH4_H2_Code.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.CH4_H2_Tatio),value=_state.State.DGAGasStatus.CH4_H2_Tatio.ToString() },

                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.CO2_CO_Tatio),value=_state.State.DGAGasStatus.CO2_CO_Tatio.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.O2_N2_Tatio),value=_state.State.DGAGasStatus.O2_N2_Tatio.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.C2H2_H2_Tatio),value=_state.State.DGAGasStatus.C2H2_H2_Tatio.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.C2H6_CH4_Tatio),value=_state.State.DGAGasStatus.C2H6_CH4_Tatio.ToString() },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.ThreeTatio_Code),value=_state.State.DGAGasStatus.ThreeTatio_Code.ToString() },
                new DeviceStatusItem { name="TotHyd_AGPR",value=(_state.State.AGPRMap!=null&&_state.State.AGPRMap.TryGetValue("TotHyd",out var val))?val.ToString():"0" },
                new DeviceStatusItem { name="TotHyd_RGPR",value=(_state.State.RGPRMap!=null&&_state.State.RGPRMap.TryGetValue("TotHyd",out var val1))?val1.ToString():"0" },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.TotHyd_Inc),value=_state.State.DGAGasStatus.TotHyd_Inc.ToString() },


                new DeviceStatusItem { name="CmbuGas_AGPR",value=(_state.State.AGPRMap!=null&&_state.State.AGPRMap.TryGetValue("CmbuGas",out var val2))?val2.ToString():"0" },
                new DeviceStatusItem { name="CmbuGas_RGPR",value=(_state.State.RGPRMap!=null&&_state.State.RGPRMap.TryGetValue("CmbuGas",out var val3))?val3.ToString():"0" },
                new DeviceStatusItem { name=nameof(_state.State.DGAGasStatus.CmbuGas_Inc),value=_state.State.DGAGasStatus.CmbuGas_Inc.ToString() },


                new DeviceStatusItem{name=nameof(errorReason),value=errorReason},
                new DeviceStatusItem{name=nameof(errorSample),value=errorSample},
            });
        }
    }
}
