using Bkl.Infrastructure;
using System;
namespace System
{
    using System;
    using System.Globalization;

    public static class TimeExtension
    {
        public static int GetDayOfWeek(this DateTime now)
        {
            return now.DayOfWeek == System.DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
        }
        public static int WeekOfYear(this DateTime now)
        {
            CultureInfo ci = new CultureInfo("zh-CN");
            System.Globalization.Calendar cal = ci.Calendar;
            CalendarWeekRule cwr = ci.DateTimeFormat.CalendarWeekRule;
            DayOfWeek dow = DayOfWeek.Monday;
            int week = cal.GetWeekOfYear(now, cwr, dow);
            return week;
        }

        static DateTime unixExpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static int UnixEpoch(this DateTime time)
        {
            return (int)time.ToUniversalTime().Subtract(unixExpoch).TotalSeconds;
        }
        public static DateTime UnixEpochBack(this long time)
        {
            return unixExpoch.AddSeconds(time).ToLocalTime();
        }
        public static DateTime UnixEpochBack(this int time)
        {
            return unixExpoch.AddSeconds(time).ToLocalTime();
        }
    }
}
namespace Bkl.Models
{

    public class BandageSensorTemperatureData : ITimeData
    {
        public long facilityId { get; set; }

        public long deviceId { get; set; }

        public int node { get; set; }
        public double value { get; set; }
        public string name { get; set; }
        public long time { get; set; }
        public long factoryId { get; set; }

        public static BklDeviceStatus ToStatus(BandageSensorTemperatureData p, string timeType = "s")
        {
            DateTime localtime = p.time.UnixEpochBack();
            return new BklDeviceStatus
            {
                GroupName = "#",
                DeviceRelId = p.deviceId,
                FactoryRelId = p.factoryId,
                FacilityRelId = p.facilityId,
                StatusName = p.name,
                StatusValue = p.value,
                Createtime = DateTime.UtcNow,
                Time = timeType == "s" ? long.Parse(localtime.ToString("yyyyMMddHHmmss")) : long.Parse(localtime.ToString("yyyyMMddHHmm")),
                TimeType = timeType
            };
        }
    }
}
