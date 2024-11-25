using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisServiceExtension
    {
        public static void AddRedis(this IServiceCollection services, BklConfig.Redis config)
        {
            services.Configure((System.Action<StackExchange.Redis.ConfigurationOptions>)(conf =>
            {
                conf.Password = config.Auth;
                conf.DefaultDatabase = config.DefaultDb;
                conf.EndPoints.Add(config.RedisHost, (int)config.RedisPort);
            }));

            services.AddScoped<IRedisClient, RedisClient>();
            services.AddSingleton<StackExchange.Redis.ConnectionMultiplexer, StackExchange.Redis.ConnectionMultiplexer>((serv) =>
            {
                var opt = serv.GetService<IOptions<StackExchange.Redis.ConfigurationOptions>>().Value;
                return StackExchange.Redis.ConnectionMultiplexer.Connect(opt);
            });
        }
       
        public static void AddRedis(this IServiceCollection services, BklConfig config)
        {
            services.Configure((System.Action<StackExchange.Redis.ConfigurationOptions>)(conf =>
            {
                conf.Password = config.RedisConfig.Auth;
                conf.DefaultDatabase = config.RedisConfig.DefaultDb;
                conf.EndPoints.Add(config.RedisConfig.RedisHost, (int)config.RedisConfig.RedisPort);
            }));

            services.AddScoped<IRedisClient, RedisClient>();
            services.AddSingleton<StackExchange.Redis.ConnectionMultiplexer, StackExchange.Redis.ConnectionMultiplexer>((serv) =>
            {
                var opt = serv.GetService<IOptions<StackExchange.Redis.ConfigurationOptions>>().Value;
                return StackExchange.Redis.ConnectionMultiplexer.Connect(opt);
            });
        }
    }
}
