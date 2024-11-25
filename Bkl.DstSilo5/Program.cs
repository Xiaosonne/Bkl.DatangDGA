using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Orleans.Hosting;
using Bkl.Dst.Grains;
using Orleans;
using Orleans.Configuration;
using Bkl.Dst.Interfaces;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Orleans.Persistence;
using Microsoft.Extensions.DependencyInjection;
using CommandLine;
using Microsoft.Extensions.Options;
using Bkl.Infrastructure;
using Microsoft.Extensions.Configuration;
using Orleans.Runtime.Development;

namespace Bkl.DstSilo5
{

    internal class Program
    {

        static string[] _mainArgs;

        private static void ConfigOrleans(Microsoft.Extensions.Hosting.HostBuilderContext context, ISiloBuilder siloBuilder1)
        {
            //string dbUrl = Environment.GetEnvironmentVariable("BKL_DB_STRING") ?? BklConstants.MySQLConnectionString;
            //Console.WriteLine($"ConnectionMysql {dbUrl}");
            //if (context.HostingEnvironment.IsDevelopment())
            //    dbUrl = BklConstants.MySQLDevConnectionString;

            var argOpt = Parser.Default.ParseArguments<SiloConfig>(_mainArgs).Value;

            BklConfig config = context.Configuration.GetSiloConfig();

            string dbUrl = config.DatabaseConfig.GetConnectionString();


            var REDIS_STORE = $"{config.RedisConfig.RedisHost}:{config.RedisConfig.RedisPort},password={config.RedisConfig.Auth}"; 
            
            var REDIS_CLUSTER = $"{config.RedisConfig.RedisHost}:{config.RedisConfig.RedisPort},password={config.RedisConfig.Auth}";

            var siloRedisDB = config.RedisConfig.SiloDb;

            Console.WriteLine($"Connection Mysql {dbUrl} Redis {REDIS_STORE}:{siloRedisDB}");
            siloBuilder1.AddRedisGrainStorage("PubSubStore", (RedisStorageOptions opt) =>
            {
                opt.ConnectionString = config.RedisConfig.SiloStreamRedis;
                opt.DatabaseNumber = siloRedisDB;
            });
            siloBuilder1.AddRedisGrainStorage(BklConstants.RedisProvider, (RedisStorageOptions opt) =>
            {
                opt.ConnectionString = config.RedisConfig.SiloStorageRedis;
                opt.DatabaseNumber = siloRedisDB;
            });
            siloBuilder1.AddSimpleMessageStreamProvider(BklConstants.StreamProvider);
            siloBuilder1.Configure<ClusterOptions>(option =>
            {
                option.ClusterId = argOpt.ClusterId;
                option.ServiceId = argOpt.ServiceId;
            });
            siloBuilder1.UseRedisClustering(option =>
            {
                option.ConnectionString = config.RedisConfig.SiloClusterRedis;
                option.Database = siloRedisDB;
            });
            siloBuilder1.ConfigureEndpoints(
                System.Net.IPAddress.Parse(argOpt.AdvertiseAddress),
                argOpt.SiloPort,
                argOpt.GatewayPort);
            siloBuilder1.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ModbusGrain).Assembly).WithReferences());
          
            siloBuilder1.ConfigureServices((context, services) =>
            {
                services.AddSingleton(config);
                services.AddRedis(config);
                services.AddSnowId(config);

                services.AddSingleton(new BklDGAConfig());
                services.AddDbContext<BklDbContext>(option =>
                {
                    option.UseMySQL(dbUrl);
                });
                services.AddSingleton(new DbContextOptionsBuilder<BklDbContext>()
                        .UseMySQL(dbUrl));
                services.UseRedisReminderService(option =>
                {
                    option.ConnectionString = config.RedisConfig.SiloReminderRedis;
                    option.DatabaseNumber = siloRedisDB;
                });
                services.AddHostedService<SiloLifetimeManager>();
            });

        }
        static async Task<int> Main(string[] args)
        {
            var argOpt = Parser.Default.ParseArguments<SiloConfig>(args).Value;
            _mainArgs = args;
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostcontext, builder) =>
                {

                    builder.AddJsonFile("appsettings.json", optional: true);
                    builder.AddJsonFile("appsettings.Development.json",optional: true);
                }) 
                .UseOrleans(ConfigOrleans)
                  .ConfigureLogging((context, logging) =>
                  {
                      logging.AddConsole();
                  })
                  .Build(); 
            await host.RunAsync();
            return 0;
        }
    }
}
