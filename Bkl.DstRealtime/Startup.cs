using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Bkl.Dst.Interfaces;
using Microsoft.EntityFrameworkCore;
using Bkl.Models;
using System.Threading;
using SiloClientShared;
using Microsoft.Extensions.Configuration;
using Bkl.DstRealtime.Services;
using Bkl.DstRealtime.Hubs;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Bkl.DstRealtime
{

    public class Startup
    {
        public Startup(IConfiguration config)
        {
            _config = config;
        }
        IConfiguration _config;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            //.AddOrleans();
            BklConfig config = _config.GetSiloConfig();

            services.AddRedis(config);
            services.AddSingleton(config);

            services.AddSingleton(new DbContextOptionsBuilder<BklDbContext>().UseMySQL(config.DatabaseConfig.GetConnectionString()));

            services.AddCors(option =>
            {
                option.AddPolicy("cors", policy => policy
                      .WithMethods("GET", "POST", "HEAD", "PUT", "DELETE", "OPTIONS")
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetIsOriginAllowed(str => true));
            });

            //services.AddHostedService<PushStateBackground>();
            //services.AddHostedService<PushAlarmBackground>();

            services.AddSingleton<IPushOption<PushStateHub>>(new PushStateOption());
            services.AddSingleton<IPushOption<PushAlarmHub>>(new PushAlarmOption());

            services.AddScoped<IBackgroundTaskQueue<SrClientMessage>>(ctx =>
            {
                return new BackgroundTaskQueue<SrClientMessage>(1000);
            });

            services.AddHostedService<PushBackground<PushStateHub>>();
            services.AddHostedService<PushBackground<PushAlarmHub>>();

            services.AddClusterClient();
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting();
            app.UseCors("cors");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<PushStateHub>("/notitionhub");
                endpoints.MapHub<PushAlarmHub>("/analysishub");
                endpoints.MapMethods("/data",new string[] {"GET","POST","PUT","DELETE","OPTION" }, async context =>
                {
                    try
                    {
                        var body = Encoding.UTF8.GetString((await context.Request.BodyReader.ReadAsync()).Buffer);
                        System.IO.File.AppendAllText("d:/alarm.log", body);

                        //var resp = JsonSerializer.Serialize(new
                        //{
                        //    url = context.Request.QueryString.ToString(),
                        //    method = context.Request.Method,
                        //    header = string.Join("\r\n", context.Request.Headers.Select(s => $"{s.Key} {s.Value.ToString()}")),
                        //    body = Encoding.UTF8.GetString((await context.Request.BodyReader.ReadAsync()).Buffer)
                        //});
                        //Console.WriteLine(resp);
                        await context.Response.WriteAsync(""); ;
                    }
                    catch
                    {
                        await context.Response.WriteAsync("error"); ;
                    }
                });
            });
        }
    }
}
