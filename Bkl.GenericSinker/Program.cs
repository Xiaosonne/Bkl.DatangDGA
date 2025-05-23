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
using System.Reflection;
using Bkl.Infrastructure;

namespace Bkl.GenericSinker
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
                  services.AddMySQL(context.Configuration, "OceanBase");





                  services.AddSingleton(Channel.CreateBounded<ChannelData<ConnectionMonitorService, HBData>>(new BoundedChannelOptions(50)
                  {
                      FullMode = BoundedChannelFullMode.DropNewest,
                  }));

                  services.AddSingleton(Channel.CreateBounded<ChannelData<HubChannelData, HubChannelData>>(new BoundedChannelOptions(500)
                  {
                      FullMode = BoundedChannelFullMode.DropNewest,
                  }));


                  var sinkSerType = typeof(StateSinkService<>);

                  var loadlibs = context.Configuration.GetSection("LoadLib").Get<string[]>(); ;

                  var boostraptype = Assembly.GetAssembly(typeof(Bkl.ModbusLib.Bootstrap)).GetExportedTypes().FirstOrDefault(s => typeof(ILibBootstrap).IsAssignableFrom(s));

                  Bkl.ModbusLib.Bootstrap boot = new ModbusLib.Bootstrap();
                  boot.FromService(services); 
                  var mainClass = boot.GetMainServiceType();
                  var sinkService = sinkSerType.MakeGenericType(mainClass);

                  var opt1 = typeof(ServiceLibOption<>);
                  var optType = opt1.MakeGenericType(mainClass);
                  IServiceLibOpt opt = (IServiceLibOpt)Activator.CreateInstance(optType);
                  opt.MainSection = context.Configuration.GetSection(mainClass.Name);

                  services.AddSingleton(optType, opt);
                  services.AddDynamicHostedService(sinkService);


                  services.AddHostedService<ConnectionMonitorService>();

                  services.AddHostedService<HubChannelService>();

                  //services.AddClusterClient();
              }).Build();
            host.Run();
        }
    }
}