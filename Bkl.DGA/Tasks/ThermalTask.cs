using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
public class ThermalTask : IThermalTask
{
    ThermalCameraISAPI _camera;
    CancellationTokenSource _cts;
    public ThermalTask(ILogger<ThermalTask> logger)
    {
        _cts = new CancellationTokenSource();
        _camera = null;
        _logger = logger;
        _channel = Channel.CreateBounded<ThermalMetryResult>(new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _logger.LogInformation("CurrentThreadId:" + Thread.CurrentThread.ManagedThreadId);
    }
    private ILogger<ThermalTask> _logger;

    public IThermalTask SetConnection(string DVRIPAddress, int DVRPortNumber, string DVRUserName, string DVRPassword)
    {
        _camera = new ThermalCameraISAPI(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword);
        return this;
    }
    public IThermalTask BindDevice(BklDeviceMetadata device)
    {
        _device = device;
        return this;
    }
    public bool Login()
    {
        return true;
    }
    public IThermalTask Start()
    {
        var token = _cts.Token;
        _task = Task.Run(async () =>
        {
            while (token.IsCancellationRequested == false)
            {
                try
                {
                    await foreach (var s in _camera.ReadThermalMetryAsync(token))
                    {

                        s.ThermometryUploadList.ThermometryUpload.Select(ss =>
                         {
                             ThermalMetryResult temp = new ThermalMetryResult();
                             temp.deviceId = _device.Id;
                             temp.facilityId = _device.FacilityId;
                             temp.factoryId = _device.FactoryId;
                             temp.ruleId = ss.ruleID;
                             temp.ruleName = ss.ruleName;
                             temp.lowPoints = new double[] { ss.LowestPoint.positionX, ss.LowestPoint.positionY };
                             temp.highPoints = new double[] { ss.HighestPoint.positionX, ss.HighestPoint.positionY };
                             temp.minTemp = ss.LinePolygonThermCfg.MinTemperature;
                             temp.value = ss.LinePolygonThermCfg.MaxTemperature;
                             temp.averageTemp = ss.LinePolygonThermCfg.AverageTemperature;
                             return temp;
                         })
                        .ToList()
                        .ForEach(temp =>
                        {
                            _channel.Writer.TryWrite(temp);
                        });
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        });
        return this;
    }
    public IThermalTask Stop()
    {
        _cts.Cancel();
        return this;
    }
    Channel<ThermalMetryResult> _channel;
    private BklDeviceMetadata _device;
    private Task _task;
    public Channel<ThermalMetryResult> DataChannel => _channel;

    public BklDeviceMetadata Device => _device;
}