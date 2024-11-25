using Bkl.Models;
using System.Collections.Generic;

namespace Bkl.Dst.Interfaces
{
    public class DeviceAlarmStatus
    {
        public BklDeviceMetadataRef DeviceMetadata { get; set; } 

        public List<DeviceAlarmCaculateStatus> DeviceRuleCaculates { get; set; }
    }
}
