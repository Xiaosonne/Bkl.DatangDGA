using Bkl.Models;
using System;
using System.Collections.Generic;

namespace Bkl.Dst.Interfaces
{
    public class DeviceAlarmMatchResults
    {
        public List<DeviceAlarmResult> AlarmResults { get; set; }
        public List<BklLinkageAction> Actions { get; set; }
        public long LinkageActionId { get; set; }
    }
    public class DeviceAlarmEntry
    {
        public string Key { get; set; }
        public string Level { get; set; }
        public DateTime LastUpdate { get; set; }
        public string StatusName { get; set; }
        public string StatusNameCN { get; set; }
    }
  
}
