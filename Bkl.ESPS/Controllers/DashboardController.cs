using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bkl.ESPS.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        public static int ParseDateTypeDate(string datetypedate, string datetype)
        {
            if (int.TryParse(datetypedate, out var num))
                return num;
            switch (datetype)
            {
                case "day":
                    return DateTime.Parse(datetypedate).DayOfYear;
                case "month":
                    return DateTime.Parse(datetypedate).Month;
                case "week":
                    return DateTime.Parse(datetypedate).WeekOfYear();
            }
            return 0;
        }

        [HttpGet("GetTotalCount")]
        public object GetToTalStatisticCount(
            [FromServices] BklDbContext context,
             [FromServices] IRedisClient redisClient,
        [FromServices] LogonUser user,
        [FromQuery] long factoryId = 0,
        string datetype = "day",
        string date = "32")
        {
            factoryId = factoryId == 0 ? user.factoryId : factoryId;
            var total = context.BklDeviceMetadata.Count(p => p.FactoryId == factoryId);
            var dateInt = ParseDateTypeDate(date, datetype);
            //var statistic = await BklAnalysisLog.GetCachedStatisticCount(
            //    context,
            //    redisClient,
            //    datetype,
            //    (DateTime.Now.Year * 1000 + dateInt).ToString(),
            //     user.factoryId, 0, 0);
            //var sum = statistic.GroupBy(q => q.group).Select(q => new { q.Key, Sum = q.Sum(r => r.values.Sum(t => t.count)) });
            //var error = sum.Where(p => p.Key == "Error").Sum(q => q.Sum);
            //var warn = sum.Where(p => p.Key == "Warn").Sum(q => q.Sum);
            string dKey = "";
            switch (datetype)
            {
                case "day":
                    dKey = (DateTime.Now.Year * 1000 + dateInt).ToString();
                    break;
                case "year":
                    dKey = DateTime.Now.Year.ToString();
                    break;
                default:
                    dKey = (DateTime.Now.Year * 100 + dateInt).ToString();
                    break;
            }
            var hashKey = $"Statistic:Devices:{datetype}:{dKey}";
            var dic = redisClient.GetValuesFromHash(hashKey).Keys.Select(q => (did: q.Split('.')[0], warnType: q.Split('.')[2])).ToArray();
            var result = new Dictionary<string, int>();
            foreach (var gp1 in dic.GroupBy(s => s.did))
            {
                foreach (var gp2 in gp1.GroupBy(s => s.warnType))
                {
                    if (!result.TryAdd(gp2.Key.ToLower(), 1))
                    {
                        result[gp2.Key.ToLower()]++;
                    }
                }
            }
            if (!result.ContainsKey("normal"))
                result.Add("normal", total - result.Sum(s => s.Value));
            result.Add("online", total);
            result.Add("offline", 0);
            return result;
        }

        [HttpGet("GetFacilityPartitialStatisticCount")]
        public IEnumerable<FacilityAlarmViewItem> GetFacilityPartitialStatisticCount([FromServices] BklDbContext context,
            [FromServices] IRedisClient redisClient,
            [FromServices] LogonUser user,

            [FromQuery] string datetype,
            [FromQuery] string date,
            [FromQuery] long factoryId = 0,
            [FromQuery] int year = 2021)
        {
            factoryId = factoryId == 0 ? user.factoryId : factoryId;
            var devices = context.BklDeviceMetadata.Where(p => p.FactoryId == factoryId).ToList();
            var total = context.BklDeviceMetadata.Count(p => p.FactoryId == factoryId);
            var dateInt = ParseDateTypeDate(date, datetype);
            string dKey = "";
            switch (datetype)
            {
                case "day":
                    dKey = (DateTime.Now.Year * 1000 + dateInt).ToString();
                    break;
                case "year":
                    dKey = DateTime.Now.Year.ToString();
                    break;
                default:
                    dKey = (DateTime.Now.Year * 100 + dateInt).ToString();
                    break;
            }
            var hashKey = $"Statistic:Devices:{datetype}:{dKey}";
            var dic = redisClient.GetValuesFromHash(hashKey)
                                .Select(q => (did: long.Parse(q.Key.Split('.')[0]), warnType: q.Key.Split('.')[2], val: (int)q.Value))
                                .ToArray();
            var arr = (from dicItem in dic
                       join dev in devices on dicItem.did equals dev.Id
                       select (dicItem.warnType, dicItem.did, dicItem.val, dev.FacilityId, fname: dev.Path1, part: dev.Path2)).ToList();

            List<FacilityAlarmViewItem> lis = new List<FacilityAlarmViewItem>();
            foreach (var sameFacility in arr.GroupBy(q => q.fname))
            {
                foreach (var samePart in sameFacility.GroupBy(q => q.part))
                {
                    yield return new FacilityAlarmViewItem
                    {
                        FacilityId = samePart.First().FacilityId,
                        Name = samePart.Key,
                        Warn = samePart.Where(s => s.warnType == "Warn").Sum(s => s.val),
                        Error = samePart.Where(s => s.warnType == "Warn").Sum(s => s.val),
                    };
                }
            }
        }
        public class FacilityAlarmViewItem
        {
            public string Name { get; set; }
            public int Warn { get; set; }
            public int Error { get; set; }
            public long FacilityId { get; internal set; }
        }
        [HttpGet("top")]
        public IEnumerable<object> GetTop([FromServices] BklDbContext context,
        [FromServices] LogonUser user,
            [FromServices] IRedisClient redisClient,
            [FromQuery] string datetype,
            [FromQuery] string date,
            [FromQuery] long factoryId = 0,
            [FromQuery] int year = 2021)
        {
            factoryId = factoryId == 0 ? user.factoryId : factoryId;
            var devices = context.BklDeviceMetadata.Where(p => p.FactoryId == factoryId).ToList();
            var total = context.BklDeviceMetadata.Count(p => p.FactoryId == factoryId);
            var dateInt = ParseDateTypeDate(date, datetype);
            string dKey = "";
            switch (datetype)
            {
                case "day":
                    dKey = (DateTime.Now.Year * 1000 + dateInt).ToString();
                    break;
                case "year":
                    dKey = DateTime.Now.Year.ToString();
                    break;
                default:
                    dKey = (DateTime.Now.Year * 100 + dateInt).ToString();
                    break;
            }
            var hashKey = $"Statistic:Devices:{datetype}:{dKey}";
            var dic = redisClient.GetValuesFromHash(hashKey)
                                .Select(q => (did: long.Parse(q.Key.Split('.')[0]), warnType: q.Key.Split('.')[2], val: (int)q.Value))
                                .ToArray();
            var temp1 = (from dev in devices
                         join dicItem in dic on dev.Id equals dicItem.did
                         select (dev.Id, dev.FacilityId, fname: dev.Path1, part: dev.Path2, dicItem.warnType, dicItem.val)).ToList();
            foreach (var item in temp1.GroupBy(q => q.FacilityId))
            {
                foreach (var same in item.GroupBy(s => s.part))
                {
                    yield return new
                    {
                        fid = item.Key,
                        fname = same.First().fname,
                        type = same.Key,
                        count = same.Sum(s => s.val)
                    };
                }
            }
            yield break;
            //return BklAnalysisLog.PartitialGroupCount(groupViews);
        }
    }


}
