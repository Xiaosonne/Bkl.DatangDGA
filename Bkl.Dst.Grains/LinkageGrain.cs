using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bkl.Dst.Grains
{
    public class LinkageGrain : Grain, ILinkageGrain
    {
        private DbContextOptions<BklDbContext> _option;
        private List<BklAnalysisRule> _rules;
        private List<BklLinkageAction> _linkages;
        private List<ModbusDevicePair> _pairs;
        private IRedisClient _redis;
        private ILogger<ThermalCameraGrain> _logger;
        private Dictionary<long, LinkageMatchedItem[]> _ruleMap;
        public LinkageGrain(DbContextOptionsBuilder<BklDbContext> dboptionbuilder, IRedisClient redis, ILogger<ThermalCameraGrain> logger)
        {
            _option = dboptionbuilder.Options;
            _redis = redis;
            _logger = logger;
        }


        public override async Task OnActivateAsync()
        {
            LinkageActionId linkageId = this.GetPrimaryKeyString();
            _ruleMap = new Dictionary<long, LinkageMatchedItem[]>();
            using (BklDbContext context = new BklDbContext(_option))
            {
                _rules = context.BklAnalysisRule.Where(s => s.LinkageActionId == linkageId.Id).ToList();
                _linkages = context.BklLinkageAction.Where(s => s.LinkageActionId == linkageId.Id).ToList();
                var pairIds = _linkages.Select(s => s.PairId).ToArray();
                var conns = context.ModbusConnInfo.Where(s => pairIds.Contains(s.Id)).ToArray();
                _pairs = context.ModbusDevicePair.Where(s => pairIds.Contains(s.Id)).ToList();

                var devs = context.BklDeviceMetadata.ToList();
                var rules1 = _rules.Where(s => s.ProbeName == "ALL_DEVICE").ToList();

                var rules2 = _rules.Where(s => s.ProbeName != "ALL_DEVICE").ToList();
                var allitems = new List<LinkageMatchedItem>();

                foreach (var item in rules1)
                {
                    var items = devs.Where(s => s.DeviceType == item.DeviceType)
                         .Select(s =>
                         {
                             return new LinkageMatchedItem
                             {
                                 RuleId = item.Id,
                                 DeviceId = item.DeviceId,
                                 Matched = false,
                                 StatusName = item.StatusName,
                             };
                         });
                    allitems.AddRange(items);
                }
                foreach (var item in rules2)
                {
                    var items = devs.Where(s => s.Id == item.DeviceId && s.DeviceType == item.DeviceType)
                         .Select(s =>
                         {
                             return new LinkageMatchedItem
                             {
                                 RuleId = item.Id,
                                 DeviceId = item.DeviceId,
                                 Matched = false,
                                 StatusName = item.StatusName,
                             };
                         });
                    allitems.AddRange(items);
                }
                _ruleMap.Add(linkageId.Id, allitems.ToArray());
                _redis.Set($"Linkage:{linkageId.Id}", JsonSerializer.Serialize(allitems));
            }
            this.RegisterTimer(this.Timer1, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
        private async Task Timer1(object state)
        {
            foreach (var kv in _ruleMap)
            {
                if (kv.Value.All(s => s.Matched && DateTime.Now.Subtract(s.Lasttime).TotalSeconds < 10))
                {
                    foreach (var action in _linkages)
                    {
                        _logger.LogInformation($"InvokeLinkage {kv.Key} {action.ConnectionUuid} {JsonSerializer.Serialize(kv.Value)}");
                        var pair = _pairs.Where(s => s.ConnUuid == action.ConnectionUuid && s.Id == action.PairId && s.NodeId == action.AttributeId).FirstOrDefault();

                        var deviceGrain = GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(new BklDeviceMetadataRef { Id = pair.DeviceId }));
                        await deviceGrain.SetStatus(new WriteDeviceStatusRequest
                        {
                            ConnUuid = action.ConnectionUuid,
                            AttributeId = action.AttributeId,
                            PairId = action.PairId,
                            ProtocolName = "",
                            DeviceId = pair.DeviceId,
                            Data = new byte[] { byte.Parse(action.ValueHexString) }
                        });
                    }
                }
            }
        }
        public Task SetMatchedItem(LinkageMatchedItem matchedItems)
        {
            _logger.LogInformation($"UpdateLinkage  {JsonSerializer.Serialize(matchedItems)}");
            foreach (var kv in _ruleMap)
            {
                var lis = kv.Value.Where(q => q.RuleId == matchedItems.RuleId && q.DeviceId == matchedItems.DeviceId).ToList();
                if (lis.Count > 0)
                {
                    lis.ForEach(t =>
                    {
                        if (matchedItems.Matched != t.Matched)
                        {
                            t.Matched = matchedItems.Matched;
                            t.CurrentValue = matchedItems.CurrentValue;
                            t.Lasttime = DateTime.Now;
                        }
                    });
                    _redis.Set($"Linkage:{kv.Key}", JsonSerializer.Serialize(kv.Value));
                }
            }
            return Task.CompletedTask;
        }
    }
}