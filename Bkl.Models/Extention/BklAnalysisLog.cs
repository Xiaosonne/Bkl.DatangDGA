using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Text;
using System;
using System.Linq;
using Bkl.Infrastructure;

namespace Bkl.Models
{
    public partial class BklAnalysisLog
    {
        static Dictionary<string, string> levelShowMap = new Dictionary<string, string>
        {
            ["Warn"] = "预警",
            ["Error"] = "报警"
        };
        static Dictionary<string, string> probeNameMap = new Dictionary<string, string>
        {
            //机舱-发电机
            ["发电机"] = "发电机",
            //机舱-刹车盘
            ["刹车盘"] = "刹车盘",

            //机舱转子
            ["转子接线箱A相"] = "A相",
            ["转子接线箱B相"] = "B相",
            ["转子接线箱C相"] = "C相",

            //机舱定子
            ["定子接线箱A相"] = "A相",
            ["定子接线箱B相"] = "B相",
            ["定子接线箱C相"] = "C相",

            //箱变
            ["高压侧A相"] = "A相",
            ["高压侧B相"] = "B相",
            ["高压侧C相"] = "C相",

            //箱变
            ["低压侧A相"] = "A相",
            ["低压侧B相"] = "B相",
            ["低压侧C相"] = "C相",

            //塔筒
            ["一级塔筒测点1"] = "测点1",
            ["一级塔筒测点2"] = "测点2",
            ["二级塔筒测点1"] = "测点1",
            ["三级塔筒测点1"] = "测点1",
            ["四级塔筒测点1"] = "测点1",
            ["四级塔筒测点2"] = "测点2",


        };
        public static BklAnalysisLog NewAnalysisLog(BklDeviceMetadata device, CaculateContext.AnalysisResult analysisResult, BklAnalysisRule dbRule, string levelString, string title, string contet)
        {
            var log = new BklAnalysisLog
            {
                Title = title,
                Content = contet,
                RecordedData = analysisResult.calcResult.ToString(),
                RecordedPicture = "",
                RecordedVideo = "",
                EndTime = DateTime.MinValue, 
                DeviceId = device.Id,
                FacilityId = device.FacilityId,
                Level = levelString,
                RuleId = dbRule.Id,
                StartTime = analysisResult.timeMin,
            };

            log.AlarmTimes = 1;
            log.HandleTimes = 0;
            log.OffsetStart = analysisResult.fromOffset;
            log.OffsetEnd = analysisResult.toOffset;
            log.Year = analysisResult.timeMin.Year;
            log.Day = analysisResult.timeMin.DayOfYear;
            log.DayOfMonth = analysisResult.timeMin.Day;
            log.HourOfDay = analysisResult.timeMin.Hour;
            log.DayOfWeek = analysisResult.timeMin.GetDayOfWeek();
            log.Week = analysisResult.timeMin.WeekOfYear();
            log.Month = analysisResult.timeMin.Month;
            return log;
        }
        public static void SetStatisticCount(BklAnalysisLog log, IRedisClient redisClient)
        {
            var dayofweek = log.StartTime.DayOfWeek == System.DayOfWeek.Sunday ? 7 : (int)log.StartTime.DayOfWeek;
            if (log.AlarmTimes == 1)
            {
                redisClient.IncrementValueInHash($"Statistic:Facility:{log.FacilityId}:year:{log.Year}", $"{log.Month}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Facility:{log.FacilityId}:month:{log.Year * 100 + log.Month}", $"{log.DayOfMonth}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Facility:{log.FacilityId}:week:{log.Year * 100 + log.Week}", $"{dayofweek}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Facility:{log.FacilityId}:day:{log.Year * 1000 + log.Day}", $"{log.HourOfDay}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:All:year:{log.Year}", $"{log.Month}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:All:month:{log.Year * 100 + log.Month}", $"{log.DayOfMonth}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:All:week:{log.Year * 100 + log.Week}", $"{dayofweek}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:All:day:{log.Year * 1000 + log.Day}", $"{log.HourOfDay}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Devices:year:{log.Year}", $"{log.DeviceId}.{log.Month}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Devices:month:{log.Year * 100 + log.Month}", $"{log.DeviceId}.{log.DayOfMonth}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Devices:week:{log.Year * 100 + log.Week}", $"{log.DeviceId}.{dayofweek}.{log.Level}", 1);
                redisClient.IncrementValueInHash($"Statistic:Devices:day:{log.Year * 1000 + log.Day}", $"{log.DeviceId}.{log.HourOfDay}.{log.Level}", 1);
            }
        }
        public class RedisGroupStatisticCountViewItem
        {
            public string level { get; set; }
            public int date { get; set; }
            public int count { get; set; }
        }
        public class RedisGroupStatisticCountView
        {

            public string group { get; set; }
            public string dateType { get; set; }
            public List<RedisGroupStatisticCountViewItem> values { get; set; }
        }
        public static Task<List<RedisGroupStatisticCountView>> GetCachedStatisticCount(BklDbContext context, IRedisClient redisClient, string datetype, string date, long factoryId, long facilityId, int deviceId)
        {
            (string group, string prop) = ParseDateType(datetype);

            return Task.Run(() =>
            {

                var values = redisClient.GetValuesFromHash($"Statistic:All:{datetype}:{date}");
                return values.Select(p => new RedisGroupStatisticCountViewItem
                {
                    level = p.Key.Split('.')[1],
                    date = int.Parse(p.Key.Split('.')[0]),
                    count = (p.Value.TryParse(out int val) ? val : 0)
                })
                    .GroupBy(p => p.level)
                    .Select(p => new RedisGroupStatisticCountView { group = p.Key, dateType = datetype, values = p.ToList() })
                    .ToList();
            });
        }
        public static Task<List<RedisGroupStatisticCountView>> GetPartitialStatistic(BklDbContext context, IRedisClient redisClient, string datetype, string date, long factoryId, long facilityId)
        {
            return Task.Run(() =>
            {

                var values = redisClient.GetValuesFromHash($"Statistic:All:{datetype}:{date}");
                return values.Select(p => new RedisGroupStatisticCountViewItem
                {
                    level = p.Key.Split('.')[1],
                    date = int.Parse(p.Key.Split('.')[0]),
                    count = (p.Value.TryParse(out int val) ? val : 0)
                })
                    .GroupBy(p => p.level)
                    .Select(p => new RedisGroupStatisticCountView { group = p.Key, dateType = datetype, values = p.ToList() })
                    .ToList();
            });
        }
        static Dictionary<string, string> DateMapper = new Dictionary<string, string>
        {
            ["month"] = "monthofyear",
            ["day"] = "dayofyear",
            ["week"] = "weekofyear",
            ["year"] = "year"
        };
        //select dev.probeName,log.level,count(log.id) from bkl_analysis_log log  join bkl_device_metadata dev on log.deviceId=dev.id group by dev.probeName, log.`level`
        /// <summary>
        /// 
        /// </summary>
        /// <param name="datetype">month,day,week,year</param>
        /// <param name="date">month:1~12,day:1-365</param>
        /// <returns>month:1~31,day:0-23,week:1~7,year:1~12</returns>
        public static async Task<List<CountGroupByProbeNameDetail>> CountGroupByProbeNameDetail(BklDbContext context,
         int year,
         string datetype,
          string date,
          long factoryId,
          long facilityId,
          long deviceId)
        {
            var con = context.Database.GetDbConnection();
            (string group, string prop) = ParseDateType(datetype);
            var sqlstr = new StringBuilder($"select  log.facilityId,dev.fullpath,  dev.groupName,dev.probeName,log.level,count(log.id) as count,log.{group} as TimeGroup from bkl_analysis_log log  ");
            sqlstr.Append($" join bkl_device_metadata dev on log.deviceId=dev.id ");
            sqlstr.Append($" where log.{prop} = @param ");
            sqlstr.Append($" and dev.factoryId = @faid ");
            if (deviceId > 0)
            {
                sqlstr.Append($" and log.deviceId = @did ");
            }
            if (facilityId > 0)
            {
                sqlstr.Append($" and log.facilityId = @fid ");
            }
            if (datetype != "year")
            {
                sqlstr.Append($" and  log.year = @year ");
            }
            sqlstr.Append($"group by dev.groupName, dev.probeName, log.`level`,log.{group}, log.facilityId");
            var result = (await con.QueryAsync<CountGroupByProbeNameDetail>(sqlstr.ToString(), new
            {
                did = deviceId,
                faid = factoryId,
                fid = facilityId,
                param = ParseDateTypeDate(date, datetype),
                year = year
            })).ToList();
            return result;
            //List<object> ret = new List<object>();
            //foreach (var sameProbe in result.GroupBy(p => p.ProbeName))
            //{
            //    object[] hoursOfDay = new object[24];
            //    for (int i = 0; i < 24; i++)
            //    {
            //        hoursOfDay[i] = new object[] { };
            //    }
            //    foreach (var sameTime in sameProbe.GroupBy(p => p.TimeGroup).OrderBy(p => p.Key))
            //    {
            //        hoursOfDay[int.Parse(sameTime.Key)] = sameTime.GroupBy(p => p.Level).Select(p => new { level = p.Key, value = p.Sum(q => q.Count) }).Cast<object>().ToArray();
            //    }
            //    ret.Add(new { area = sameProbe.Key, value = hoursOfDay });
            //}
            //return result;
        }
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
                    var str = datetypedate.Split('-')[1];
                    return int.Parse(str.Substring(0, str.Length - 2));
            }
            return 0;
        }
        public static (string group, string prop) ParseDateType(string datetype)
        {
            var group = "hourofday";
            var prop = "dayofyear";
            switch (datetype)
            {
                case "day":
                    group = "hourofday";
                    prop = "day";
                    break;
                case "week":
                    group = "dayofweek";
                    prop = "week";
                    break;
                case "month":
                    group = "dayofmonth";
                    prop = "month";
                    break;
                case "year":
                    group = "day";
                    prop = "year";
                    break;
                default:
                    break;
            }
            return (group, prop);
        }
        public static List<object> PartitialGroupCount(List<CountGroupByProbeNameDetail> groupViews)
        {
            List<object> list = new List<object>();
            foreach (var group in groupViews.GroupBy(item => item.groupName.Contains("塔筒") ? "tatong" : (item.groupName.Contains("箱变") ? "xiangbian" : "jicang")))
            {
                list.Add(new
                {
                    Name = group.Key,
                    Warn = group.Where(q => q.Level == "Warn").Sum(s => s.Count),
                    Error = group.Where(q => q.Level == "Error").Sum(s => s.Count)
                });
            }
            return list;
        }
        public static IEnumerable<object> JsonDataView(List<CountGroupByProbeNameDetail> lis, int timeGroupCount)
        {
            foreach (var sameGroup in lis.GroupBy(p => p.groupName))
            {
                List<TimeOrderdedGroupdByProbeName.GroupHeader> headers = new List<TimeOrderdedGroupdByProbeName.GroupHeader>();
                List<object> sameGroupValues = new List<object>();
                foreach (var sameProbe in sameGroup.GroupBy(q => q.ProbeName))
                {
                    var grops = sameProbe
                    .GroupBy(s => s.TimeGroup)
                    .Select(s => new
                    {
                        time = int.Parse(s.Key),
                        warn = s.Where(r => r.Level == "Warn").Sum(r => r.Count),
                        error = s.Where(r => r.Level == "Error").Sum(r => r.Count)
                    }).ToList();
                    foreach (var item in Enumerable.Range(1, timeGroupCount))
                    {
                        if (grops.Any(q => q.time == item))
                            continue;
                        grops.Add(new { time = item, warn = 0, error = 0 });
                    }
                    sameGroupValues.Add(new { probeName = sameProbe.Key, Series = grops.OrderBy(s => s.time) });
                    headers.Add(new TimeOrderdedGroupdByProbeName.GroupHeader
                    {
                        ProbeName = sameProbe.Key,
                        Warn = grops.Sum(s => s.warn),
                        Error = grops.Sum(s => s.error)
                    });
                }
                yield return new { headers, groupName = sameGroup.Key, probeValues = sameGroupValues };
            }
        }
        public static List<object> EchartDayView(List<CountGroupByProbeNameDetail> lis)
        {

            //发电机 发电机 Warn    2   8
            //刹车盘 刹车盘 Warn    2   8
            //发电机 发电机 Error   1   8
            //刹车盘 刹车盘 Error   1   8
            //转子接线箱 转子接线箱A相 Warn    1   8
            //转子接线箱 转子接线箱B相 Warn    1   8
            //转子接线箱 转子接线箱C相 Warn    1   8
            //转子接线箱 转子接线箱A相 Warn    1   9
            //转子接线箱 转子接线箱B相 Warn    1   9
            //转子接线箱 转子接线箱C相 Warn    1   9
            //发电机 发电机 Error   3   9
            //刹车盘 刹车盘 Error   3   9
            //发电机 发电机 Warn    1   9
            //刹车盘 刹车盘 Warn    1   9

            List<object> lisRet = new List<object>();
            foreach (var gp in lis.GroupBy(q => q.groupName))
            {
                var xAxis = new Dictionary<string, object>();
                xAxis.Add("type", "category");
                xAxis.Add("data", Enumerable.Range(1, 24).Select(h => $"{h}时").ToArray());

                var obj = new Dictionary<string, object>();



                switch (gp.Key)
                {
                    case "发电机":
                    case "刹车盘":
                        {
                            obj.Add(nameof(xAxis), xAxis);
                            obj.Add("yAxis", new { type = "value" });
                            obj.Add("legend", new { data = new string[] { "预警", "报警" } });
                            List<object> series = new List<object>();
                            foreach (var sameLevel in gp.GroupBy(p => p.Level))
                            {
                                series.Add(new
                                {
                                    level = sameLevel.Key,

                                    name = levelShowMap[sameLevel.Key],
                                    type = "bar",
                                    stack = "total",
                                    data = Enumerable.Range(1, 24).Select(hour => gp.Where(p => p.TimeGroup == hour.ToString() && sameLevel.Key == p.Level).Sum(q => q.Count)).ToArray()
                                }); ;
                            }
                            obj.Add(nameof(series), series);
                            obj.Add("name", gp.Key);
                            lisRet.Add(new { groupName = gp.Key, value = obj });
                        }
                        break;
                    default:
                        {
                            var probs = gp.Select(p => p.ProbeName).Distinct();
                            var cats = probs.Join(new string[] { "预警", "报警" }, p => "", q => "", (p, q) => $"{probeNameMap[p]}{q}").ToArray();
                            obj.Add(nameof(xAxis), xAxis);
                            obj.Add("yAxis", new { type = "value" });
                            List<dynamic> series = new List<dynamic>();
                            foreach (var sameProbe in gp.GroupBy(p => p.ProbeName))
                            {
                                foreach (var sameLevel in sameProbe.GroupBy(q => q.Level))
                                {
                                    series.Add(new
                                    {
                                        level = sameLevel.Key,
                                        name = $"{probeNameMap[sameProbe.Key]}{levelShowMap[sameLevel.Key]}",
                                        type = "bar",
                                        stack = sameProbe.Key,
                                        data = Enumerable.Range(1, 24)
                                                .Select(hour => gp.Where(p => p.TimeGroup == hour.ToString() && sameLevel.Key == p.Level)
                                                .Sum(q => q.Count)).ToArray()
                                    }); ;
                                }
                            }
                            obj.Add(nameof(series), series);
                            obj.Add("legend", new { data = series.Select(q => q.name).ToArray() });
                            obj.Add("name", gp.Key);
                            lisRet.Add(new { groupName = gp.Key, value = obj });
                        }
                        break;
                }

            }

            return lisRet.ToList();
        }

        public static async Task<IEnumerable<AnalysisLogGroupView>> QueryAnalysisLogs(
            BklDbContext context,
            int year,
            long factoryId,
            string datetype = null,
            string date = null,
            long facilityId = 0,
            long deviceId = 0,
            int page = 0,
            int pagesize = 10,
            int needVideo = 0)
        {
            var con = context.Database.GetDbConnection();
            string group = "hourofday";
            string prop = "dayofyear";
            if (!string.IsNullOrEmpty(datetype))
            {
                switch (datetype)
                {
                    case "day":
                        group = "hourofday";
                        prop = "day";
                        break;
                    case "week":
                        group = "dayofweek";
                        prop = "week";
                        break;
                    case "month":
                        group = "dayofmonth";
                        prop = "month";
                        break;
                    case "year":
                        group = "day";
                        prop = "year";
                        break;
                    default:
                        break;
                }
            }
            var sqlstr = new StringBuilder($"select log.recordedVideo,log.recordedData,dev.id as deviceId,  dev.groupName,dev.probeName,log.id,log.ruleId,log.level,log.starttime,log.endtime");
            if (pagesize > 0)
            {
                sqlstr.Append(",log.title,log.content ");
            }
            sqlstr.Append(" from bkl_analysis_log log  ");
            sqlstr.Append($" join bkl_device_metadata dev on log.deviceId=dev.id ");
            sqlstr.Append($" where 1=1 ");
            if (!string.IsNullOrEmpty(datetype))
            {
                sqlstr.Append($"and {prop} = @param ");
            }
            // and log.facilityId=@fid and 
            if (facilityId > 0)
            {
                sqlstr.Append($"and  log.facilityId=@fid ");
            }
            if (factoryId > 0)
            {
                sqlstr.Append($"and  dev.factoryId=@faid ");
            }
            if (deviceId > 0)
            {
                sqlstr.Append($"and  log.deviceId=@did ");
            }
            if (year > 0)
            {
                sqlstr.Append($"and  log.year=@year ");
            }
            if (needVideo == 1)
            {
                sqlstr.Append($"and  log.recordedVideo!=''");
            }
            sqlstr.Append(" order by id desc");
            if (pagesize > 0)
            {
                sqlstr.Append(" limit @page,@pagesize ");
            }
            return await con.QueryAsync<AnalysisLogGroupView>(sqlstr.ToString(), new
            {
                param = string.IsNullOrEmpty(date) ? 0 : ParseDateTypeDate(date, datetype),
                fid = facilityId,
                faid = factoryId,
                did = deviceId,
                year = year,
                page = page * pagesize,
                pagesize = pagesize
            });
        }

        public static IEnumerable<object> GetGroupByMinuteViewEcharts(IEnumerable<AnalysisLogGroupView> logs)
        {
            var data = GetGroupByMinuteView(logs);
            //转子接线箱
            foreach (var item in data.GroupBy(q => q.group))
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("name", item.Key);
                dic.Add("xAxis", new { type = "value", });
                dic.Add("yAxis", new { type = "category", data = item.Select(q => probeNameMap[q.probe]).Distinct().ToArray() });
                var header = new TimeOrderdedGroupdByProbeName.GroupHeader();
                List<TimeOrderdedGroupdByProbeName.GroupHeader> headers = new List<TimeOrderdedGroupdByProbeName.GroupHeader>();

                List<object> series = new List<object>();
                int nonce = 0;
                foreach (var group in item)
                {
                    var vals = group.values.Select(p => p.now).ToList();
                    var ex = vals.Average();
                    var dx = vals.Select(q => Math.Pow(q - ex, 2)).Average();
                    Func<int, double> std = (int a) => (a - ex) / Math.Sqrt(dx);
                    // var dic1 = vals.Select(std);
                    // var minAbs = dic1.Min();
                    // minAbs = minAbs < 0 ? Math.Abs(minAbs) : 0;
                    var head = new TimeOrderdedGroupdByProbeName.GroupHeader();
                    head.ProbeName = probeNameMap[group.probe];
                    head.Error = group.values.Count(p => p.level == "Error");
                    head.Warn = group.values.Count(p => p.level == "Warn");
                    headers.Add(head);
                    foreach (var val in group.values)
                    {
                        List<object> datas = new List<object>();
                        foreach (var i in Enumerable.Range(1, nonce))
                        {
                            datas.Add("");
                        }
                        datas.Add(2000 + 500 * (std(val.now)));
                        series.Add(new
                        {
                            color = val.level == "Error" ? "red" : (val.level == "Warn" ? "orange" : "green"),
                            name = probeNameMap[group.probe],
                            label = new { show = true, formatter = val.time },
                            type = "bar",
                            stack = "s1",
                            data = datas
                        });
                    }
                    nonce++;
                }
                dic.Add("series", series);
                dic.Add("headers", headers);
                yield return dic;
            }

        }
        public static IEnumerable<TimeOrderdedGroupdByProbeName> GetGroupByMinuteView(IEnumerable<AnalysisLogGroupView> logs)
        {
            foreach (var item in logs.GroupBy(q => q.groupName))
            {
                foreach (var log in item.GroupBy(s => s.probeName))
                {
                    List<TimeOrderedSeries> series = new List<TimeOrderedSeries>();
                    int start = 0;
                    int index = 0;
                    int time = 0;
                    foreach (var order in log.OrderBy(p => p.starttime))
                    {
                        var now = (int)order.starttime.TimeOfDay.TotalSeconds - start;
                        time += now;
                        series.Add(new TimeOrderedSeries { order = index++, now = now, level = "normal", time = TimeSpan.FromSeconds(time).ToString() });
                        start = (int)order.starttime.TimeOfDay.TotalSeconds;
                        now = (int)order.endtime.TimeOfDay.TotalSeconds - start;
                        time += now;
                        series.Add(new TimeOrderedSeries { order = index++, now = now, level = order.level, time = TimeSpan.FromSeconds(time).ToString() });
                        start = (int)order.endtime.TimeOfDay.TotalSeconds;
                    }
                    time += series.Last().now;
                    series.Add(new TimeOrderedSeries { order = index++, now = 86400 - start, level = "normal", time = TimeSpan.FromSeconds(time).ToString() });
                    yield return new TimeOrderdedGroupdByProbeName { group = item.Key, probe = log.Key, values = series.ToList() };
                }
            }
        }
    }
    public class TimeOrderdedGroupdByProbeName
    {
        public class GroupHeader
        {
            public string ProbeName { get; set; }

            public int Error { get; set; }
            public int Warn { get; set; }
        }

        public string group { get; set; }
        public string probe { get; set; }
        public List<TimeOrderedSeries> values { get; set; }
    }

    public class TimeOrderedSeries
    {
        public int order { get; set; }
        public int now { get; set; }
        public string level { get; set; }
        public string time { get; set; }
    }
    public class AnalysisLogGroupView
    {
        public long deviceId { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string groupName { get; set; }
        public string probeName { get; set; }
        public long id { get; set; }
        public long ruleId { get; set; }
        public string level { get; set; }
        public string recordedData { get; set; }
        public string recordedVideo { get; set; }
        public DateTime starttime { get; set; }
        public DateTime endtime { get; set; }
    }
}
