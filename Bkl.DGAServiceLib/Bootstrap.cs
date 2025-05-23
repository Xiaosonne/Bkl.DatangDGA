using Bkl.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Bkl.Models.DGAModel;

namespace Bkl.DGAServiceLib
{
    public class Bootstrap : ILibBootstrap

    {
        public void FromService(IServiceCollection services)
        {

            services.AddSingleton(Channel.CreateBounded<ChannelData<DGAService, DeviceState[]>>(new BoundedChannelOptions(100)));

            services.AddSingleton(Channel.CreateBounded<ChannelData<DGAService, AlarmServiceGasRatio>>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropNewest,
            }));
            services.AddSingleton(Channel.CreateBounded<ChannelData<AlarmService, AlarmServiceFeatureGas[]>>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropNewest,
            }));

            services.AddHostedService<AlarmService>();
            services.AddHostedService<DistributeService>();
            services.AddHostedService<DGAService>();


        }

        public Type GetMainServiceType()
        {
            return typeof(DGAService);
        }
    }
}
