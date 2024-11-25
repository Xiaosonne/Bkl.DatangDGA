using Orleans;
using System;
using System.Threading.Tasks;
using Bkl.Dst.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Bkl.Models;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.Ocsp;
using Bkl.Infrastructure;
using System.Linq;

namespace Bkl.DstRealtime.Hubs
{
    public class BaseHub : Hub
    {
        private readonly IRedisClient _redis;
        protected readonly ILogger<BaseHub> _logger;
        protected readonly IClusterClient _clusterClient;
        protected readonly DbContextOptions<BklDbContext> _contextOptions;

        public BaseHub(ILogger<BaseHub> logger,
            IClusterClient clusterClient,
            IRedisClient redis,
            DbContextOptionsBuilder<BklDbContext> dbContextOptions)
        {
            _redis = redis;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
            _contextOptions = dbContextOptions.Options;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"instanceHash:{GetHashCode()} connId:{Context.ConnectionId}");
            return Task.CompletedTask;
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {

            try
            {
                var lis = _redis.GetAllItemsFromSortedSet("Orleans.Sublist");

                foreach (var req in lis.Where(s => s.Contains(Context.ConnectionId)).Select(SrJoinGroupRequest.Parse))
                {
                    _logger.LogInformation($"instanceHash:{GetHashCode()} connId:{Context.ConnectionId} {req} disconnected");
                    //var deviceGrain = _clusterClient.GetGrain<IDeviceGrain>(new DeviceGrainId(req.DeviceId));
                    //await deviceGrain.UnSubscribe(req);
                    _redis.RemoveItemFromSortedSet("Orleans.Sublist", req.ToString());
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, req.GroupId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
        protected async Task AddGroup(string groupId, long deviceId, string callback)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            var request = new SrJoinGroupRequest
            {
                ConnectionId = Context.ConnectionId,
                DeviceId = deviceId,
                GroupId = groupId,
                ClientMethod = callback,
            };
            _redis.AddItemToSortedSet("Orleans.Sublist", request.ToString(), request.DeviceId);
            _logger.LogInformation($"instanceHash:{GetHashCode()}  AddGroup {groupId} {deviceId} {callback}");
        }
    }
}
