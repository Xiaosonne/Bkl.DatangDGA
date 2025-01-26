using Bkl.ESPS.Bussiness;
using Bkl.Infrastructure;
using Bkl.Models;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bkl.ESPS.Controllers
{

    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public partial class DgaRulesController : Controller
    {
        [HttpGet("report")]
        public async Task<IActionResult> DgaReportAsync([FromServices] BklDbContext context,
            [FromServices] LogonUser user,
            [FromQuery] long factoryId,
            [FromQuery] long facilityId,
            [FromQuery] long deviceId,
            [FromQuery] string startTime,
            [FromQuery] string endTime)
        {
            var mon = DateTime.TryParse(startTime, out var start) ? start.Month : DateTime.Now.Month;
            var year = DateTime.TryParse(endTime, out var end) ? end.Year : DateTime.Now.Year;
            var where = context.BklDGAStatus.Where(s => s.Createtime.Month == mon && s.Createtime.Year == year);
            if (factoryId > 0)
                where = where.Where(s => s.FactoryRelId == factoryId);
            if (facilityId > 0)
                where = where.Where(s => s.FacilityRelId == facilityId);
            if (deviceId > 0)
                where = where.Where(s => s.DeviceRelId == deviceId);
            var ids = where
                 .GroupBy(s => s.DeviceRelId)
                 .Select(t => new { max = t.Max(q => q.Id), min = t.Min(q => q.Id) })
                 .ToList()
                 .Select(s => new long[] { s.max, s.min })
                 .SelectMany(s => s)
                 .ToArray();
            var status = context.BklDGAStatus.Where(s => ids.Contains(s.Id)).ToArray();
            var deviceIds = status.Select(s => s.DeviceRelId).ToArray();
            var alarmrules = context.BklAnalysisRule.ToList();

            var logs = context.BklAnalysisLog.Where(s => s.Createtime.Month == mon && s.Createtime.Year == year && deviceIds.Contains(s.DeviceId)).ToArray();
            var devs = context.BklDeviceMetadata.Where(s => deviceIds.Contains(s.Id)).Select(s => new { id = s.Id, factoryId = s.FactoryId, facilityId = s.FacilityId, s.DeviceName, s.FacilityName, s.FactoryName }).ToArray();
            var logCOunt = logs.GroupBy(s => s.DeviceId)
                .Select(ts =>
                {
                    return new
                    {
                        devId = ts.First().DeviceId,

                        alarmCount = ts.GroupBy(s => s.Title).Select(t => new { key = t.Key, value = t.Count() })
                     .ToArray()
                    };
                });

            var arr = logs.GroupBy(s => s.FacilityId)
                  .Select(q =>
                  {
                      var dev = devs.FirstOrDefault(s => s.facilityId == q.Key);

                      return new
                      {
                          factoryName = dev.FactoryName,
                          deviceName = dev.DeviceName,
                          sum = q.Count(),
                          sumError = q.Where(s => s.Level != "正常").Count(),
                          counts = q.GroupBy(r => r.Level).Select(q => new { count = q.Count(), level = q.Key }).ToArray()
                      };
                  })
                  .OrderByDescending(r => r.sumError)
                  .ToArray();

            var topLong = string.Join("、", logs.Where(s => s.Level != "正常").OrderByDescending(q => q.EndTime - q.StartTime)
                  .Select(r =>
                  {
                      var dev = devs.FirstOrDefault(s => s.id == r.DeviceId);
                      return $"{dev.FactoryName}{dev.FacilityName}{r.Title}";
                  })
                  .Take(3)
                  .ToArray());
            var topLong2 = string.Join("、", logs.Where(s => s.Level != "正常").GroupBy(s => s.FacilityId).Select(q => new { q.Key, count = q.Count() })
                .OrderByDescending(s => s.count)
                .Select(r =>
                {
                    var dev = devs.FirstOrDefault(q => q.facilityId == r.Key);
                    return $"{dev.FactoryName}{dev.FacilityName}";
                })
                .Take(3));
            var topLong3 = string.Join("、", logs.Where(s => s.Level != "正常").GroupBy(s => s.Title)
                .Select(s => new { s.Key, count = s.Count() })
                .OrderByDescending(s => s.count)
                .Select(r => r.Key)
                .Take(5));
            string str = $"从{startTime}至{endTime}止，共计巡检{devs.Select(q => q.factoryId).Distinct().Count()}处场站，" +
                $"系统产生{logs.Where(q => q.Level != "正常").Count()}条预警报警记录。" +
                $"所有预警记录中持续最长时间的是{topLong}，报警最多的场站是{topLong2}，报警频率最高的是{topLong3}。" +
                $"";
            var ret = status.OrderBy(s => s.Id)
                .GroupBy(s => s.DeviceRelId)
                .Select(t => t.ToArray())
                .Select(t =>
                {
                    var orderded = t.OrderBy(s => s.Time).ToList();
                    return new
                    {
                        time = orderded.First().Createtime.ToString("yyyy年MM月"),
                        dev = devs.FirstOrDefault(q => q.id == t[0].DeviceRelId),
                        first = orderded.First(),
                        last = orderded.Last(),
                        alarmsCount = logCOunt.FirstOrDefault(s => s.devId == t[0].DeviceRelId)?.alarmCount
                    };
                })
                .Where(s => s.dev != null)
                .ToArray();

            List<object> lis = new List<object>();
            foreach (var order in FactoryOrders.orders)
            {
                var da = ret.FirstOrDefault(s => s.dev.FactoryName.Contains(order));
                if (da != null) lis.Add(da);
            }

            return Json(new { topfacs = arr, desc = str, details = lis }, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
        [Authorize(Roles = "admin")]
        [HttpPost("check-logs")]
        public async Task<IActionResult> CheckLogsAsync([FromServices] BklDbContext context, [FromServices] LogonUser user, [FromBody] long[] logIds, [FromQuery] string reason)
        {
            var logs = context.BklAnalysisLog.Where(s => s.HandleTimes == 0 && logIds.Contains(s.Id)).ToList();
            logs.ForEach(s =>
            {
                s.HandleTimes = 1;
                s.RecordedVideo = JsonSerializer.Serialize(new
                {
                    user = user.userId,
                    time = DateTime.Now.UnixEpoch(),
                    reason
                });
            });
            await context.SaveChangesAsync();
            return Json("");
        }
        [HttpGet("logs")]
        public IActionResult GetLogs([FromServices] BklDbContext context1,
            [FromServices] LogonUser user,
             DateTime starttime,
            DateTime endtime,
            string gasAlgorithm = null,
            long deviceId = 0,
            long factoryId = 0,
            int handle = 0,
            int page = 0,
            int pagesize = 10)
        {
            if (starttime != DateTime.MinValue && endtime != DateTime.MinValue)
            {
                starttime = starttime.ToUniversalTime();
                endtime = endtime.ToUniversalTime();
            }

            var pages = (from log in context1.BklAnalysisLog
                         join device in context1.BklDeviceMetadata on log.DeviceId equals device.Id into devs
                         from p in devs.DefaultIfEmpty()
                         join rule in context1.BklAnalysisRule on log.RuleId equals rule.Id into rules
                         from q in rules.DefaultIfEmpty()
                         where
                         log.StartTime > starttime && log.EndTime < endtime &&
                         log.HandleTimes == handle &&
                         (deviceId == 0 || p == null || p.Id == deviceId) &&
                         (factoryId == 0 || p.FactoryId == factoryId) &&
                         (gasAlgorithm == null || q == null || q.ProbeName == gasAlgorithm)

                         orderby log.Createtime descending
                         select new { log, rule = q, device = p }
                      );


            return Json(new
            {
                data = pages.Skip(page * pagesize).Take(pagesize).ToArray(),
                page = page,
                pagesize = pagesize,
                total = pages.Count(),
            });
        }

        [Authorize(Roles = "admin")]
        [HttpGet("specialgas")]
        public IActionResult GetSpecial([FromServices] BklDbContext context1, [FromServices] LogonUser user, [FromQuery] string gasAlgorithm, long deviceId = 0, string deviceType = null)
        {
            var arr = context1.BklAnalysisRule.Where(p => p.ProbeName == gasAlgorithm && (deviceType == null || p.DeviceType == deviceType) && (deviceId == 0 || p.DeviceId == deviceId)).ToArray();
            return Json(arr.GroupBy(t => t.RuleName).Select(t => new ThresholdRule().From(t.ToArray())).ToArray());
        }

        [Authorize(Roles = "admin")]
        [HttpGet("threshold")]
        public IActionResult GetThree([FromServices] BklDbContext context1, [FromServices] LogonUser user, [FromQuery] string gasAlgorithm, long deviceId = 0, string deviceType = null)
        {
            var arr = context1.BklAnalysisRule.Where(p => p.ProbeName == gasAlgorithm && (deviceType == null || p.DeviceType == deviceType) && (deviceId == 0 || p.DeviceId == deviceId)).ToArray();
            return Json(new ThresholdRule().From(arr));
        }
        [Authorize(Roles = "admin")]
        [HttpPost("threshold")]
        public IActionResult PostThree([FromServices] BklDbContext context1, [FromServices] LogonUser user, [FromQuery] string gasAlgorithm, [FromBody] ThresholdRule rule, [FromQuery] string ruleName = null)
        {
            var dbrules = context1.BklAnalysisRule.Where(p => p.ProbeName == gasAlgorithm && (ruleName == null || p.RuleName == ruleName) && p.DeviceId == rule.DeviceId).ToList();
            if (dbrules.Count == 0)
            {
                var arr = rule.Convert(user.userId, gasAlgorithm);
                context1.AddRange(arr);
                context1.SaveChanges();
                return new JsonResult(arr);
            }
            else
            {
                var ruleindb = dbrules.IntersectBy(rule.ThresholdList.Select(s => s.GasName), t => t.StatusName).ToList();
                ruleindb.ForEach(t =>
                {
                    var thre = rule.ThresholdList.FirstOrDefault(s => s.GasName == t.StatusName);
                    if (thre != null)
                    {
                        t.Level = (int)Enum.Parse<MatchRuleLevelCN>(thre.level);
                        t.Min = thre.value.ToString();
                        t.Max = thre.value.ToString();
                    }
                });
                context1.BklAnalysisRule.RemoveRange(dbrules.Except(ruleindb)
                    .ToList());
                var added = rule.ThresholdList.ExceptBy(dbrules.Select(s => s.StatusName), t => t.GasName)
                      .Aggregate(new ThresholdRule()
                      {
                          DeviceId = rule.DeviceId,
                          DeviceType = rule.DeviceType,
                          FactoryId = rule.FactoryId,
                          RuleName = rule.RuleName,
                          ThresholdList = new List<ThresholdRule.KeyValueLevel>()
                      }, (pre, cur) =>
                      {
                          pre.ThresholdList.Add(cur);
                          return pre;
                      })
                      .Convert(user.userId, gasAlgorithm);
                context1.BklAnalysisRule.AddRange(added);
                context1.SaveChanges();
                return new JsonResult(added);
            }
        }
        [Authorize(Roles = "admin")]
        [HttpGet("dga-config")]
        public async Task<IActionResult> GetDeviceDgaConfig(
          [FromServices] LogonUser user,
          [FromServices] BklDbContext context,
          [FromQuery] long id)
        {
            var updateDev = context.BklDeviceMetadata.FirstOrDefault(s => s.Id == id);
            if (updateDev == null)
            {
                return NotFound();
            }
            var config = TryCatchExtention.TryCatch(() => JsonSerializer.Deserialize<DGAAlarmConfig>(updateDev.DeviceMetadata), DGAAlarmConfig.Default, null);
            await context.SaveChangesAsync();
            return Json(config, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
        [Authorize(Roles = "admin")]
        [HttpPut("dga-config")]
        public async Task<IActionResult> UpdateDevice(
            [FromServices] LogonUser user,
            [FromServices] BklDbContext context,
            [FromQuery] long id,
            [FromBody] DGAAlarmConfig post)
        {
            var updateDev = context.BklDeviceMetadata.FirstOrDefault(s => s.Id == id);
            if (updateDev == null)
            {
                return NotFound();
            }
            updateDev.DeviceMetadata = JsonSerializer.Serialize(post, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false,

            });
            await context.SaveChangesAsync();
            return Ok();
        }
        [Authorize(Roles = "admin")]
        [HttpGet("three")]
        public IActionResult Get([FromServices] BklDbContext context1, [FromServices] LogonUser user, long deviceId = 0, string deviceType = null)
        {
            var arr = context1.BklAnalysisRule.Where(p => p.ProbeName == "三比值法" && (deviceType == null || p.DeviceType == deviceType) && (deviceId == 0 || p.DeviceId == deviceId)).ToArray();
            return Json(new ThreeRule().From(arr));
        }
        [Authorize(Roles = "admin")]
        [HttpPost("three")]
        public IActionResult Post([FromServices] BklDbContext context1, [FromServices] LogonUser user, [FromBody] ThreeRule rule)
        {

            var rules = context1.BklAnalysisRule.Where(p => p.ProbeName == "三比值法" && p.DeviceId == rule.DeviceId).ToList();
            if (rules.Count == 0)
            {
                var arr = rule.Convert(user.userId);
                context1.AddRange(arr);
                context1.SaveChanges();
                return new JsonResult(arr);
            }
            else
            {
                var ruleindb = rules.IntersectBy(rule.CodeLevelMap.Select(s => s.Key), t => t.StatusName).ToList();
                ruleindb.ForEach(t =>
                {
                    t.Level = (int)Enum.Parse<MatchRuleLevelCN>(rule.CodeLevelMap[t.StatusName]);
                });
                context1.BklAnalysisRule.RemoveRange(rules.Except(ruleindb)
                    .ToList());
                var added = rule.CodeLevelMap.ExceptBy(rules.Select(s => s.StatusName), t => t.Key)
                      .Aggregate(new ThreeRule()
                      {
                          DeviceId = rule.DeviceId,
                          DeviceType = rule.DeviceType,
                          FactoryId = rule.FactoryId,
                          CodeLevelMap = new Dictionary<string, string>()
                      }, (pre, cur) =>
                      {
                          pre.CodeLevelMap.Add(cur.Key, cur.Value);
                          return pre;
                      })
                      .Convert(user.userId);
                context1.BklAnalysisRule.AddRange(added);
                context1.SaveChanges();
                return new JsonResult(added);
            }

        }
        public class ThreeRule
        {
            public static Dictionary<string, string> CodeTypeMap = new Dictionary<string, string>
            {
                { "T1","低温过热(t<150℃℃)" },
                 {"T1","低温过热(150℃<t<300℃)"},
                 {"T2","中温过热(300℃<t<700℃)"},
                 {"T3","高温过热(t>700℃℃)"},
                 {"PD","局部放电"},
                 {"D1","低能放电"},
                 {"D1","低能放电兼过热"},
                  {"D2","电弧放电"},
                  {"D2","电弧放电兼过热"},
            };
            public long DeviceId { get; set; }
            public Dictionary<string, string> CodeLevelMap { get; set; }
            public string DeviceType { get; set; }
            public long FactoryId { get; set; }

            public ThreeRule From(BklAnalysisRule[] rules)
            {
                var first = rules.FirstOrDefault();
                if (first == null)
                {
                    return new ThreeRule { };
                }
                return new ThreeRule
                {
                    DeviceId = first.DeviceId,
                    FactoryId = first.FactoryId,
                    DeviceType = first.DeviceType,
                    CodeLevelMap = rules.ToDictionary(s => s.StatusName, s => ((MatchRuleLevelCN)s.Level).ToString())
                };
            }
            public BklAnalysisRule[] Convert(long userId)
            {
                return this.CodeLevelMap.Select(s => new BklAnalysisRule
                {
                    ProbeName = "三比值法",
                    Level = (int)Enum.Parse<MatchRuleLevelCN>(s.Value),
                    StartTime = "0",
                    EndTime = "0",
                    StatusName = s.Key,
                    RuleName = "三比值预警",
                    DeviceId = this.DeviceId,
                    DeviceType = this.DeviceType,
                    CreatorId = userId,
                    ExtraInfo = "null",
                    FactoryId = this.FactoryId,
                    Min = "0",
                    Max = "0",
                    Method = "equalTo",
                    TimeType = "s",
                    Id = SnowId.NextId(),
                }).ToArray();
            }

        }

        public class ThresholdRule
        {
            public static Dictionary<string, string> CodeTypeMap = new Dictionary<string, string>
            {
                { "T1","低温过热(t<150℃℃)" },
                 {"T1","低温过热(150℃<t<300℃)"},
                 {"T2","中温过热(300℃<t<700℃)"},
                 {"T3","高温过热(t>700℃℃)"},
                 {"PD","局部放电"},
                 {"D1","低能放电"},
                 {"D1","低能放电兼过热"},
                  {"D2","电弧放电"},
                  {"D2","电弧放电兼过热"},
            };
            public class KeyValueLevel
            {
                public string GasName { get; set; }
                public double value { get; set; }
                public string level { get; set; }
                public string method { get; set; }
            }

            public long DeviceId { get; set; }
            public string DeviceType { get; set; }
            public long FactoryId { get; set; }
            public string RuleName { get; set; }
            public double Interval { get; set; }
            public List<KeyValueLevel> ThresholdList { get; set; }

            public ThresholdRule From(BklAnalysisRule[] rules)
            {
                var first = rules.FirstOrDefault();
                if (first == null)
                {
                    return new ThresholdRule { };
                }

                return new ThresholdRule
                {
                    DeviceId = first.DeviceId,
                    FactoryId = first.FactoryId,
                    DeviceType = first.DeviceType,
                    RuleName = first.RuleName,
                    Interval = string.IsNullOrEmpty(first.EndTime) ? 0 : (int)TimeSpan.Parse(first.EndTime).TotalDays,
                    ThresholdList = rules.Select(s => new KeyValueLevel
                    {
                        level = ((MatchRuleLevelCN)s.Level).ToString(),
                        GasName = s.StatusName,
                        value = double.Parse(s.Max),
                        method = s.Method,
                    }).ToList(),

                };
            }
            public BklAnalysisRule[] Convert(long userId, string gasAlgorithm)
            {
                return this.ThresholdList.Select(s => new BklAnalysisRule
                {
                    ProbeName = gasAlgorithm,
                    Level = (int)Enum.Parse<MatchRuleLevelCN>(s.level),
                    StartTime = "0",
                    EndTime = Interval == 0 ? "0" : TimeSpan.FromDays(Interval).ToString(),
                    StatusName = s.GasName,
                    RuleName = RuleName,
                    DeviceId = this.DeviceId,
                    DeviceType = this.DeviceType,
                    CreatorId = userId,
                    ExtraInfo = "null",
                    FactoryId = this.FactoryId,
                    Min = s.value.ToString(),
                    Max = s.value.ToString(),
                    Method = s.method == null ? "biggerThan" : s.method,
                    TimeType = "s",
                    Id = SnowId.NextId(),
                }).ToArray();
            }

        }


    }
}
