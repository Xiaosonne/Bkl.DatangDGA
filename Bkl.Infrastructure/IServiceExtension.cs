using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.Extensions.Configuration;
using System;
using Yitter.IdGenerator;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceExtension
    {
        public static void AddSnowId(this IServiceCollection service, BklConfig.Snow config)
        {
            SnowId.SetIdGenerator(new IdGeneratorOptions
            {
                WorkerId = (ushort)config.WorkerId,
                DataCenterId = config.DataCenterId,
                DataCenterIdBitLength = config.DataCenterIdBitLength,
                WorkerIdBitLength = config.WorkerIdBitLength,
                SeqBitLength = config.SeqBitLength,
            });
            service.AddSingleton(SnowId.IdGenInstance);

        }

        public static void AddSnowId(this IServiceCollection service, BklConfig config)
        {
            var workerId = Environment.GetEnvironmentVariable("BKL_WORKER_ID");

            BklConfig.Instance = config;
            if (BklConfig.Instance.SnowConfig == null)
            {
                BklConfig.Instance.SnowConfig = new BklConfig.Snow();
            }
            if (!string.IsNullOrEmpty(workerId) && ushort.TryParse(workerId, out var www))
            {
                BklConfig.Instance.SnowConfig.WorkerId = www;
            }
            SnowId.SetIdGenerator(new IdGeneratorOptions
            {
                WorkerId = (ushort)BklConfig.Instance.SnowConfig.WorkerId,
                DataCenterId = BklConfig.Instance.SnowConfig.DataCenterId,
                DataCenterIdBitLength = BklConfig.Instance.SnowConfig.DataCenterIdBitLength,
                WorkerIdBitLength = BklConfig.Instance.SnowConfig.WorkerIdBitLength,
                SeqBitLength = BklConfig.Instance.SnowConfig.SeqBitLength,
            });
            service.AddSingleton(SnowId.IdGenInstance);

        }
        public static BklConfig AddConfig(this IServiceCollection services, IConfigurationRoot Configuration)
        {
            BklConfig config = new BklConfig();
            Configuration.GetSection("BklConfig").Bind(config);
            services.AddSingleton(config);
            return config;
        }
        public static BklConfig GetSiloConfig(this IConfiguration Configuration)
        {

            var sec = Configuration.GetSection("BklConfig");
            BklConfig config = new BklConfig();
            if (sec != null)
                sec.Bind(config);
            if (config.RedisConfig == null)
                config.RedisConfig = new BklConfig.Redis
                {
                    Auth = Environment.GetEnvironmentVariable("BKL_REDIS_AUTH") ?? "Etor0070x01",
                    DefaultDb = int.Parse(Environment.GetEnvironmentVariable("BKL_REDIS_DB") ?? "1"),
                    SiloDb = int.Parse(Environment.GetEnvironmentVariable("BKL_SILO_DB") ?? "3"),
                    RedisPort = int.Parse(Environment.GetEnvironmentVariable("BKL_REDIS_PORT") ?? "6379"),
                    RedisHost = Environment.GetEnvironmentVariable("BKL_REDIS_HOST") ?? "127.0.0.1",
                };
            if (config.DatabaseConfig == null)
                config.DatabaseConfig = new BklConfig.Database
                {
                    host = Environment.GetEnvironmentVariable("BKL_DB_HOST") ?? "127.0.0.1",
                    database = Environment.GetEnvironmentVariable("BKL_DB_NAME") ?? "bacara",
                    eusername = (Environment.GetEnvironmentVariable("BKL_DB_USER") ?? "root").AESEncrypt(BklConfig.Database.DB_AES_KEY),
                    epassword = (Environment.GetEnvironmentVariable("BKL_DB_PWD") ?? "bkl123...").AESEncrypt(BklConfig.Database.DB_AES_KEY),
                };
            BklConfig.Instance = config;
            return config;
        }
    }
}
