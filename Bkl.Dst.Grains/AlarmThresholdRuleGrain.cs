using Bkl.Dst.Interfaces;
using Bkl.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using static Bkl.Models.BklConfig;

namespace Bkl.Dst.Grains
{

    public class AlarmThresholdRuleGrain : Grain, IAlarmThresholdRuleGrain
    {
        public class DeviceAlarmStatus
        {
            public BklDeviceMetadataRef DeviceMetadata { get; set; }

            public List<DeviceAlarmCaculateStatus> DeviceRuleCaculates { get; set; }

            //public Dictionary<string, DeviceAlarmResult> DeviceAlarmResults { get; set; }
        }
        IPersistentState<DeviceAlarmStatus> _state;
        ILogger<AlarmThresholdRuleGrain> _logger;
        List<BklAnalysisRule> _rules;
        IDeviceGrain _deviceGrain;
        DateTime _lastLoadRules = DateTime.MinValue;
        List<BklLinkageAction> _actions;
        Queue<BklLinkageAction> _actionQueues = new Queue<BklLinkageAction>();



        public AlarmThresholdRuleGrain([PersistentState("rule", BklConstants.RedisProvider)] IPersistentState<DeviceAlarmStatus> ruleState, ILogger<AlarmThresholdRuleGrain> logger)
        {
            _state = ruleState;
            _logger = logger;
        }
        public override async Task OnActivateAsync()
        {
            AlarmThresholdRuleId alarmId = this.GetPrimaryKeyString();
            _deviceGrain = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(alarmId.DeviceId));
            if (!_state.RecordExists)
            {
                _state.State.DeviceRuleCaculates = new List<DeviceAlarmCaculateStatus>();
                //_state.State.DeviceAlarmResults = new Dictionary<string, DeviceAlarmResult>();
            }
            await LoadRules();
            await _state.WriteStateAsync();
            // this.RegisterTimer(TimerMatchRules, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1000));
            // this.RegisterTimer(TimerLinkage, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50));
        }
        private async Task LoadRules()
        {
            AlarmThresholdRuleId alarmId = this.GetPrimaryKeyString();
            if (DateTime.Now.Subtract(_lastLoadRules).TotalSeconds > 10)
            {
                var builder = this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>();
                using (BklDbContext context = new BklDbContext(builder.Options))
                {
                    var dev = context.BklDeviceMetadata.Where(s => s.Id == alarmId.DeviceId).FirstOrDefault();
                    _rules = await context.BklAnalysisRule
                            .Where(s => s.DeviceType == dev.DeviceType &&
                            (s.DeviceId == 0 || s.DeviceId == dev.Id)).ToListAsync();
                    var linkageActionIds = _rules.Select(s => s.LinkageActionId).Distinct();
                    _actions = await context.BklLinkageAction.Where(s => linkageActionIds.Contains(s.LinkageActionId)).ToListAsync();
                }
                _lastLoadRules = DateTime.Now;
            }
        }
        public async Task<List<DeviceAlarmMatchResults>> OnStatusUpdate(DeviceUpdateStatus status)
        {
            //_logger.LogInformation($"ProccedAlarm  {_state.State.DeviceMetadata.FullPath} {status.Name} {status.Value}");

            await LoadRules();

            await UpdateSavedStatusSnap(status);

            var lis = SameGroupRulesMatch(status);

            return lis;
        }
        private List<DeviceAlarmMatchResults> SameGroupRulesMatch(DeviceUpdateStatus status)
        {
            var pairIds = this._state.State.DeviceRuleCaculates.Where(s => s.AttributeId == status.AttributeId)
                .Select(s => s.PairId).ToList();

            var groups = _rules.Where(s => pairIds.Contains(s.PairId)).GroupBy(s => s.LinkageActionId);
            List<DeviceAlarmMatchResults> resultsLis = new List<DeviceAlarmMatchResults>();
            foreach (var sameLinkageRules in groups)
            {
                var results = DoRulesMatch(sameLinkageRules);
                results.LinkageActionId = sameLinkageRules.Key;
                resultsLis.Add(results);
            }
            return resultsLis;
        }
        private DeviceAlarmMatchResults DoRulesMatch(IGrouping<long, BklAnalysisRule> sameLinkageRules)
        {
            long linkageActionId = sameLinkageRules.Key;
            var pairIds = sameLinkageRules.Select(s => s.PairId).ToArray();
            var relatedStatus = _state.State.DeviceRuleCaculates.Where(q => pairIds.Contains(q.PairId)).ToList();
            var actions = _actions.Where(s => s.LinkageActionId == linkageActionId).ToList();
             var linkageCondition = string.Join(" ", sameLinkageRules.Select(s => $" {s.StatusName}:{s.Max},").ToArray());
             var actionString = string.Join(" ", actions.Select(s => $"{s.WriteStatusName}={s.ValueHexString}"));
            _logger.LogInformation($"Rules {linkageActionId} {linkageCondition} {actionString}");

            DeviceAlarmMatchResults resultsRet = new DeviceAlarmMatchResults()
            {
                Actions = actions,
                AlarmResults = null
            };

            switch (sameLinkageRules.First().Method)
            {
                case "min":
                case "max":
                case "average":
                case "increament":
                    {
                        List<DeviceAlarmResult> lis = new List<DeviceAlarmResult>();
                        //_logger.LogInformation($"OnAlarm {sameLinkageRules.Key} \r\nRULE:{string.Join(" ", sameLinkageRules.Select(s => $"{s.StatusName} {s.Method}\t {s.Max}").ToArray())} \r\nSTAT:{string.Join(" ", relatedStatus.Select(q => $"{q.StatusName} \t\t {q.CurrentValue}"))}");
                        foreach (var calc in relatedStatus)
                        {
                            if (calc.CurrentStatus.CreateTime.Subtract(calc.LastProcced).TotalSeconds < 120)
                            {
                                continue;
                            }
                            calc.LastProcced = System.DateTime.Now;
                            var results = ValueRuleMatch(calc, sameLinkageRules.ToList());

                            if (results.Count != 0)
                            {
                                lis.AddRange(results);
                                //await SetDeviceAlarm(results, sameLinkageRules);
                            }
                        }
                        resultsRet.AlarmResults = lis;
                        return resultsRet;
                    }
                    break;
                case "AllEqual":
                case "AnyNotEqual":
                case "AllNotEqual":
                case "AnyEqual":
                    {
                        var results = CompareRuleMatch(relatedStatus, sameLinkageRules.ToList());
                        resultsRet.AlarmResults = results;
                        if (results.Count == 0)
                            return resultsRet;
                        if (actions.Count == 0 && results.Count > 0)
                        {
                            //await SetDeviceAlarm(results, sameLinkageRules);
                            return resultsRet;
                        }
                        if (results.Count == 0)
                        {
                            return resultsRet;
                        }
                        //_logger.LogInformation($"OnLinkage {sameLinkageRules.Key} \r\nRULE:{string.Join(" ", sameLinkageRules.Select(s => $"{s.StatusName} {s.Method}\t {s.Max}").ToArray())} \r\nSTAT:{newStateSnap} lastFire {_actionQueues.Count} {results.Count > 0}");
                        //_logger.LogInformation($"LinkageActionsEnqueue QeueuSize:{_actionQueues.Count} Rule:{linkageCondition}");

                        //foreach (var control in actions.OrderBy(s => s.Order))
                        //{
                        //    _actionQueues.Enqueue(new BklLinkageAction
                        //    {
                        //        ConnectionUuid = control.ConnectionUuid,
                        //        AttributeId = control.AttributeId,
                        //        PairId = control.PairId,
                        //        Order = control.Order,
                        //        Sleep = control.Sleep,
                        //        Createtime = DateTime.MinValue,
                        //        ValueCN = control.ValueCN,
                        //        ValueHexString = control.ValueHexString,
                        //        WriteType = control.WriteType,
                        //        WriteStatusName = control.WriteStatusName,
                        //        WriteStatusNameCN = control.WriteStatusNameCN,
                        //    });
                        //}
                    }
                    break;
                default:
                    break;
            }
            return resultsRet;
        }


        private async Task UpdateSavedStatusSnap(DeviceUpdateStatus status)
        {
            //计算统计量
            var dValue = double.Parse(status.Value);
            var calc = _state.State.DeviceRuleCaculates.FirstOrDefault(q => q.PairId == status.Index);
            if (calc == null)
            {
                calc = new DeviceAlarmCaculateStatus();
                calc.PairId = status.Index;
                calc.AttributeId = status.AttributeId;
                calc.StatusName = status.Name;
                calc.CurrentValue = double.NegativeInfinity;
                calc.PreValue = double.NegativeInfinity;

                calc.IncrementalAccumulate = 0;
                calc.Accumulate = 0;

                calc.MaxValue = double.NegativeInfinity;
                calc.MinValue = double.MaxValue;
                calc.AverageValue = 0;
                calc.Count = 0;
                _state.State.DeviceRuleCaculates.Add(calc);
            }
            calc.CurrentStatus = status;
            calc.Count += 1;
            calc.Accumulate += dValue;
            if (calc.CurrentValue != double.NegativeInfinity)
                calc.IncrementalAccumulate += dValue - calc.CurrentValue;
            calc.AverageValue = calc.Accumulate / calc.Count;
            if (dValue > calc.MaxValue)
            {
                calc.MaxValue = dValue;
            }
            if (dValue < calc.MinValue)
            {
                calc.MinValue = dValue;
            }
            calc.PreValue = calc.CurrentValue;
            calc.LastUpdate = DateTime.Now;
            calc.CurrentValue = dValue;
            await _state.WriteStateAsync();
        }


        DateTime lastLinkageFire = DateTime.MinValue;
        private async Task TimerLinkage(object state)
        {
            if (!_actionQueues.TryPeek(out var control) || control == null)
            {
                return;
            }

            if (control.Sleep == 0)
            {
                _actionQueues.Dequeue();
                await _deviceGrain.SetStatus(new WriteDeviceStatusRequest
                {
                    ConnUuid = control.ConnectionUuid,
                    AttributeId = control.AttributeId,
                    PairId = control.PairId,
                    ProtocolName = "",
                    DeviceId = _state.State.DeviceMetadata.Id,
                    Data = new byte[] { byte.Parse(control.ValueHexString) }
                });
                _logger.LogInformation($"LinkageAction {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {control.WriteStatusName} {control.WriteStatusNameCN} {control.ConnectionUuid} {control.AttributeId} {control.ValueCN} {control.ValueHexString}");
            }
            if (control.Sleep > 0)
            {
                var span = DateTime.Now.Subtract(lastLinkageFire).TotalMilliseconds;
                if (span > control.Sleep)
                {
                    lastLinkageFire = DateTime.Now;
                    _actionQueues.Dequeue();
                    await _deviceGrain.SetStatus(new WriteDeviceStatusRequest
                    {
                        ConnUuid = control.ConnectionUuid,
                        AttributeId = control.AttributeId,
                        PairId = control.PairId,
                        ProtocolName = "",
                        DeviceId = _state.State.DeviceMetadata.Id,
                        Data = new byte[] { byte.Parse(control.ValueHexString) }
                    });
                    _logger.LogInformation($"LinkageAction queueSize:{_actionQueues.Count} timespan:{span} delay:{control.Sleep} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {control.WriteStatusName} {control.WriteStatusNameCN} {control.ConnectionUuid} {control.AttributeId} {control.ValueCN} {control.ValueHexString}");
                }
            }
        }
        private List<DeviceAlarmResult> ValueRuleMatch(DeviceAlarmCaculateStatus calc, List<BklAnalysisRule> rs)
        {
            DeviceUpdateStatus rawStatus = calc.CurrentStatus;
            List<DeviceAlarmResult> lis = new List<DeviceAlarmResult>();
            foreach (var rule in rs)
            {
                double max = double.Parse(rule.Max);
                double min = double.Parse(rule.Min);
                double compareValue = 0;
                switch (rule.Method.ToLower())
                {
                    case "max":
                        compareValue = calc.MaxValue;
                        break;
                    case "min":
                        compareValue = calc.MinValue;
                        break;
                    case "average":
                        compareValue = calc.AverageValue;
                        break;
                    case "increment":
                        compareValue = calc.IncrementalAccumulate;
                        break;
                    default:
                        break;
                }
                var rule1 = new DeviceAlarmResult
                {
                    AttributeId = rawStatus.AttributeId,
                    PairId = rawStatus.Index,
                    DeviceId = rawStatus.DeviceId,
                    DeviceStatusName = rawStatus.Name,
                    DeviceStatusNameCN = rawStatus.NameCN,
                    AlarmExtraInfo = rule.ExtraInfo,
                    AlarmName = rule.RuleName,
                    AlarmProbeName = rule.ProbeName,
                    AlarmValue = compareValue,
                    AlarmId = rule.Id,
                    AlarmMax = max,
                    AlarmMin = min,
                    AlarmLevel = (DeviceAlarmType)rule.Level,
                };
                if (calc.CurrentValue > min && calc.CurrentValue <= max)
                {
                    rule1.AlarmLevel = (DeviceAlarmType)rule.Level;

                }
                else
                {
                    rule1.AlarmLevel = DeviceAlarmType.Normal;
                }
                lis.Add(rule1);
            }
            return lis;
        }
        private IEnumerable<(string result, BklAnalysisRule rule, DeviceAlarmCaculateStatus calc)> GetEqualResult(List<DeviceAlarmCaculateStatus> calcs, List<BklAnalysisRule> rs)
        {
            List<(bool, BklAnalysisRule, DeviceAlarmCaculateStatus, DeviceAlarmResult)> lis = new List<(bool, BklAnalysisRule, DeviceAlarmCaculateStatus, DeviceAlarmResult)>();
            var calcMethod = rs.First().Method;
            foreach (var rule in rs)
            {
                bool normal = true;
                var calc = calcs.FirstOrDefault(s => s.PairId == rule.PairId);
                if (calc == null)
                {
                    yield return ("empty", rule, null);
                }
                if (Convert.ToInt32(calc.CurrentValue) == Convert.ToInt32(rule.Max))
                {
                    yield return ("equal", rule, calc);
                }
                else
                {
                    yield return ("notequal", rule, calc);
                }
            }
        }
        private DeviceAlarmResult GetDeviceAlarmResult(DeviceUpdateStatus rawStatus, BklAnalysisRule rule, DeviceAlarmCaculateStatus calc, string calcMethod)
        {
            var rule1 = new DeviceAlarmResult
            {
                DataId = rawStatus.DataId,
                PairId = rawStatus.Index,
                DeviceId = rawStatus.DeviceId,
                DeviceStatusName = rawStatus.Name,
                DeviceStatusNameCN = rawStatus.NameCN,
                AttributeId = rawStatus.AttributeId,
                AlarmExtraInfo = rule.ExtraInfo,
                AlarmName = $"{((DeviceAlarmTypeCN)rule.Level).ToString()}:{rule.RuleName},触发原因:{rawStatus.NameCN}由{calc.PreValue}变为{rawStatus.Value},期望:{rule.Max},当前:{calc.CurrentValue}",// string.Join(" ", rs.Select(s => $"{s.RuleName} {s.Max}").ToArray()) + " " + calcMethod,
                AlarmProbeName = rule.ProbeName,
                AlarmValue = calc.CurrentValue,
                AlarmId = rule.Id,
                AlarmMax = calc.CurrentValue,
                AlarmMin = calc.CurrentValue,
                AlarmLevel = (DeviceAlarmType)rule.Level,
                Method = calcMethod,
            };
            return rule1;
        }
        private List<DeviceAlarmResult> CompareRuleMatch(List<DeviceAlarmCaculateStatus> calcs, List<BklAnalysisRule> rs)
        {

            List<DeviceAlarmResult> lis = new List<DeviceAlarmResult>();
            var calcMethod = rs.First().Method;

            var results = GetEqualResult(calcs, rs);
            _logger.LogInformation($"compare:{calcMethod} {string.Join(" ", results.Select(s => $"{s.calc.StatusName} {s.result} {s.rule.Max}"))}");

            switch (calcMethod)
            {
                case "AllEqual":
                    if (results.All(s => s.result == "equal") && results.Count() == rs.Count)
                    {
                        lis.AddRange(results.Select(s => GetDeviceAlarmResult(s.calc.CurrentStatus, s.rule, s.calc, calcMethod)));
                    }
                    break;
                case "AllNotEqual":
                    if (results.All(s => s.result == "notequal") && results.Count() == rs.Count)
                    {
                        lis.AddRange(results.Select(s => GetDeviceAlarmResult(s.calc.CurrentStatus, s.rule, s.calc, calcMethod)));
                    }
                    break;
                case "AnyEqual":
                    if (results.Any(s => s.result == "equal"))
                    {
                        lis.AddRange(results.Select(s => GetDeviceAlarmResult(s.calc.CurrentStatus, s.rule, s.calc, calcMethod)));
                    }
                    break;
                case "AnyNotEqual":
                    if (results.Any(s => s.result == "notequal"))
                    {
                        lis.AddRange(results.Select(s => GetDeviceAlarmResult(s.calc.CurrentStatus, s.rule, s.calc, calcMethod)));
                    }
                    break;
                default:
                    break;
            }
            return lis;
        }



        private async Task SetDeviceAlarm(List<DeviceAlarmResult> lis, IGrouping<long, BklAnalysisRule> rs)
        {
            foreach (var newAlarm in lis)
            {
                await _deviceGrain.SetAlarm(newAlarm);
                _logger.LogInformation($"PushAlarm {rs.Key} {_state.State.DeviceMetadata.FullPath} {newAlarm.AlarmName} {rs.First().Method} {newAlarm.AlarmLevel}  ");
            }
        }
    }
}
