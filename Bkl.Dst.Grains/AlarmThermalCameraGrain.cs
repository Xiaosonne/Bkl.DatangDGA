using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static Bkl.Dst.Grains.AlarmThermalCameraGrain.State;
using static Bkl.Models.BklConfig;

namespace Bkl.Dst.Grains
{
    public class AlarmThermalCameraGrain : Grain, IAlarmThermalCameraGrain
    {
        public class State
        {
            public class AreaValue
            {
                public AreaValue()
                {
                    Count = 0;
                }
                public DateTime LastProceed { get; set; }

                public int Count { get; set; }
                public double Value { get; set; }
                public double AverageValue { get; set; }
                public double MinValue { get; set; }

                public static AreaValue operator +(AreaValue area, ThermalMetryResult result)
                {
                    area.Count++;
                    area.Value += result.value;
                    area.AverageValue += result.averageTemp;
                    area.Value += result.minTemp;
                    return area;
                }
            }
            public Dictionary<string, AreaValue> Values { get; set; }
        }

        private ILogger<AlarmThermalCameraGrain> _logger;
        private DbContextOptions<BklDbContext> _option;
        private List<BklAnalysisRule> _rules;
        private List<BklLinkageAction> _linkages;
        private Dictionary<string, AreaValue> _areaValues;
        public AlarmThermalCameraGrain(DbContextOptionsBuilder<BklDbContext> dboptionbuilder, ILogger<AlarmThermalCameraGrain> logger)
        {
            _logger = logger;
            _option = dboptionbuilder.Options;
        }
        public override async Task OnActivateAsync()
        {
            AlarmThresholdRuleId alarmId = this.GetPrimaryKeyString();
            //if (!_state.RecordExists)
            //{
            //    _state.State = new State { Values = new Dictionary<string, AreaValue>() };
            //    await _state.WriteStateAsync();
            //}
            _areaValues = new Dictionary<string, AreaValue>();
            using (BklDbContext context = new BklDbContext(_option))
            {
                _rules = context.BklAnalysisRule.Where(s => (s.ProbeName == "ALL_DEVICE" || s.ProbeName == "CURRENT_DEVICE" && s.DeviceId == alarmId.DeviceId) && s.DeviceType == "ThermalCamera").ToList();
                var linkIds = _rules.Select(s => s.LinkageActionId).ToArray();
                _linkages = context.BklLinkageAction.Where(s => linkIds.Contains(s.LinkageActionId)).ToList();
                List<LinkageMatchedItem> lis = new List<LinkageMatchedItem>();
                foreach (var rule in _rules)
                {
                    var linkage = GrainFactory.GetGrain<ILinkageGrain>(new LinkageActionId(rule.LinkageActionId));

                    await linkage.SetMatchedItem(new LinkageMatchedItem
                    {
                        DeviceId = alarmId.DeviceId,
                        RuleId = rule.Id,
                        StatusName = rule.StatusName,
                        CurrentValue = "",
                        TargetValue = JsonSerializer.Serialize(new { type = "inRange", max = rule.Max, min = rule.Min })
                    });
                }

            }
        }
        public async Task ProcessAlarm(ThermalMetryResult status)
        {
            AlarmThresholdRuleId alarmId = this.GetPrimaryKeyString();
            BklAnalysisRule matched = null;
            List<BklAnalysisRule> allMatchedRule = null;
            _areaValues.TryGetValue(status.ruleId.ToString(), out var areaValue);
            if (areaValue == null)
            {
                areaValue = new AreaValue { LastProceed = DateTime.MinValue };
                _areaValues.TryAdd(status.ruleId.ToString(), areaValue);
            }
            else
            {
                areaValue.Value = status.value;
                areaValue.MinValue = status.minTemp;
                areaValue.AverageValue = status.averageTemp;
            }
            _areaValues[status.ruleId.ToString()] = areaValue;

            var value = areaValue.Value;
            var minValue = areaValue.MinValue;
            var aveValue = areaValue.AverageValue;

            if (DateTime.Now.Subtract(areaValue.LastProceed).TotalSeconds > 10)
            {
                areaValue.LastProceed = DateTime.Now;

                List<BklAnalysisRule> matchedRules = new List<BklAnalysisRule>();
                foreach (var rule in _rules)
                {
                    _logger.LogInformation($"MatchingRule {rule.ProbeName} {rule.DeviceId} {rule.Method} {rule.Level} {rule.Min} {rule.Max} {value} {aveValue} {minValue}");
                    if (rule.StatusName == "ALL_AREA" || status.ruleName == rule.StatusName)
                    {
                        if ((rule.Method == "max" && value > double.Parse(rule.Min) && value < double.Parse(rule.Max))
              || (rule.Method == "min" && minValue > double.Parse(rule.Min) && minValue < double.Parse(rule.Max))
              || (rule.Method == "average" && aveValue > double.Parse(rule.Min) && aveValue < double.Parse(rule.Max)))
                        {
                            matchedRules.Add(rule);

                        }
                        else
                        {
                            var linkage = GrainFactory.GetGrain<ILinkageGrain>(new LinkageActionId(rule.LinkageActionId));
                            await linkage.SetMatchedItem(new LinkageMatchedItem
                            {
                                DeviceId = alarmId.DeviceId,
                                RuleId = rule.Id,
                                CurrentValue = JsonSerializer.Serialize(areaValue),
                                Matched = false,
                            });
                        }
                    }
                }

                foreach (var rule in matchedRules)
                {
                    _logger.LogInformation($"MatchedRule {rule.ProbeName} {rule.DeviceId} {rule.Method} {rule.Level} {rule.Min} {rule.Max} {value} {aveValue} {minValue}");

                    if (rule.Level == (int)DeviceAlarmType.Linkage)
                    {
                        var linkage = GrainFactory.GetGrain<ILinkageGrain>(new LinkageActionId(rule.LinkageActionId));
                        await linkage.SetMatchedItem(new LinkageMatchedItem
                        {
                            DeviceId = alarmId.DeviceId,
                            RuleId = rule.Id,
                            CurrentValue = JsonSerializer.Serialize(areaValue),
                            Matched = true,
                        });
                    }
                    else
                    {
                        var deviceGrain = GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(status.deviceId));
                        var alarmresult = new DeviceAlarmResult
                        {
                            SourceType = "ThermalCamera",
                            DeviceId = status.deviceId,
                            DeviceStatusName = "temperature",
                            AlarmExtraInfo = status.ruleName,
                            AlarmName = rule.RuleName,
                            AlarmProbeName = rule.ProbeName,
                            AlarmValue = rule.Method == "max" ? value : (rule.Method == "min" ? minValue : aveValue),
                            AlarmId = rule.Id,
                            AlarmMax = double.Parse(rule.Max),
                            AlarmMin = double.Parse(rule.Min),
                            AlarmLevel = (DeviceAlarmType)rule.Level,
                            DeviceStatusNameCN = "温度规则" + status.ruleName,
                        };
                        await deviceGrain.SetAlarm(alarmresult);
                    }
                }
                //var rulesOfDevice = _rules.Where(s => s.DeviceId == status.deviceId && s.ProbeName == status.ruleName).ToList();
                //if (rulesOfDevice.Count == 0)
                //{
                //    rulesOfDevice = _rules.Where(s => s.DeviceId == status.deviceId && string.IsNullOrEmpty(s.ProbeName)).ToList();
                //}
                //if (rulesOfDevice.Count == 0)
                //{
                //    rulesOfDevice = _rules.Where(s => s.DeviceId == 0).ToList();
                //}

                //allMatchedRule = rulesOfDevice
                //   .Where(rule => )
                //   .OrderByDescending(rule => rule.Level).ToList();

                //matched = allMatchedRule.FirstOrDefault();
                //if (matched != null)
                //{
                //    _state.State.Values[status.ruleName] = new AreaValue();
                //    _state.State.Values[status.ruleName] = _state.State.Values[status.ruleName] + status;

                //    var deviceGrain = GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(status.deviceId));
                //    var alarmresult = new DeviceAlarmResult
                //    {
                //        SourceType = "ThermalCamera",
                //        DeviceId = status.deviceId,
                //        DeviceStatusName = "temperature",
                //        AlarmExtraInfo = status.ruleName,
                //        AlarmName = matched.RuleName,
                //        AlarmProbeName = matched.ProbeName,
                //        AlarmValue = matched.Method == "max" ? value : (matched.Method == "min" ? minValue : aveValue),
                //        AlarmId = matched.Id,
                //        AlarmMax = double.Parse(matched.Max),
                //        AlarmMin = double.Parse(matched.Min),
                //        AlarmLevel = (DeviceAlarmType)matched.Level,
                //    };
                //    await deviceGrain.SetAlarm(alarmresult);
                //    //todo Alarm Type To Normal
                //}
            }

        }
    }
}