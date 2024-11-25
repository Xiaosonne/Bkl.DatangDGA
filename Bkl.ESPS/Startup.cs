using Bkl.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Bkl.Infrastructure;
using Orleans;
using Orleans.Configuration;
using Bkl.Dst.Interfaces;
using Orleans.Hosting;
using System.Threading;
using SiloClientShared;
using Bkl.ESPS.Controllers;
using System.Threading.Tasks;

namespace Bkl.ESPS
{

    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var default1 = JwtSecurityTokenHandler.DefaultInboundClaimTypeMap;
            BklConfig config = new BklConfig();
            Configuration.GetSection("BklConfig").Bind(config);
            services.AddSnowId(config);
            services.AddRedis(config);
            services.AddSingleton(config);
            services.AddGaussDb(Configuration);

            //services.AddScoped<DbContextOptions<BklDbContext>>((service) => {
            //    var httpContext=service.GetService<IHttpContextAccessor>();
            //    return new DbContextOptionsBuilder<BklDbContext>().UseMySQL(config.MySqlString).Options;
            //});
            Console.WriteLine("AddDbContext " + BklConfig.Database.GenInitConfig().eusername);
            Console.WriteLine("AddDbContext " + BklConfig.Database.GenInitConfig2().eusername+" "+ BklConfig.Database.GenInitConfig2().epassword);



            //services.AddHostedService<ModbusSlaveService>(); 
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<LogonUser>(context =>
            {
                var http = context.GetService<IHttpContextAccessor>();
                return new LogonUser(http);
            });
            services.AddScoped<CommonDeviceImport>();

            services.AddCors(option =>
            option.AddPolicy("cors", policy => policy.WithMethods("GET", "POST", "HEAD", "PUT", "DELETE", "OPTIONS")
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowed(str => true)));
            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(option =>
                {
                    option.RequireHttpsMetadata = false;
                    option.SaveToken = true;
                    option.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.AuthConfig.Secret)),
                        ValidIssuer = config.AuthConfig.Issuer,
                        ValidAudience = config.AuthConfig.Audience,
                        ValidateIssuer = false,
                        ValidateAudience = false
                    }; 
                });
            services.AddHealthChecks();
            //services.AddSignalR();
            services.AddControllers();

            //services.AddMDNS(new MDNSHelper.ApplicationProfile("esps", "_bcr-esps", 5000));

            //services.AddHostedService<PushStateService>();
            //services.AddHostedService<PushAlarmService>();

            //services.AddClusterClient();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var config = serviceScope.ServiceProvider.GetRequiredService<BklConfig>();
                if (Configuration.GetValue<string>("run") == "initDatabase")
                {
                    var context = serviceScope.ServiceProvider.GetRequiredService<BklDbContext>();
                    using (context)
                    {
                        context.Database.EnsureDeleted();
                        context.Database.Migrate();
                    }
                }
            }



            if (env.IsDevelopment())
            {
                Console.WriteLine("Developement Env ");
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors("cors");
            app.UseHealthChecks("/healthchecks");
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/thhermal-alarm", async (context) =>
                {
                    var arr = await context.Request.BodyReader.ReadAsync();
                    var str = Encoding.UTF8.GetString(arr.Buffer);
                });
                endpoints.MapControllers();
                //endpoints.MapHub<PushStateHub>("/notitionhub");
                //endpoints.MapHub<PushAlarmHub>("/analysishub");
            });
        }
    }
}
