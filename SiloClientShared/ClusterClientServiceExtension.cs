using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace SiloClientShared
{
    public static class ClusterClientServiceExtension
    {
        private static Func<Exception, Task<bool>> CreateRetryFilter(int maxAttempts = 5, int perLoopDelaySeconds = 10)
        {
            var attempt = 0;
            return RetryFilter;

            async Task<bool> RetryFilter(Exception exception)
            {
                attempt++;
                Console.WriteLine($"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster");
                if (attempt > maxAttempts)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(perLoopDelaySeconds));
                return true;
            }
        }

        public static void AddClusterClient(this IServiceCollection services)
        {
            //var builder = new ClientBuilder()
            //       .Configure<ClusterOptions>(option =>
            //       {
            //           option.ClusterId = Environment.GetEnvironmentVariable("BKL_CLUSTER_ID") ?? BklConstants.ClusterId;
            //           option.ServiceId = Environment.GetEnvironmentVariable("BKL_SERVICE_ID") ?? BklConstants.ServiceId;
            //       })
            //       .UseRedisClustering(option =>
            //       {
            //           option.ConnectionString = BklConfig.Instance.RedisConfig.SiloClusterRedis;
            //           option.Database = BklConfig.Instance.RedisConfig.SiloDb;
            //       })
            //       .AddSimpleMessageStreamProvider(BklConstants.StreamProvider)
            //       .ConfigureApplicationParts(parts =>
            //       {
            //           //    parts.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences();
            //           parts.AddApplicationPart(typeof(IModbusGrain).Assembly).WithReferences();
            //       });

            //IClusterClient client = builder.Build();
            //client.Connect(CreateRetryFilter(10));
            //services.AddSingleton(client);
            //services.AddSingleton(builder);
        }
    }
}
