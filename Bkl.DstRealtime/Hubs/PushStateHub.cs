using Orleans;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Bkl.Models;
using System.Linq;
using System.Collections.Generic;
using Orleans.Streams;
using Bkl.Infrastructure;

namespace Bkl.DstRealtime.Hubs
{
    public class PushStateHub : BaseHub
    {
        public PushStateHub(ILogger<PushStateHub> logger, IRedisClient redis, IClusterClient clusterClient, DbContextOptionsBuilder<BklDbContext> dbContextOptions) : base(logger, clusterClient, redis, dbContextOptions)
        {

        }

        public async Task SubscribeFacility(long facilityId)
        {
            Console.WriteLine($"statehub connId:{Context.ConnectionId} facilityId:{facilityId}");

            List<long> devIds = null;
            using (BklDbContext contex = new BklDbContext(_contextOptions))
            {
                devIds = contex.BklDeviceMetadata.Where(q => q.FacilityId == facilityId).Select(s => s.Id).ToList();
            }
            foreach (var deviceId in devIds)
            {
                await AddGroup($"facilityId-{facilityId}", deviceId, "onStateWithMeta");
            }
        }

        public async Task SubscribeDevice(long deviceId)
        {
            await AddGroup($"deviceId-{deviceId}", deviceId, "onStateWithMeta");
        }


    }
}
