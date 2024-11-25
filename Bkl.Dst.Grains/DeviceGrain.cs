using Bkl.Dst.Interfaces;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bkl.Infrastructure;
using System;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using StackExchange.Redis;
using System.Data;

namespace Bkl.Dst.Grains
{
    public class DeviceGrain : Grain, IDeviceGrain, IRemindable
    {
        private BklConfig _config;
        private IRedisClient _redis;
        private ILogger<DeviceGrain> _logger;
        private IPersistentState<State> _state;
        private IAlarmThresholdRuleGrain _alarmGrain;
        private IAlarmDbStorageSync _storageAlarmGrain;
        private IStatusDbStorageSync _storageStatusGrain;
        private IDisposable _timerStatus;
        private IDisposable _timerLinkage;
        private IAnalysisDGAGrain _dgaAnalysis;

        public class State
        {
            public State()
            {
                DeviceAlarms = new List<DeviceAlarmResult>();
                DeviceStatus = new List<DeviceUpdateStatus>();
                DeviceControlState = new List<DeviceControlState>();
            }
            public List<DeviceUpdateStatus> DeviceStatus { get; set; }
            public List<DeviceControlState> DeviceControlState { get; set; }

            public List<DeviceAlarmResult> DeviceAlarms { get; set; }

            public BklDeviceMetadata Device { get; set; }

        }

        public DeviceGrain(
            [PersistentState("status", BklConstants.RedisProvider)] IPersistentState<State> state,
            IRedisClient redis,
            BklConfig config,
            ILogger<DeviceGrain> logger)
        {
            _config = config;
            _redis = redis;
            _logger = logger;
            _state = state;
        }
        public override async Task OnActivateAsync()
        {
            if (!_state.RecordExists)
            {
                _state.State = new State();
                await _state.WriteStateAsync();
            }
            if (_state.State.DeviceAlarms == null)
            {
                _state.State.DeviceAlarms = new List<DeviceAlarmResult>();
                await _state.WriteStateAsync();
            }
            if (_state.State.Device == null)
            {
                var devId = long.Parse(this.GetPrimaryKeyString().Substring(6));
                using (BklDbContext dbContext = new BklDbContext(this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>().Options))
                {
                    _state.State.Device = dbContext.BklDeviceMetadata.FirstOrDefault(q => q.Id == devId);
                }
                await _state.WriteStateAsync();
            }
            if (_state.State.Device.DeviceType != "ThermalCamera")
                _alarmGrain = this.GrainFactory.GetGrain<IAlarmThresholdRuleGrain>(new AlarmThresholdRuleId { DeviceId = _state.State.Device.Id });
            if (_state.State.Device.DeviceType == "DGA")
            {
                _dgaAnalysis = this.GrainFactory.GetGrain<IAnalysisDGAGrain>($"DGAGPR" + _state.State.Device.Id);
                await _dgaAnalysis.Subscribe(_state.State.Device.Id);
            }

            switch (_state.State.Device.ConnectionType)
            {
                case "rtsp":
                    using (BklDbContext dbContext = new BklDbContext(this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>().Options))
                    {
                        var cams = dbContext.BklThermalCamera.Where(s => s.DeviceId == _state.State.Device.Id).ToList();
                        foreach (var item in cams)
                        {
                            var thermal = this.GrainFactory.GetGrain<IThermalCameraGrain>(new ThermalCameraGrainId(item));
                            await thermal.WeakUp();
                        }
                    }
                    break;
                case "modbus":
                case "modbusip":
                case "modbusrtu":
                case "modbusrtuovertcp":
                case "modbustcp":
                    using (BklDbContext dbContext = new BklDbContext(this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>().Options))
                    {
                        var connId = dbContext.ModbusDevicePair.Where(s => s.DeviceId == _state.State.Device.Id).Select(s => s.ConnectionId).ToList().Distinct();
                        var connUuids = dbContext.ModbusConnInfo.Where(s => connId.Contains(s.Id)).Select(s => s.Uuid).ToList();
                        foreach (var connUuid in connUuids)
                        {
                            var modbus = this.GrainFactory.GetGrain<IModbusGrain>(new ModbusGrainId(connUuid));
                            await modbus.Weakup();
                        }
                    }
                    break;
                case "http":
                    var httpGrain = this.GrainFactory.GetGrain<IHttpGain>(new HttpGainId(_state.State.Device.ConnectionString));
                    await httpGrain.Weakup();
                    break;
                default:
                    break;
            }
            if (_state.State.Device.DeviceType != "ThermalCamera")
            {
                _storageAlarmGrain = this.GrainFactory.GetGrain<IAlarmDbStorageSync>($"mysql-alarm");
                _storageStatusGrain = this.GrainFactory.GetGrain<IStatusDbStorageSync>($"mysql-status");
            }
            await this.RegisterOrUpdateReminder("deviceQueue", TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
        }
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (_timerStatus != null)
            {
                try { _timerStatus.Dispose(); } catch { }
                _timerStatus = null;
            }

            if (_timerLinkage != null)
            {
                try { _timerLinkage.Dispose(); } catch { }
                _timerLinkage = null;
            }


            _timerStatus = this.RegisterTimer(TimerProcessStatus, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _timerLinkage = this.RegisterTimer(TimerLinkage, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));


            return Task.CompletedTask;
        }


        public Task<DeviceUpdateStatus[]> GetStatus()
        {
            return Task.FromResult(_state.State.DeviceStatus.ToArray());
        }
        public Task<DeviceAlarmEntry[]> GetAlarms()
        {
            var group = _state.State.DeviceAlarms.GroupBy(alarmResult =>
             {
                 string key = "";
                 if (alarmResult.SourceType == "ThermalCamera")
                     key = (alarmResult.AlarmProbeName.Empty() ? "" : $"{alarmResult.AlarmProbeName}.") + alarmResult.DeviceStatusName;
                 else
                     key = alarmResult.PairId.ToString();
                 return key;
             });
            var lis = new List<DeviceAlarmEntry>();
            foreach (var gp in group)
            {
                var result = gp.OrderByDescending(s => (int)s.AlarmLevel).First();
                DeviceAlarmEntry entry = new DeviceAlarmEntry
                {
                    Key = gp.Key,
                    Level = result.AlarmLevel.ToString(),
                    StatusName = result.DeviceStatusName,
                    StatusNameCN = result.DeviceStatusNameCN,
                    LastUpdate = result.CreateTime,
                };
                lis.Add(entry);
            }

            return Task.FromResult(lis.ToArray());
        }
        Queue<DeviceUpdateStatus> _queUpdateStatus = new Queue<DeviceUpdateStatus>();
        Queue<DeviceAlarmResult> _queUpdateAlarm = new Queue<DeviceAlarmResult>();
        Queue<BklLinkageAction> _queLinkageAction = new Queue<BklLinkageAction>();
        Queue<DeviceAlarmMatchResults> _queAlarmMatchResults = new Queue<DeviceAlarmMatchResults>();


        public Task UpdateManyStatus(DeviceUpdateStatus[] deviceStatusItem)
        {
            foreach (var newStatus in deviceStatusItem)
            {
                newStatus.DeviceId = _state.State.Device.Id;
                newStatus.FacilityId = _state.State.Device.FacilityId;
                newStatus.FactoryId = _state.State.Device.FactoryId;
                newStatus.GroupName = newStatus.GroupName ?? _state.State.Device.GroupName;

                //_logger.LogInformation($"DeviceUpdateStatus {_state.State.Device.FullPath} {newStatus.ConnUuid} {newStatus.AttributeId} {newStatus.Name} {newStatus.Value}");

                _queUpdateStatus.Enqueue(newStatus);
            }
            return Task.CompletedTask;
            //try
            //{
            //    await _state.ReadStateAsync();
            //    _state.State.DeviceStatus = new List<DeviceUpdateStatus>(_state.State.DeviceStatus.ToArray());
            //    await _state.WriteStateAsync();
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError("write state async " + ex.ToString());
            //}
        }


        public Task UpdateStatus(DeviceUpdateStatus newStatus)
        {
            newStatus.DeviceId = _state.State.Device.Id;
            newStatus.FacilityId = _state.State.Device.FacilityId;
            newStatus.FactoryId = _state.State.Device.FactoryId;
            newStatus.GroupName = newStatus.GroupName ?? _state.State.Device.GroupName;
            _queUpdateStatus.Enqueue(newStatus);
            return Task.CompletedTask;

        }



        public async Task SetAlarm(DeviceAlarmResult alarmResult)
        {
            alarmResult.FactoryId = _state.State.Device.FactoryId;
            alarmResult.FacilityId = _state.State.Device.FacilityId;
            alarmResult.FacilityDetailPosition = _state.State.Device.FullPath;
            _queUpdateAlarm.Enqueue(alarmResult);
        }


        public Task<BklDeviceMetadata> GetDevice()
        {
            return Task.FromResult(_state.State.Device);
        }

        public async Task SetStatus(WriteDeviceStatusRequest writeRequest)
        {
            var item = _state.State.DeviceControlState.FirstOrDefault(s => s.PairId == writeRequest.PairId);
            var newStatus = string.Join("", writeRequest.Data.Select(s => s.ToString("x2")));

            if (item == null)
            {
                item = new DeviceControlState
                {
                    PairId = writeRequest.PairId,
                    AttributeId = writeRequest.AttributeId,
                    LastChanged = DateTime.MinValue,
                    Status = newStatus,
                    SourceId = writeRequest.SourceId,
                };
                _state.State.DeviceControlState.Add(item);
            }
            if (item.SourceId != writeRequest.SourceId || item.Status != newStatus || DateTime.Now.Subtract(item.LastChanged).TotalSeconds > 30)
            {
                item.LastChanged = DateTime.Now;
                item.SourceId = writeRequest.SourceId;
                item.Status = newStatus;
                var decoderGrain = this.GrainFactory.GetGrain<IModbusGrain>(new ModbusGrainId(writeRequest.ConnUuid));
                await decoderGrain.WriteStatus(writeRequest);
            }

        }
        const int ALARM_BATCH = 100;
        const int STATUS_BATCH = 100;
        const int STATUS_SAVE_INTERVAL = 30;
        const int STATUS_PUSH_INTERVAL = 5;
        DateTime _lastLinkageFire = DateTime.MinValue;
        private async Task TimerLinkage(object state)
        {
            if (!_queLinkageAction.TryPeek(out var control) || control == null)
            {
                return;
            }

            if (control.Sleep == 0)
            {
                _queLinkageAction.Dequeue();
                await SetStatus(new WriteDeviceStatusRequest
                {
                    ConnUuid = control.ConnectionUuid,
                    AttributeId = control.AttributeId,
                    PairId = control.PairId,
                    ProtocolName = "",
                    DeviceId = _state.State.Device.Id,
                    Data = new byte[] { byte.Parse(control.ValueHexString) }
                });
                _logger.LogInformation($"LinkageAction {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {control.WriteStatusName} {control.WriteStatusNameCN} {control.ConnectionUuid} {control.AttributeId} {control.ValueCN} {control.ValueHexString}");
            }
            if (control.Sleep > 0)
            {
                var span = DateTime.Now.Subtract(_lastLinkageFire).TotalMilliseconds;
                if (span > control.Sleep)
                {
                    _lastLinkageFire = DateTime.Now;
                    _queLinkageAction.Dequeue();
                    await SetStatus(new WriteDeviceStatusRequest
                    {
                        ConnUuid = control.ConnectionUuid,
                        AttributeId = control.AttributeId,
                        PairId = control.PairId,
                        ProtocolName = "",
                        DeviceId = _state.State.Device.Id,
                        Data = new byte[] { byte.Parse(control.ValueHexString) }
                    });
                    _logger.LogInformation($"LinkageAction queueSize:{_queLinkageAction.Count} timespan:{span} delay:{control.Sleep} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {control.WriteStatusName} {control.WriteStatusNameCN} {control.ConnectionUuid} {control.AttributeId} {control.ValueCN} {control.ValueHexString}");
                }
            }
        }


        private async Task TimerProcessStatus(object obj)
        {
            await _state.ReadStateAsync();
            while (this._queAlarmMatchResults.Count > 0)
            {
                var item = this._queAlarmMatchResults.Dequeue();
                if (item.Actions.Count > 0)
                {
                    item.Actions.ForEach(s => _queLinkageAction.Enqueue(s));
                    item.AlarmResults.ForEach(s =>
                    {
                        s.FactoryId = _state.State.Device.FactoryId;
                        s.FacilityId = _state.State.Device.FacilityId;
                        s.FacilityDetailPosition = _state.State.Device.FullPath;
                        _queUpdateAlarm.Enqueue(s);
                    });
                }
                else
                {
                    item.AlarmResults.ForEach(s =>
                    {
                        s.FactoryId = _state.State.Device.FactoryId;
                        s.FacilityId = _state.State.Device.FacilityId;
                        s.FacilityDetailPosition = _state.State.Device.FullPath;
                        _queUpdateAlarm.Enqueue(s);
                    });
                }
            }
            int process1 = 0;
            while (this._queUpdateAlarm.Count > 0 && process1 < ALARM_BATCH)
            {
                process1++;
                DeviceAlarmResult alarmResult = this._queUpdateAlarm.Dequeue();
                var lastResult = _state.State.DeviceAlarms.FirstOrDefault(s => s.PairId == alarmResult.PairId);
                if (lastResult == null || lastResult.AlarmId == alarmResult.AlarmId && lastResult.AlarmLevel == alarmResult.AlarmLevel)
                {
                    if (lastResult != null)
                    {
                        alarmResult.LastCount = lastResult.LastCount + 1;
                        alarmResult.LastReport = lastResult.LastReport;
                    }
                }
                if (lastResult == null || alarmResult.AlarmLevel != lastResult.AlarmLevel || alarmResult.AlarmId != lastResult.AlarmId)
                {
                    if (_storageAlarmGrain != null && (lastResult == null || lastResult.AlarmLevel != alarmResult.AlarmLevel || lastResult.AlarmValue != alarmResult.AlarmValue))
                    {
                        await _storageAlarmGrain.StoreAlarm(alarmResult);
                    }

                    if (lastResult != null)
                        _state.State.DeviceAlarms.Remove(lastResult);
                    _state.State.DeviceAlarms.Add(alarmResult);
                    await SendAlarmMessage(alarmResult);

                }
                else if (DateTime.Now.Subtract(alarmResult.LastReport).TotalSeconds > 1)
                {
                    alarmResult.LastReport = DateTime.Now;
                    _state.State.DeviceAlarms.Remove(lastResult);
                    _state.State.DeviceAlarms.Add(alarmResult);

                    await SendAlarmMessage(alarmResult);
                }
            }
            Dictionary<long, DeviceUpdateStatus> dic = new Dictionary<long, DeviceUpdateStatus>();
            int process = 0;
            while (this._queUpdateStatus.Count > 0 && process < STATUS_BATCH)
            {
                process++;
                try
                {
                    var newStatus = this._queUpdateStatus.Dequeue();
                    var alarm = this._state.State.DeviceAlarms.FirstOrDefault(s => s.PairId == newStatus.Index);

                    var oldStatus = _state.State.DeviceStatus.FirstOrDefault(s => s.AttributeId == newStatus.AttributeId && s.Index == newStatus.Index);

                    newStatus.Level = alarm == null ? "normal" : (alarm.AlarmValue.ToString() == newStatus.Value ? alarm.AlarmLevel.ToString().ToLower() : "normal");
                    var oldValurStr = oldStatus?.Value;
                    var newValueStr = newStatus?.Value;

                    if (oldStatus == null)
                    {
                        oldStatus = newStatus;
                        _state.State.DeviceStatus.Add(newStatus);
                        await SendStatusMessage(newStatus);
                        await _storageStatusGrain.Store(newStatus);

                    }
                    if (oldStatus.Value != newStatus.Value)
                    {
                        var str = $"触发原因：{_state.State.Device.FacilityName}的{newStatus.NameCN}从{oldStatus.Value}变为{newStatus.Value}";

                        oldStatus.Value = newStatus.Value;
                        oldStatus.Level = newStatus.Level;
                        oldStatus.CreateTime = newStatus.CreateTime;
                        oldStatus.PushTime = newStatus.PushTime;

                        await SendStatusMessage(newStatus);
                        await _storageStatusGrain.Store(newStatus);
                        await SaveStatusChange(newStatus, str);

                    }
                    else
                    {
                        oldStatus.Value = newStatus.Value;
                        oldStatus.Level = newStatus.Level;
                        if (newStatus.CreateTime.Subtract(oldStatus.CreateTime).TotalSeconds > _config.ModbusStatusSaveInterval)
                        {
                            oldStatus.CreateTime = newStatus.CreateTime;
                            await _storageStatusGrain.Store(newStatus);
                        }
                        if (newStatus.PushTime.Subtract(oldStatus.PushTime).TotalSeconds > STATUS_PUSH_INTERVAL)
                        {
                            oldStatus.PushTime = newStatus.PushTime;
                            await SendStatusMessage(newStatus);
                        }
                    }



                    if (_state.State.Device.DeviceType != "ThermalCamera" && oldValurStr != newValueStr)
                    {
                        var results = await _alarmGrain.OnStatusUpdate(newStatus);
                        foreach (var result in results)
                        {
                            if (result.AlarmResults != null && result.AlarmResults.Count > 0)
                            {
                                _queAlarmMatchResults.Enqueue(result);
                            }
                        }
                    }

                    if (!dic.TryAdd(newStatus.Index, newStatus))
                    {
                        dic[newStatus.Index] = oldStatus;
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
            try
            {
                _redis.SetRangeInHash($"DeviceStatus:{this._state.State.Device.Id}", dic.ToDictionary(s => s.Key.ToString(), s => (RedisValue)JsonSerializer.Serialize(s.Value)));
            }
            catch (Exception ex)
            {
                _logger.LogError("WriteDeviceStatusError" + ex.ToString());
            }
            try
            {
                await _state.WriteStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("WriteDeviceStatusError" + ex.ToString());
            }
        }

        private async Task SaveStatusChange(DeviceUpdateStatus newStatus, string str)
        {
            try
            {
                await _storageAlarmGrain.StoreAlarm(new DeviceAlarmResult
                {
                    DataId = newStatus.DataId,
                    PairId = newStatus.Index,
                    DeviceId = newStatus.DeviceId,
                    DeviceStatusName = newStatus.Name,
                    DeviceStatusNameCN = newStatus.NameCN,
                    AttributeId = newStatus.AttributeId,
                    FacilityId = _state.State.Device.FacilityId,
                    FactoryId = _state.State.Device.FactoryId,
                    CreateTime = newStatus.CreateTime,
                    FacilityDetailPosition = _state.State.Device.FullPath,
                    SourceType = "logchange",
                    AlarmExtraInfo = "",
                    AlarmName = str,
                    AlarmProbeName = _state.State.Device.FacilityName + "" + newStatus.NameCN,
                    AlarmValue = double.Parse(newStatus.Value),
                    AlarmId = 0,
                    AlarmMax = double.Parse(newStatus.Value),
                    AlarmMin = double.Parse(newStatus.Value),
                    AlarmLevel = DeviceAlarmType.Warn,
                    Method = "change",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_state.State.Device.FacilityName} {newStatus.NameCN} {ex.ToString()} ");
            }
        }

        private async Task SendAlarmMessage(DeviceAlarmResult alarmResult)
        {


            var sendData = new
            {
                meta = JsonSerializer.Serialize(new
                {
                    DeviceId = alarmResult.DeviceId,
                    FactoryId = alarmResult.FactoryId,
                    FacilityId = alarmResult.FacilityId,
                }),
                status = JsonSerializer.Serialize(new
                {

                    Createtime = alarmResult.CreateTime.ToString("yyyy-MM-dd HH:mm"),
                    CalculateResult = alarmResult.AlarmValue,
                    Level = alarmResult.AlarmLevel.ToString(),
                    RuleName = alarmResult.AlarmName,
                    PairId = alarmResult.PairId,
                    DeviceName = this._state.State.Device.DeviceName,
                    StatusName = alarmResult.DeviceStatusName,
                    StatusNameCN = alarmResult.DeviceStatusNameCN,
                    ProbeName = alarmResult.AlarmProbeName,
                    FacilityDetailPosition = alarmResult.FacilityDetailPosition,
                })
            };
            await SendStreamMessage("alarm", sendData);
        }

        private async Task SendStatusMessage(DeviceUpdateStatus deviceStatusItem)
        {
            var sendData = new
            {
                meta = JsonSerializer.Serialize(new
                {
                    deviceId = deviceStatusItem.DeviceId,
                    factoryId = deviceStatusItem.FactoryId,
                    facilityId = deviceStatusItem.FacilityId
                }),
                status = JsonSerializer.Serialize(new DeviceUpdateStatus[] { deviceStatusItem }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            await SendStreamMessage("state", sendData);
        }

        private async Task SendStreamMessage(string type, object sendData)
        {
            var stateGrain = GrainFactory.GetGrain<IStreamGrain>("signalrStateStream");
            var alarmGrain = GrainFactory.GetGrain<IStreamGrain>("signalrAlarmStream");
            if (type == "state")
                await stateGrain.SendMessage(new SrClientMessage
                {
                    MessageType = type,
                    DeviceId = this._state.State.Device.Id,
                    DataType = sendData.GetType().FullName,
                    Data = JsonSerializer.Serialize(sendData),
                });
            if (type == "alarm")
                await alarmGrain.SendMessage(new SrClientMessage
                {
                    MessageType = type,
                    DeviceId = this._state.State.Device.Id,
                    DataType = sendData.GetType().FullName,
                    Data = JsonSerializer.Serialize(sendData),
                });
        }

    }
}
