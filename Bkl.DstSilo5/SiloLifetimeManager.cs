using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Orleans.Runtime;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Bkl.DstSilo5
{
    public class SiloLifetimeManager : IHostedService
    {
        private IHostApplicationLifetime _applicationLifetime;
        private IServiceProvider _serviceProvider;
        private ILocalSiloDetails _siloDetails;
        private IRedisClient _redisClient;
        private ILogger<SiloLifetimeManager> _logger;

        public SiloLifetimeManager(IHostApplicationLifetime applicationLifetime, IServiceProvider serviceProvider, ILocalSiloDetails siloDetails, ILogger<SiloLifetimeManager> logger)
        {
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _siloDetails = siloDetails; 
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{_siloDetails.SiloAddress.ToString()} {_siloDetails.SiloAddress.ToStringWithHashCode()} start");
            _applicationLifetime.ApplicationStopping.Register(OnStopping);
            return Task.CompletedTask;
        }

        private void OnStopping()
        {
            _redisClient = _serviceProvider.GetService<IRedisClient>();
            _logger.LogInformation($"{_siloDetails.SiloAddress.ToString()} {_siloDetails.SiloAddress.ToStringWithHashCode()} stop");
            _redisClient.RemoveEntryFromHash($"{BklConstants.ServiceId}/{BklConstants.ClusterId}", _siloDetails.SiloAddress.ToString());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
