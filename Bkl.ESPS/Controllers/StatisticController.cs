using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bkl.ESPS.Controllers
{

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class StatisticController : Controller
    {

        [HttpGet("GetAnalysisLog")]
        public async Task<object> GetWarnLog(
            [FromServices] BklDbContext context,
              [FromServices] LogonUser user,
            [FromQuery] int page = 0,
            [FromQuery] int pagesize = 10,
            [FromQuery] long facilityId = 0,
            [FromQuery] long factoryId = 0,
            [FromQuery] int year = 2021,
            [FromQuery] int needVideo = 0
            )
        {
            var logs = await BklAnalysisLog.QueryAnalysisLogs(context, year, factoryId == 0 ? user.factoryId : factoryId,
                datetype: null,
                date: null,
                facilityId: facilityId,
                page: page,
                pagesize: pagesize,
                needVideo: needVideo);
            return logs;
        }
        [HttpGet("GetWarnCount/{facilityId}/Echarts")]
        public async Task<List<object>> GetWarnCountDetail(
            [FromServices] BklDbContext context,
            [FromServices] LogonUser user,
            [FromQuery] long factoryId,
             int year, string datetype, string date, [FromRoute] long facilityId, [FromRoute] long deviceId = 0)
        {
            var groupViews = await BklAnalysisLog.CountGroupByProbeNameDetail(context, year, datetype, date, factoryId == 0 ? user.factoryId : factoryId, facilityId, deviceId);
            return BklAnalysisLog.EchartDayView(groupViews);
        }
        //day 2020-2-1 month 2021-2 week 2021-6th
        [HttpGet("GetStatisticCount/{datetype}/{date}/{returnType}")]
        public async Task<object> GetDetail(
            [FromServices] BklDbContext context,
            [FromServices] LogonUser user,
            [FromRoute] string returnType,
            [FromRoute] string datetype,
            [FromRoute] string date,
            [FromQuery] long factoryId = 0,
            [FromQuery] long facilityId = 0,
            [FromQuery] long deviceId = 0,
            [FromQuery] int year = 2021
            )
        {
            int totalCount = 0;
            switch (datetype)
            {
                case "day":
                    date = DateTime.Parse(date).DayOfYear.ToString();
                    totalCount = 24;
                    break;
                case "month":
                    var monthDate = DateTime.Parse(date);
                    date = monthDate.Month.ToString();
                    totalCount = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
                    break;
                case "week":
                    date = date.Split('-')[1];
                    date = date.Substring(0, date.Length - 2);
                    totalCount = 7;
                    break;
            }

            if (datetype == "day")
            {

                var logs = await BklAnalysisLog.QueryAnalysisLogs(context, year, factoryId == 0 ? user.factoryId : factoryId, datetype, date, facilityId, deviceId);
                if (returnType == "json")
                {
                    return BklAnalysisLog.GetGroupByMinuteView(logs);
                }
                if (returnType == "echarts")
                {
                    return BklAnalysisLog.GetGroupByMinuteViewEcharts(logs);
                }
                return null;
            }
            var groupViews = await BklAnalysisLog.CountGroupByProbeNameDetail(context, year, datetype, date, factoryId == 0 ? user.factoryId : factoryId, facilityId, deviceId);
            if (returnType == "json")
            {
                return BklAnalysisLog.JsonDataView(groupViews, totalCount);
            }
            if (returnType == "echarts")
            {
                return BklAnalysisLog.EchartDayView(groupViews);

            }
            return null;
        }

        [HttpGet("tanshua/{starttime}/{endtime}/temperature-top")]
        public object GetTanShuaWeekTempTop(
                   [FromServices] BklDbContext context,
                   [FromServices] LogonUser user,
                   [FromRoute] string starttime,
                   [FromRoute] string endtime,
                   [FromQuery] long deviceId = 0)
        {
            long factoryId = user.factoryId;
            var starttimeInt = long.Parse(starttime);
            var endtimeInt = long.Parse(endtime);
            var statusList = context.BklDeviceStatus.Where(q => q.Time >= starttimeInt && q.Time <= endtimeInt & q.FactoryRelId == factoryId)
                 .GroupBy(q => q.DeviceRelId)
                 .Select(q => new
                 {
                     DeviceId = q.Key,
                     Sum = q.Sum(s => s.StatusValue),
                     Average = q.Average(s => s.StatusValue),
                     Max = q.Max(s => s.StatusValue),
                     Min = q.Min(s => s.StatusValue)
                 }).OrderByDescending(s => s.Sum).ToList();
            return statusList;
        }
        [HttpGet("query/alarmlog/{datetype}/{date}/group-count")]
        [HttpGet("tanshua/{datetype}/{date}/count")]
        public async Task<object> GetTanShuaWeekDetail(
               [FromServices] BklDbContext context,
               [FromServices] LogonUser user,
               [FromRoute] string date,
               [FromRoute] string datetype,
               [FromQuery] long facilityId = 0,
               [FromQuery] long factoryId = 0,
               [FromQuery] long deviceId = 0,
               [FromQuery] int year = 2021)
        {
            int totalCount = 0;
            switch (datetype)
            {
                case "day":
                    date = DateTime.Parse(date).DayOfYear.ToString();
                    totalCount = 24;
                    break;
                case "month":
                    var monthDate = DateTime.Parse(date);
                    date = monthDate.Month.ToString();
                    totalCount = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
                    break;
                case "week":
                    date = date.Split('-')[1];
                    date = date.Substring(0, date.Length - 2);
                    totalCount = 7;
                    break;
            }
            var logs = await BklAnalysisLog.QueryAnalysisLogs(context, year, factoryId == 0 ? user.factoryId : factoryId, datetype, date, facilityId, deviceId);
            return logs.GroupBy(dev => $"{dev.groupName}-{dev.probeName}").Select(q => new
            {
                q.First().probeName,
                q.First().groupName,
                warn = q.Where(s => s.level == "50").Count(),
                error = q.Where(s => s.level == "60").Count(),
            }).ToList();
        }
        [HttpGet("dga-status-devices")]
        public IActionResult ListDeviceWithStatus([FromServices] LogonUser user, [FromServices] BklDbContext bklDbContext, [FromServices] IRedisClient redisClient, long deviceId = 0)
        {
            var arr = bklDbContext.BklDeviceMetadata
            .Where(q => (deviceId == 0 || q.Id == deviceId) && q.FactoryId == user.factoryId)
            .OrderByDescending(s => s.Id)

            .ToList();
            var dids = arr.Select(q => q.Id).ToList();
            if (deviceId != 0)
            {
                Dictionary<string, object> returnData = new Dictionary<string, object>();
                try
                {
                    fillDeviceIdStatus(redisClient, deviceId, returnData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                returnData.Add("device", arr.FirstOrDefault());

                return new JsonResult(returnData);
            }
            else
            {
                List<object> lis = new List<object>();
                arr.ForEach(device =>
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic.Add("device", device);
                    fillDeviceIdStatus(redisClient, device.Id, dic);
                    lis.Add(dic);
                });
                return new JsonResult(lis);
            }

        }

        private static BklDGAStatus fillDeviceIdStatus(IRedisClient redisClient, long deviceId, Dictionary<string, object> returnData)
        {
            var dic = redisClient.GetValuesFromHash($"DGAStatus:{deviceId}");
            var dic2 = redisClient.GetValuesFromHash($"DGAErrorStatus:{deviceId}");
            var tatioValueStr = (string)redisClient.Get($"GasStatus:{deviceId}");
            var absRate = redisClient.GetValuesFromHash($"absoluteRate:{deviceId}").ToDictionary(s => s.Key, s => (double)s.Value);
            var retRate = redisClient.GetValuesFromHash($"relativeRate:{deviceId}").ToDictionary(s => s.Key, s => (double)s.Value);
            var tatioValue = tatioValueStr == null ? default(BklDGAStatus) : JsonSerializer.Deserialize<BklDGAStatus>(tatioValueStr);
            returnData.Add("status", dic.Select(q => JsonSerializer.Deserialize<DeviceStatusItem>(q.Value)).ToList());
            returnData.Add("errorStatus", dic2.ToDictionary(s => s.Key, s => s.Value.ToString()));
            returnData.Add("extStatus", new { absoluteRate = absRate, relativeRate = retRate, tatio = tatioValue });
            if (tatioValue != null)
                fillThreeTatioError(returnData, tatioValue);
            return tatioValue;
        }

        private static void fillThreeTatioError(Dictionary<string, object> returnData, BklDGAStatus tatioValue)
        {
            foreach (var item in DGATTHelper.threeTatioCode)
            {
                bool b1 = item.Key.Item1 == tatioValue.C2H2_C2H4_Code || item.Key.Item1 < 0 && tatioValue.C2H2_C2H4_Code <= Math.Abs(item.Key.Item1);
                bool b2 = item.Key.Item2 == tatioValue.CH4_H2_Code || item.Key.Item2 < 0 && tatioValue.CH4_H2_Code <= Math.Abs(item.Key.Item2);
                bool b3 = item.Key.Item3 == tatioValue.C2H4_C2H6_Code || item.Key.Item3 < 0 && tatioValue.C2H4_C2H6_Code <= Math.Abs(item.Key.Item3);

                if (b1 && b2 && b3)
                {
                    returnData.Add("errorReason", item.Value.Item1);
                    returnData.Add("errorSample", item.Value.Item2);
                }
            }
            if (!returnData.ContainsKey("errorReason"))
            {
                returnData.Add("errorReason", "设备运行正常");
                returnData.Add("errorSample", "无");
            }
        }
        [HttpGet("query-alarmlog")]

        public IActionResult QueryAlarmLog([FromServices] BklDbContext context, long deviceId, DateTime startTime, DateTime endTime, long facilityId = 0, int maxInterval = 60)
        {
            IQueryable<BklAnalysisLog> where = context.BklAnalysisLog.Where(q => q.EndTime <= endTime && q.StartTime >= startTime).OrderByDescending(s => s.Createtime);
            if (facilityId > 0)
                where = where.Where(s => s.FacilityId == facilityId);
            if (deviceId > 0)
                where = where.Where(s => s.DeviceId == deviceId);
            var list = where.ToList();
            return new JsonResult(list, new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.General));
        }

        [HttpGet("thermal-query-status")]

        public IActionResult ThermalQueryStatus([FromServices] BklDbContext context, long deviceId, DateTime startTime, DateTime endTime, int maxInterval = 60)
        {

            var start = long.Parse(startTime.ToString("yyyyMMddHHmmss"));
            var end = long.Parse(endTime.ToString("yyyyMMddHHmmss"));
            var list = context
                .BklDeviceStatus
                .Where(q => q.DeviceRelId == deviceId && q.Time <= end && q.Time >= start)
                .ToList();
            var listObj = new List<Dictionary<string, object>>();
            foreach (var sameGroup in list.GroupBy(q => q.GroupName))
            {
                foreach (var sameStatus in sameGroup.GroupBy(q => q.StatusName))
                {
                    foreach (var sameTime in sameStatus.GroupBy(q => DateTime.ParseExact(q.Time.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture).UnixEpoch() / maxInterval))
                    {
                        var first = sameTime.OrderByDescending(q => q.Time).FirstOrDefault();
                        var dic1 = new Dictionary<string, object>();
                        dic1.Add("Time", (sameTime.Key * maxInterval).UnixEpochBack().ToString("yyyy-MM-dd HH:mm:ss"));
                        dic1.Add("StatusValue", first.StatusValue);
                        dic1.Add("MaxValue", sameTime.Max(m => m.StatusValue));
                        dic1.Add("MinValue", sameTime.Min(m => m.StatusValue));
                        dic1.Add("StatusName", sameStatus.Key);
                        dic1.Add("GroupName", sameGroup.Key);
                        listObj.Add(dic1);
                    }
                }
            }
            var ret = listObj.GroupBy(s => s["Time"]).Select(q => q.ToArray()).ToArray();
            return new JsonResult(ret, new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.General));
        }
        [HttpGet("query-status")]

        public IActionResult QueryStatus([FromServices] BklDbContext context, long deviceId, DateTime startTime, DateTime endTime, int maxInterval = 60)
        {
            var start = long.Parse(startTime.ToString("yyyyMMddHHmmss"));
            var end = long.Parse(endTime.ToString("yyyyMMddHHmmss"));
            var list = context
                .BklDeviceStatus
                .Where(q => q.DeviceRelId == deviceId && q.Time <= end && q.Time >= start)
                .ToList()
                .GroupBy(q => DateTime.ParseExact(q.Time.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture).UnixEpoch() / maxInterval)
                .Select(s =>
                {
                    var lis = s.GroupBy(q => q.GroupName + "." + q.StatusName).Select(r =>
                                  {
                                      return r.OrderBy(t => t.Id).FirstOrDefault();
                                  });
                    var dic = lis.ToDictionary(m => m.GroupName + "." + m.StatusName, m => m.StatusValue);
                    dic.Add("Time", lis.FirstOrDefault().Time);
                    return dic;
                }).ToArray();
            return new JsonResult(list, new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.General));
        }
        [HttpGet("dga-query-status")]

        public IActionResult DGAQueryStatus([FromServices] BklDbContext context, long deviceId,
        DateTime startTime,
        DateTime endTime,
         int maxInterval = 60,
         int page = 0, int pageSize = 10)
        {
            var start = startTime.UnixEpoch();
            var end = endTime.UnixEpoch();
            var list = context.BklDGAStatus
            .Where(q => q.DeviceRelId == deviceId && q.Time <= end && q.Time >= start)
                 //.ToList()
                 //.Where(q => (q.CH4_Inc != 0 || q.CO_Inc != 0))
                 //.GroupBy(s => s.Time / maxInterval)
                 //.Select(s => s.OrderByDescending(q => q.Time).FirstOrDefault())
                 .OrderByDescending(s => s.Time)
                 .Skip(page * pageSize)
                 .Take(pageSize)
                 .ToList()
                 .OrderBy(s => s.Time)
                 .ToList();
            var returnData = list.Select(item =>
              {
                  var dicItem = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(item)); ;
                  if (dicItem[nameof(item.ThreeTatio_Code)]?.ToString() != "none")
                  {
                      fillThreeTatioError(dicItem, item);
                  }
                  return dicItem;
              });
            return new JsonResult(returnData, new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.General));
        }
        [HttpGet("dga-gas-production")]

        public IActionResult QueryGasProduction([FromServices] BklDbContext context, long deviceId, DateTime startTime, DateTime endTime, string taskId = null)
        {
            var starts = startTime.ToUniversalTime();
            var ends = endTime.ToUniversalTime();
            var taskIdDb = taskId.Empty() ? "system" : taskId;
            var list = context.BklDGAGasProduction.Where(q => q.DeviceRelId == deviceId && q.Createtime <= ends && q.Createtime >= starts)
                 .ToList();
            var ret = list.GroupBy(s => s.RateType).ToDictionary(s => s.Key, m => m.GroupBy(n => n.Time).Select(r =>
            {
                var dic = r.ToDictionary(rr => rr.GasName, rr => (object)rr.Rate);
                dic.Add("Time", r.Key);
                dic.Add("Createtime", ((int)r.Key).UnixEpochBack());
                dic.Add("RatioType", m.Key);
                dic.Add("DeviceId", r.First().DeviceRelId);
                return dic;
            }));
            return new JsonResult(ret, new System.Text.Json.JsonSerializerOptions(JsonSerializerDefaults.General));
        }
    }//controller
}//namespace
