using Bkl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using static StackExchange.Redis.Role;
using System.Threading.Channels;
using System;
using SiloClientShared;
using Bkl.DGA.Services;

namespace Bkl.DGA
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var host = Host.CreateDefaultBuilder(args)
                  .ConfigureLogging(logging =>
                  {
                      logging.ClearProviders();
                      logging.AddConsole();
                  })
              .ConfigureAppConfiguration((hostcontext, builder) =>
              {
                  Console.WriteLine("HostEnvironment " + hostcontext.HostingEnvironment.EnvironmentName);
                  builder.AddJsonFile("appsettings.json",
                      optional: true,
                      reloadOnChange: true);
                  //builder.AddJsonFile(
                  //    $"appsettings.{hostcontext.HostingEnvironment.EnvironmentName}.json",
                  //     optional: true,
                  //     reloadOnChange: true);

                  builder.AddJsonFile(
                          $"appsettings.bkl.json",
                           optional: true,
                           reloadOnChange: true);
              })
              .ConfigureServices((context, services) =>
              {

                  BklConfig config = new BklConfig();
                  context.Configuration.GetSection("BklConfig").Bind(config);
                  //Console.WriteLine("mysql connection " + config.MySqlString);

                  services.AddLogging(logging =>
                  {
                      logging.ClearProviders();
                      logging.AddConsole();
                  });
                  services.AddSnowId(config);
                  services.AddSingleton(config);
                  services.AddRedis(config);
                  services.AddGaussDb(context.Configuration);


                  services.AddSingleton(Channel.CreateBounded<ChannelData<DGAService, DeviceState[]>>(new BoundedChannelOptions(100)));

                  services.AddSingleton(Channel.CreateBounded<ChannelData<DGAService, AlarmServiceGasRatio>>(new BoundedChannelOptions(100)
                  {
                      FullMode = BoundedChannelFullMode.DropNewest,
                  }));

                  services.AddSingleton(Channel.CreateBounded<ChannelData<FeatureGasAlarmService, AlarmServiceFeatureGas[]>>(new BoundedChannelOptions(100)
                  {
                      FullMode = BoundedChannelFullMode.DropNewest,
                  }));


                  services.AddSingleton(Channel.CreateBounded<ChannelData<ConnectionMonitorService, ConnectionMonitorService.HBData>>(new BoundedChannelOptions(50)
                  {
                      FullMode = BoundedChannelFullMode.DropNewest,
                  }));



                  services.AddTransient<ModbusTask>();
                  services.AddHostedService<DGAService>();
                  services.AddHostedService<StateSinkService>();
                  services.AddHostedService<FeatureGasAlarmService>();
                  services.AddHostedService<ConnectionMonitorService>();

                  //services.AddClusterClient();
              }).Build();
            host.Run();
        }
    }
}