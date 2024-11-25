using Bkl.Models;
using Bkl.StreamServer.Hubs;
using Bkl.StreamServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using static Bkl.Models.DGAModel;

namespace Bkl.StreamServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Host.ConfigureAppConfiguration((hostcontext, builder) =>
            {

                builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                builder.AddJsonFile($"appsettings.{hostcontext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                builder.AddJsonFile($"appsettings.remote.json", optional: true, reloadOnChange: true);
                //builder.AddJsonFile($"appsettings.bkl.json", optional: true, reloadOnChange: true);
            });
            builder.Services.AddControllers();
            builder.Services.AddHostedService<DistributeService>();
            builder.Services.AddHostedService<PersistentService>();
            //builder.Services.AddHostedService<AlarmService>();
            builder.Services.AddSignalR();

            builder.Services.AddCors(option =>
            {
                option.AddPolicy("cors", policy => policy
                      .WithMethods("GET", "POST", "HEAD", "PUT", "DELETE", "OPTIONS")
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetIsOriginAllowed(str => true));
            });
            var redis = new BklConfig.Redis()
            {
                RedisHost = "127.0.0.1",
                RedisPort = 6379,
                Auth = "Etor0070x01",
                DefaultDb = 1
            };
            builder.Configuration.GetSection("Redis").Bind(redis);

            builder.Services.AddGaussDb(builder.Configuration);

            builder.Services.AddSingleton(Channel.CreateBounded<ChannelData<PersistentService, DgaPushData>>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest }));

            builder.Services.AddSingleton(Channel.CreateBounded<ChannelData<PersistentService, DgaAlarmResult>>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest }));
            builder.Services.AddRedis(redis);
            builder.Services.AddSnowId(new BklConfig.Snow());
            //builder.Services.AddHostedService<ZqmDemo>();
            var app = builder.Build();


            app.MapControllers();
            app.UseRouting();
            app.UseCors("cors");
            app.UseEndpoints(end =>
            {
                end.MapHub<DeviceStateHub>("/dgastates");
            });
            app.Run();
        }
    }
}
