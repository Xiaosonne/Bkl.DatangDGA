using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Bkl.Dst.Grains
{

    public record ConnJson(string brandName, string visible, string thermal);

    public class ThermalCameraGrain : Grain, IThermalCameraGrain, IRemindable
    {
        private BklThermalCamera _camera;
        private BklDeviceMetadata _device;
        private BklDeviceMetadataRef _deviceMeta;
        private IDeviceGrain _deviceGrain;
        private IAlarmThermalCameraGrain _alarmGrain;
        private IStreamGrain _streamGrain;
        private IGrainReminder _loginReminder;
        private IStatusDbStorageSync _storageStatusGrain;
        private ILogger<ThermalCameraGrain> _logger;
        private IRedisClient _redis;
        private BklConfig _config;
        private DateTime _lastWriteRedis = DateTime.Now;
        private DateTime _lastWriteMysql = DateTime.Now;

        private IGrainReminder _keepLoginReminder;

        private IDisposable _timerHandle;

        private ThermalCameraISAPI _cameraSdk;
        private UniviewHelper _cameraUniSdk;

        private ConnJson _connJson;
        private Dictionary<int, ThermalMetryResult> _thermalTemperature = new Dictionary<int, ThermalMetryResult>();
        private List<ThermalMeasureRule> _thermalRule = null;

        public ThermalCameraGrain(ILogger<ThermalCameraGrain> logger, BklConfig config, IRedisClient redis)
        {
            _logger = logger;
            _redis = redis;
            _config = config;
        }
        public override async Task OnActivateAsync()
        {
            ThermalCameraGrainId grainId = this.GetPrimaryKeyString();
            long camid = grainId.CamId;
            _logger.LogInformation($"OnActivateAsync {grainId} ");
            var builder = this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>();
            using (BklDbContext context = new BklDbContext(builder.Options))
            {
                _camera = context.BklThermalCamera.Where(s => s.Id == grainId.CamId)
                     .AsNoTracking()
                     .FirstOrDefault();
                if (_camera != null)
                {
                    _device = context.BklDeviceMetadata.Where(s => s.Id == _camera.DeviceId).FirstOrDefault();
                    _deviceMeta = _device;
                    _connJson = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<ConnJson>(str), _device.ConnectionString);
                }

            }
            if (_camera == null)
            {
                _logger.LogError(" camera id find null ");
                return;
            }


            _deviceGrain = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(_deviceMeta));

            _alarmGrain = this.GrainFactory.GetGrain<IAlarmThermalCameraGrain>(new AlarmThresholdRuleId() { DeviceId = _deviceMeta.Id });

            _streamGrain = GrainFactory.GetGrain<IStreamGrain>("signalrStateStream");

            _loginReminder = await this.RegisterOrUpdateReminder("loginTimer", TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));

            _storageStatusGrain = this.GrainFactory.GetGrain<IStatusDbStorageSync>($"mysql-thermal-status");
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            _logger.LogInformation($"{_camera.Ip} reminder:{reminderName} {status.FirstTickTime} {status.CurrentTickTime}");
            if (_timerHandle == null)
            {
                if (_connJson == null || _connJson.brandName == null || _connJson.brandName == "海康")
                    _timerHandle = this.RegisterTimer(OnRealtimeThermal, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));
                if (_connJson != null && _connJson.brandName == "宇视")
                    _timerHandle = this.RegisterTimer(OnRealtimeUniThermal, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

            }
            return Task.CompletedTask;
        }
        async Task OnRealtimeUniThermal(object state)
        {
            _cameraUniSdk = new UniviewHelper(_camera.Ip, _camera.Port, _camera.Account, _camera.Password);
            try
            {
                var resp = await _cameraUniSdk.GetTemperatureValues();
                resp.Response.Data.TemperatureValueInfoList.Select(ss =>
                {
                    ThermalMetryResult temp = new ThermalMetryResult();
                    temp.deviceId = _device.Id;
                    temp.facilityId = _device.FacilityId;
                    temp.factoryId = _device.FactoryId;
                    temp.ruleId = ss.ID;
                    temp.ruleName = "";
                    temp.lowPoints = new double[] { 0, 0 };
                    temp.highPoints = new double[] { 0, 0 };
                    temp.minTemp = ss.MinTemperature;
                    temp.value = ss.MaxTemperature;
                    temp.averageTemp = ss.AverageTemperature;
                    temp.time = DateTime.Now.UnixEpoch();
                    return temp;
                })
                .ToList()
                .ForEach(temp =>
                {
                    _logger.LogInformation($"NewStatus {_camera.Ip} {_camera.DeviceId} {temp.ruleId} {temp.ruleName} {temp.value}");
                    ProcessCallback(temp);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        async Task OnRealtimeThermal(object state)
        {

            _cameraSdk = new ThermalCameraISAPI(_camera.Ip, _camera.Port, _camera.Account, _camera.Password);
            _logger.LogInformation($"ReadBegin {_camera.Ip} {DateTime.Now}");
            Dictionary<int, ThermalMetryResult> dic = new Dictionary<int, ThermalMetryResult>();
            try
            {
                var resp = await _cameraSdk.ReadThermalMetryOnceAsync();
                if (resp == null)
                    return;

                if (_thermalRule == null)
                    _thermalRule = await _cameraSdk.GetThermalRules();

                foreach (var data in resp.Data.TempRules)
                {
                    var region = _thermalRule.FirstOrDefault(s => s.ruleId == data.id);
                    var temp = new ThermalMetryResult();
                    temp.ruleId = Convert.ToByte(data.id);
                    temp.ruleName = region.ruleName ?? $"rule{data.id}";
                    temp.regionType = region == null ? 0 : region.regionType;
                    temp.averageTemp = data.averageTemperature;
                    temp.value = data.maxTemperature;
                    temp.minTemp = data.minTemperature;
                    temp.deviceId = _deviceMeta.Id;
                    temp.facilityId = _deviceMeta.FacilityId;
                    temp.factoryId = _deviceMeta.FactoryId;

                    temp.highPoints = new double[] { data.MaxTemperaturePoint.positionX, data.MaxTemperaturePoint.positionY };

                    temp.lowPoints = new double[] { data.MinTemperaturePoint.positionX, data.MinTemperaturePoint.positionY };

                    temp.time = DateTime.Now.UnixEpoch();
                    _logger.LogInformation($"NewStatus {_camera.Ip} {_camera.DeviceId} {temp.ruleId} {temp.ruleName} {temp.value}");

                    ProcessCallback(temp);

                    _logger.LogInformation($"ProcessOver {_camera.Ip} {_camera.DeviceId} {temp.ruleId} {temp.ruleName} {temp.value}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {_camera.Ip}   " + ex.ToString());
            }
            _logger.LogInformation($"EndRead {_camera.Ip}  {DateTime.Now}");
        }

        void ProcessCallback(ThermalMetryResult data)
        {
            _logger.LogInformation($"{_camera.Ip} {data.ruleId} {data.ruleName} {data.value}");

            SendNotify(_deviceMeta, data);

            if (!_thermalTemperature.TryAdd(data.ruleId, data))
            {
                _thermalTemperature[data.ruleId] = data;
            }

            if (DateTime.Now.Subtract(_lastWriteRedis).TotalSeconds > 10)
            {
                _lastWriteRedis = DateTime.Now;

                WriteToRedis();
            }

            if (DateTime.Now.Subtract(_lastWriteMysql).TotalSeconds > _config.ThermalStatusSaveInterval)
            {
                _lastWriteMysql = DateTime.Now;

                WriteToDatabase();
            }

            try
            {
                _alarmGrain.ProcessAlarm(data);
            }
            catch (Exception ex)
            {
                _logger.LogError("CameraWriteDeviceAlarm " + ex.ToString());
            }

            //count++;
            //if (DateTime.Now.Subtract(last).TotalSeconds > 30)
            //{
            //    _logger.LogInformation($"thermal:{device.Id} last:{last} totalCount:{count} ruleName:{temp.ruleName} value:{temp.value}");
            //    last = DateTime.Now;
            //}
        }

        private void WriteToRedis()
        {
            try
            {
                var dic = _thermalTemperature.Values.Select(temp => new DeviceUpdateStatus
                {
                    Name = "temperature",
                    CreateTime = temp.time.UnixEpochBack(),
                    NameCN = "温度",
                    GroupName = temp.ruleName,
                    Index = temp.ruleId,
                    Type = "dt_float",
                    Unit = "℃",
                    UnitCN = "摄氏度",
                    Value = temp.value.ToString(),
                    DeviceId = temp.deviceId,
                    FacilityId = temp.facilityId,
                    FactoryId = temp.factoryId,
                }).ToDictionary(s => s.Index.ToString(),
                      s => (RedisValue)JsonSerializer.Serialize(s));

                _redis.SetRangeInHash($"DeviceStatus:{_deviceMeta.Id}", dic);
            }
            catch (Exception ex)
            {
                _logger.LogError("CameraWriteDeviceRedis " + ex.ToString());
            }
        }

        private void WriteToDatabase()
        {
            try
            {
                var vals = _thermalTemperature.Values.Select(temp => new DeviceUpdateStatus
                {
                    Name = "temperature",
                    CreateTime = temp.time.UnixEpochBack(),
                    NameCN = "温度",
                    GroupName = temp.ruleName,
                    Index = temp.ruleId,
                    Type = "dt_float",
                    Unit = "℃",
                    UnitCN = "摄氏度",
                    Value = temp.value.ToString(),
                    DeviceId = temp.deviceId,
                    FacilityId = temp.facilityId,
                    FactoryId = temp.factoryId,
                }).ToArray();
                foreach (var item in vals)
                {
                    _storageStatusGrain.Store(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("CameraWriteDeviceMysql " + ex.ToString());
            }
        }

        private async void SendNotify(BklDeviceMetadataRef device, ThermalMetryResult temp)
        {
            temp.deviceId = device.Id;
            temp.facilityId = device.FacilityId;
            temp.factoryId = device.FactoryId;
            var sendData = new
            {
                meta = JsonSerializer.Serialize(new
                {
                    deviceId = device.Id,
                    factoryId = device.FactoryId,
                    facilityId = device.FacilityId
                }),
                status = JsonSerializer.Serialize(temp)
            };
            try
            {
                await _streamGrain.SendMessage(new SrClientMessage
                {
                    DeviceId = device.Id,
                    MessageType = "state",
                    DataType = sendData.GetType().FullName,
                    Data = JsonSerializer.Serialize(sendData),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"sendMessage    {ex.ToString()}");
            }
        }

        public Task<DataResponse<Dictionary<int, ThermalMetryResult>>> GetStatus()
        {

            return Task.FromResult(new DataResponse<Dictionary<int, ThermalMetryResult>>
            {
                data = _thermalTemperature
            });
        }

        public async Task<ThermalSetRuleResponse> UpdateRule(ThermalMeasureRule rule)
        {
            try
            {
                var resp = await _cameraSdk.SetThermalRule(rule);
                return new ThermalSetRuleResponse { success = false, error = 100, };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return new ThermalSetRuleResponse { success = false, error = 100 };
        }
        public Task<DataResponse<List<ThermalXYTemperature>>> GetThermalJPEG(int x, int y, int padding)
        {
            throw new Exception("not complete");
        }

        public async Task<DataResponse<List<ThermalMeasureRule>>> GetMeasureRules()
        {
            try
            {
                var resp = await _cameraSdk.GetThermalRules();

                return new DataResponse<List<ThermalMeasureRule>> { data = resp };
            }
            catch (Exception ex)
            {
                return new DataResponse<List<ThermalMeasureRule>>
                {
                    success = false,
                    error = 100,
                    msg = ex.Message
                };
            }
        }

        public Task WeakUp()
        {
            _logger.LogInformation($"thermal camera weakup {_camera.Ip} {_camera.DeviceId}");
            return Task.CompletedTask;
        }
    }
}
