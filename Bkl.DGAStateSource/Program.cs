using Bkl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Yitter.IdGenerator;

namespace Bkl.DGAStateSource
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
                 builder.AddJsonFile(
                     $"appsettings.bkl.json",
                      optional: true,
                      reloadOnChange: true);
             })
             .ConfigureServices((context, services) =>
             {


                 BklConfig.Instance = new BklConfig();
                 if (BklConfig.Instance.SnowConfig == null)
                 {
                     BklConfig.Instance.SnowConfig = new BklConfig.Snow();
                 }
                 SnowId.SetIdGenerator(new IdGeneratorOptions
                 {
                     WorkerId = (ushort)BklConfig.Instance.SnowConfig.WorkerId,
                     DataCenterId = BklConfig.Instance.SnowConfig.DataCenterId,
                     DataCenterIdBitLength = BklConfig.Instance.SnowConfig.DataCenterIdBitLength,
                     WorkerIdBitLength = BklConfig.Instance.SnowConfig.WorkerIdBitLength,
                     SeqBitLength = BklConfig.Instance.SnowConfig.SeqBitLength,
                 });

                 services.AddSingleton(SnowId.IdGenInstance);

                 services.AddTransient<ModbusTask>();
                 services.AddTransient<Iec61850Task>();
                 services.AddTransient<SqlServerTask>();
                 services.AddHostedService<StateSourceService>();
                 //services.AddClusterClient();
             }).Build();
            host.Run();


        }
    }
}
