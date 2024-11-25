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
using Bkl.Infrastructure;
using Microsoft.Extensions.Configuration;
public class ModbusService : BackgroundService
{
    private DbContextOptionsBuilder _builder;
    private IConfiguration _config;
    private Channel<ChannelData<DGAService, DeviceState[]>> _dgaChannel;
    private IServiceScope _scope;
    private IServiceProvider _serviceProvider;
    private BklDbContext _context;

    public ModbusService(IServiceProvider serviceProvider,
        Channel<ChannelData<DGAService, DeviceState[]>> dgaChannel,
        IConfiguration config,
        DbContextOptionsBuilder builder)
    {
        _builder = builder;
        _config = config;
        _dgaChannel = dgaChannel;
        _scope = serviceProvider.CreateScope();
        _serviceProvider = _scope.ServiceProvider;
        _context = _scope.ServiceProvider.GetService<BklDbContext>();

    }

    DateTime _lastRefresh = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Dictionary<string, ModbusTask> modbusList = new Dictionary<string, ModbusTask>();
        HashSet<long> runningDevice = new HashSet<long>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var readInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:ReadInterval").Value, out var a) ? a : 3600, 3600);

            var refreshInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:RefreshInterval").Value, out var a) ? a : 30, 30);


            var connTimeOut = TryCatchExtention.TryCatch(() => double.TryParse(_config.GetSection("DGA:ConnectionTimeout").Value, out var a) ? a : 1, 1);


            if (DateTime.Now.Subtract(_lastRefresh).TotalSeconds > refreshInterval)
            {
                _lastRefresh = DateTime.Now;
                Console.WriteLine("reload device list " + DateTime.Now);
                try
                {
                    using (var context = new BklDbContext(_builder.Options as DbContextOptions<BklDbContext>))
                    {
                        var devs = context.BklDeviceMetadata.Where(s => s.DeviceType == "DGA-JH").ToList();
                        foreach (var dev in devs)
                        {
                            var uuids = await context.ModbusDevicePair.Where(s => s.DeviceId == dev.Id).Select(s => s.ConnUuid).Distinct().ToArrayAsync();
                            foreach (var uuid in uuids)
                            {
                                if (!modbusList.TryGetValue(uuid, out var task))
                                {
                                    task = _serviceProvider.GetService<ModbusTask>();
                                    modbusList.Add(uuid, task);
                                }
                                await task.Init(dev, uuid, context);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            try
            {
                foreach (var gp in modbusList.Values)
                {
                    List<DeviceState> lis = new List<DeviceState>();
                    // Console.WriteLine($"ENTER ");
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(connTimeOut));
                    await foreach (var status in gp.QueryAsync(readInterval, cts.Token))
                    {
                        //Console.WriteLine($"WRITE {status.First().DeviceId}  {string.Join("\t", status.Select(stat => $"{stat.Name} {stat.NameCN} {stat.Value}"))}");
                        //await _deviceStatusQueue.Writer.WriteAsync(status);
                        // Console.WriteLine($"WRITE  END");

                        if (status.All(d => d.ProtocolName.StartsWith("DGA")))
                        {
                            await _dgaChannel.Writer.WriteAsync(new ChannelData<DGAService, DeviceState[]> { Data = status });
                        }
                        // Console.WriteLine($"STATUS  {string.Join("\t", status.Select(stat => $"{stat.Name} {stat.NameCN} {stat.Value}"))}");
                    }
                }
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
