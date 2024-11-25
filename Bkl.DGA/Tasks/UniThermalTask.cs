using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public record ConnJson(string brandName, string visible, string thermal);

public class UniThermalTask : IThermalTask
{
    private ILogger<ThermalTask> _logger;
    private Channel<ThermalMetryResult> _channel;
    private CancellationTokenSource _cts;
    private UniviewHelper _camera;
    private BklDeviceMetadata _device;
    private object _task;

    public UniThermalTask(ILogger<ThermalTask> logger)
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

    public Channel<ThermalMetryResult> DataChannel => _channel;

    public BklDeviceMetadata Device => _device;

    public IThermalTask BindDevice(BklDeviceMetadata device)
    {
        _device = device;
        return this;
    }

    public bool Login()
    {
        return true;
    }

    public IThermalTask SetConnection(string DVRIPAddress, int DVRPortNumber, string DVRUserName, string DVRPassword)
    {
        _camera = new UniviewHelper(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword);
        return this;
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
                    var resp = await _camera.GetTemperatureValues();
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
                        return temp;
                    })
                    .ToList()
                    .ForEach(temp => _channel.Writer.TryWrite(temp));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                await Task.Delay(1000);
            }
        });
        return this;
    }

    public IThermalTask Stop()
    {
        _cts.Cancel();
        return this;
    }
}
