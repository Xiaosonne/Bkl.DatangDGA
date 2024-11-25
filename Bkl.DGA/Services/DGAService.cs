using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Reactive.Subjects;
using Bkl.Infrastructure;
using System.Collections.Generic;
using NetMQ.Sockets;
using Microsoft.Extensions.Configuration;
using NetMQ;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using System.Reactive.Linq;
using System.Text.Json;
using System.Reactive;
using System.Reactive.Disposables;
using static NetMQ.NetMQSelector;
using System.Security.Cryptography;
using Bkl.Dst.Interfaces;
using Bkl.DGA.Services;



public partial class DGAService : BackgroundService
{
    private IRedisClient _redis;
    private Channel<ChannelData<FeatureGasAlarmService, AlarmServiceFeatureGas[]>> _gasAlarmSource;
    private IConfiguration _config;
    Channel<ChannelData<DGAService, DeviceState[]>> _deviceStatus;
    private ILogger<DGAService> _logger;
    List<DGAAnalysis> _analysis = new List<DGAAnalysis>();
    private Task _pushTask;
    private IServiceScope _scope;
    private DbContextOptions<BklDbContext> _contextOpt;
    private IServiceProvider _serviceProvider;
    public DGAService(Channel<ChannelData<DGAService, DeviceState[]>> devicequeue,
                 Channel<ChannelData<FeatureGasAlarmService, AlarmServiceFeatureGas[]>> gasAlarmSource,
        IServiceProvider service,
    ILogger<DGAService> logger,
        IConfiguration config)
    {
        _gasAlarmSource = gasAlarmSource;
        _config = config;
        _deviceStatus = devicequeue;
        _logger = logger;
        _serviceProvider = service;
        _scope = service.CreateScope();
        _contextOpt = _scope.ServiceProvider.GetService<DbContextOptions<BklDbContext>>();
    }
    static string[] cmbugas = new string[] { "H2", "CO", "CH4", "C2H2", "C2H4", "C2H6" };
    static string[] tothyd = new string[] { "CH4", "C2H2", "C2H4", "C2H6" };
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Subject<DeviceState> allStatus = new Subject<DeviceState>();
        PublisherSocket pubsocket = new PublisherSocket(_config.GetSection("AppSetting:DGAStateServer").Value);
        PublisherSocket pubAlarmsocket = new PublisherSocket(_config.GetSection("AppSetting:DGAAlarmServer").Value);

        Subject<DGAModel.DgaAlarmResult> alarmSource = new Subject<DGAModel.DgaAlarmResult>();
        Subject<DGAModel.DgaPushData> stateSource = new Subject<DGAModel.DgaPushData>();
        var alarmStream = alarmSource.Publish();
        alarmSource.Where(s => s != DGAModel.DgaAlarmResult.Normal)
            .Window(TimeSpan.FromSeconds(10))
            .Subscribe(s =>
            {
                s.GroupBy(s => $"{s.DeviceId}{s.ErrorType}")
                .SelectMany(t => t.LastAsync())
                .Subscribe(item =>
                {
                    Console.WriteLine($"alarm {DateTime.Now} {item.DeviceId} {item.AlarmTime} {item.AlarmValue} {item.ErrorType}");
                    pubAlarmsocket.SendMoreFrame("dgaalarm").SendFrame(item.ToJson());
                });
            });
        stateSource.Window(TimeSpan.FromSeconds(10))
               .Select(s => s.ToList())
               .Subscribe(tsg =>
               {
                   tsg.Subscribe(t =>
                   {
                       var samedevice = t.GroupBy(s => s.DeviceId)
                            .Select(s => s.OrderByDescending(n => n.Createtime).FirstOrDefault())
                            .ToList();
                       foreach (var rs in samedevice)
                       {
                           pubsocket.SendMoreFrame("dga").SendFrame(rs.ToJson());
                       }
                   });
               });
        while (stoppingToken.IsCancellationRequested == false)
        {
            var channeldata = await _deviceStatus.Reader.ReadAsync();
            try
            {
                var data = channeldata.Data;
                if (data.Length == 0)
                    continue;
                var state = data.First();
                var deviceId = state.DeviceId;
                var ana = _analysis.FirstOrDefault(s => s.DeviceId == deviceId);
                if (ana == null)
                {
                    using (var _context = new BklDbContext(_contextOpt))
                    {
                        ana = new DGAAnalysis() { DeviceId = deviceId, FacilityId = state.FacilityId, FactoryId = state.FactoryId };
                        ana.LoadFromDatabase(_context);
                        //重启后计算三比值
                        try
                        {
                            string[] arr = new string[] { "CH4", "C2H4", "C2H6", "C2H2", "H2" };
                            foreach (var name in arr)
                            {
                                ana.SetThreeSate(new DeviceState
                                {
                                    Name = name,
                                    Value = ana.ReadGasValue(name).ToString(),
                                });
                            }
                            alarmSource.OnNext(ana.GetThreeCodeAlarmResults());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.ToString());
                        }

                        _analysis.Add(ana);
                    }
                }
                if (DateTime.Now.Subtract(ana.LoadTime).TotalSeconds > 120)
                {
                    using (var _context = new BklDbContext(_contextOpt))
                    {
                        ana.LoadFromDatabase(_context);
                        _logger.LogInformation($"ReloadDeviceContext {deviceId} {DateTime.Now}");
                    }
                }
                try
                {

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }

                try
                {

                    var featureGas = ana.YearsGasData.Select(s =>
                    {
                        return new AlarmServiceFeatureGas
                        {
                            DeviceMeta = new DeviceMeta { DeviceId = ana.Device.Id, FacilityId = ana.Device.FacilityId, FactoryId = ana.Device.FactoryId },
                            GasName = s.GasName,
                            GasData = s.MonthData[DateTime.Now.Month - 1].DaysGasData.Take(DateTime.Now.Day).ToArray(),
                        };
                    })
                        .ToArray();
                    await _gasAlarmSource.Writer.WriteAsync(new ChannelData<FeatureGasAlarmService, AlarmServiceFeatureGas[]>
                    {
                        Data = featureGas
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                if (data.All(s => s.Name != "CmbuGas"))
                    ana.OnNewState(new DeviceState
                    {
                        Name = "CmbuGas",
                        NameCN = "总可燃",
                        Unit = "ppm",
                        UnitCN = "ppm",
                        Value = data.Where(s => cmbugas.Contains(s.Name)).Select(s => double.Parse(s.Value)).Sum().ToString(),
                        DeviceId = state.DeviceId,
                        FacilityId = state.FacilityId,
                        FactoryId = state.FactoryId,
                        CreateTime = DateTime.Now,
                        ProtocolName = state.ProtocolName,
                    });
                if (data.All(s => s.Name != "TotHyd"))
                    ana.OnNewState(new DeviceState
                    {
                        Name = "TotHyd",
                        NameCN = "总烃",
                        Unit = "ppm",
                        UnitCN = "ppm",
                        Value = data.Where(s => tothyd.Contains(s.Name)).Select(s => double.Parse(s.Value)).Sum().ToString(),
                        DeviceId = state.DeviceId,
                        FacilityId = state.FacilityId,
                        FactoryId = state.FactoryId,
                        CreateTime = DateTime.Now,
                        ProtocolName = state.ProtocolName,
                    });
                foreach (var item in data)
                {

                    try
                    {
                        if (item.CreateTime == DateTime.MinValue)
                            item.CreateTime = DateTime.Now;
                        ana.OnNewState(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }

                }
                try
                {
                    var da = ana.Serialized();
                    stateSource.OnNext(da);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }

                try
                {
                    foreach (var item in ana.GetGPRAlarmResults())
                        alarmSource.OnNext(item);
                    alarmSource.OnNext(ana.GetThreeCodeAlarmResults());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

        }
    }
}
