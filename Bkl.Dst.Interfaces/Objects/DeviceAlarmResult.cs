using System;

namespace Bkl.Dst.Interfaces
{
    public enum DeviceAlarmType
    {
        Normal = 40,
        Warn = 50,
        Error = 60,
        Linkage=70,
    }
    public enum DeviceAlarmTypeCN
    {
        正常 = 40,
        预警 = 50,
        告警 = 60,
        联动 = 70,
    }
    public class DeviceControlState
    {
        public long PairId { get; set; }
        public DateTime LastChanged { get; set; }
        public string SourceId { get; set; }
        public long AttributeId { get; set; }
        public string Status { get; set; }
    }
    public class DeviceAlarmResult
    {
        public DeviceAlarmResult()
        {
            CreateTime = DateTime.Now;
        }
        public DateTime CreateTime { get; set; }
        public string SourceType { get; set; }
        public double AlarmValue { get; set; }

        public DeviceAlarmType AlarmLevel { get; set; }

        public long AlarmId { get; set; }

        public string AlarmName { get; set; }

        public string AlarmExtraInfo { get; set; }

        public string AlarmProbeName { get; set; }

        public double AlarmMax { get; set; }

        public double AlarmMin { get; set; }

        public string DeviceStatusName { get; set; }

        public long DeviceId { get; set; }
        public long FactoryId { get; set; }
        public long FacilityId { get; set; }
        public string FacilityDetailPosition { get; set; }
        public long PairId { get; set; }
        public DateTime LastReport { get; set; }
        public string DeviceStatusNameCN { get; set; }
        public int LastCount { get; set; }
        public string Method { get; set; }
        public long AttributeId { get; set; }
        public long DataId { get; set; }
    }
}
