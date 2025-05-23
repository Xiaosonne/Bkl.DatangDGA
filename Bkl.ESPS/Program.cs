using Bkl.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bkl.ESPS
{
    public class Program

    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            })
            .ConfigureAppConfiguration((hostcontext, builder) =>
            {

                builder.AddJsonFile("appsettings.json",  optional: true, reloadOnChange: true);
                //builder.AddJsonFile($"appsettings.{hostcontext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                //builder.AddJsonFile($"appsettings.bkl.json", optional: true, reloadOnChange: true);
                //builder.AddJsonFile($"appsettings.xiangrikui.json", optional: true, reloadOnChange: true);
                builder.AddJsonFile($"appsettings.{hostcontext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            })
            .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
