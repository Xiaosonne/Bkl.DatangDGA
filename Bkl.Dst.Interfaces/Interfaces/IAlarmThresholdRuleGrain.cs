using Bkl.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{
    public interface IAlarmThresholdRuleGrain:IGrainWithStringKey
    {
         Task<List<DeviceAlarmMatchResults>> OnStatusUpdate(DeviceUpdateStatus status);
    }
}
    