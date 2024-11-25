using Orleans;
using System;
using System.Threading.Tasks;
using Bkl.Dst.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Bkl.Models;
using System.Linq;
using System.Collections.Generic;
using Orleans.Streams;
using Bkl.Infrastructure;

namespace Bkl.DstRealtime.Hubs
{
    public class PushAlarmHub : BaseHub
    {
        public PushAlarmHub(ILogger<PushStateHub> logger, IClusterClient clusterClient, IRedisClient redis, DbContextOptionsBuilder<BklDbContext> dbContextOptions) : base(logger, clusterClient, redis, dbContextOptions)
        {

        }
        public async Task SubAllAlarmLog()
        {
            _logger.LogInformation("sub all alarm log");
            List<long> devIds = null;
            using (BklDbContext contex = new BklDbContext(_contextOptions))
            {
                devIds = contex.BklDeviceMetadata.Select(s => s.Id).ToList();
            }
            foreach (var deviceId in devIds)
            {
                try { await AddGroup($"allalarmlog", deviceId, "onAlarmWithMeta"); }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
        }
        public async Task SubAlarmLog(long deviceId)
        {
            _logger.LogInformation($"alarmhub connId:{Context.ConnectionId} facilityId:{deviceId}");
            try { await AddGroup($"devalarmlog-{deviceId}", deviceId, "onAlarmWithMeta"); }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }


        public async Task SubFacilityAlarmLog(long facilityId)
        {
            _logger.LogInformation($"alarmhub connId:{Context.ConnectionId} facilityId:{facilityId}");
            List<long> devIds = null;
            using (BklDbContext contex = new BklDbContext(_contextOptions))
            {
                devIds = contex.BklDeviceMetadata.Where(q => q.FacilityId == facilityId).Select(s => s.Id).ToList();
            }
            foreach (var deviceId in devIds)
            {
                try { await AddGroup($"alarmlog-{facilityId}", deviceId, "onAlarmWithMeta"); }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
        }

    }
}
