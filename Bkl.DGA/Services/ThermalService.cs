using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class ThermalService : BackgroundService
{
    private ILogger<ThermalService> _logger;
    private IServiceScope _scope;

    public ThermalService(IServiceProvider serviceProvider, ILogger<ThermalService> logger)
    {
        _logger = logger;
        _scope = serviceProvider.CreateScope();
    }
    public record ConnJson(string brandName, string visible, string thermal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var context = _scope.ServiceProvider.GetService<BklDbContext>();
        var cameras = context.BklThermalCamera.ToList();
        var devices = context.BklDeviceMetadata.Where(s => s.DeviceType == "ThermalCamera").ToList();
        var bundles = (from p in cameras
                       join q in devices on p.DeviceId equals q.Id
                       select new { camera = p, conn = q.ConnectionString, device = q })
                       .ToList()
                       .Select(s => new { camera = s.camera, s.device, conn = JsonSerializer.Deserialize<ConnJson>(s.conn) })
                       .ToList();

        //IClusterClient orleanClient;
        //while (true)
        //{
        //    try
        //    {
        //        orleanClient = _scope.ServiceProvider.GetService<IClusterClient>();
        //        break;
        //    }
        //    catch
        //    {

        //    }
        //    await Task.Delay(1000);
        //}

        Queue<IThermalTask> logingQueue = new Queue<IThermalTask>();
        List<IThermalTask> thermalQueue = new List<IThermalTask>();

        bundles.Select(s => ((s.conn.brandName == null || s.conn.brandName == "海康") ? (IThermalTask)_scope.ServiceProvider
        .GetService<ThermalTask>() : (IThermalTask)_scope.ServiceProvider
        .GetService<UniThermalTask>())
        .SetConnection(s.camera.Ip, s.camera.Port, s.camera.Account, s.camera.Password)
        .BindDevice(s.device)).ToList().ForEach(s => logingQueue.Enqueue(s));
        while (true)
        {
            if (logingQueue.Count != 0)
            {
                var item = logingQueue.Dequeue();
                if (item.Login())
                {
                    item.Start();
                    thermalQueue.Add(item);
                }
            } 
            foreach (var task in thermalQueue)
            { 
                var items = await task.DataChannel.Reader.ReadAsync();
                Console.WriteLine($" {task.Device.FacilityName} {task.Device.ConnectionString} {task.Device.DeviceName}  {items.ruleName} {items.minTemp} {items.value}");
                await Task.Delay(10);
            }
        }
    }
}
