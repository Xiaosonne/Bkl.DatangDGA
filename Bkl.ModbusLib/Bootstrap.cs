using Bkl.Infrastructure;
using Bkl.ModbusLib;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Bkl.Models.DGAModel;

namespace Bkl.ModbusLib
{
    public class Bootstrap : ILibBootstrap

    {
        public void FromService(IServiceCollection services)
        {
            services.AddSingleton(Channel.CreateBounded<ChannelData<ModbusService, DeviceState[]>>(new BoundedChannelOptions(100)));
            services.AddHostedService<ModbusService>();
        }

        public Type GetMainServiceType()
        {
            return typeof(ModbusService);
        }
    }
}
