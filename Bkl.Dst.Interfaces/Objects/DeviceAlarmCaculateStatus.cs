using Bkl.Models;
using System;

namespace Bkl.Dst.Interfaces
{
    public class DeviceAlarmCaculateStatus
    {
        public DateTime LastProcced { get; set; }

        public int Count { get; set; }

        public double PreValue { get; set; }

        public double CurrentValue { get; set; }

        public double Accumulate { get; set; }

        public double IncrementalAccumulate { get; set; }

        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double AverageValue { get; set; }
        public string StatusName { get; set; }
        public long PairId { get; set; }
        public DeviceUpdateStatus CurrentStatus { get; set; }
        public DateTime LastUpdate { get; set; }
        public long AttributeId { get; set; }
    }
}
