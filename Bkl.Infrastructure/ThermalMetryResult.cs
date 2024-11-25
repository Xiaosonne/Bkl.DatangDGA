using System;

namespace Bkl.Infrastructure
{
    public class LinkageMatchedItem
    {
        public long DeviceId { get; set; }
        public long RuleId { get; set; }
        public string StatusName { get; set; }
        public string CurrentValue { get; set; }
        public string TargetValue { get; set; }
        public bool Matched { get; set; }
        public DateTime Lasttime { get; set; }
    }
    public class ThermalMetryResult : ITimeData
    {
        public int regionType { get; set; }
        public string ruleName { get; set; }
        public double value { get; set; }
        public double minTemp { get; set; }
        public double averageTemp { get; set; }
        public long time { get; set; }
        public double[] highPoints { get; set; }
        public double[] lowPoints { get; set; }
        public long deviceId { get; set; }
        public int ruleId { get; set; }
        public long factoryId { get; set; }
        public long facilityId { get; set; }
    }

}
