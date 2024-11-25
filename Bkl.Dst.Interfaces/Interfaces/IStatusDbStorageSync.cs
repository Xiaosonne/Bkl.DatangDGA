using Bkl.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{
    public interface IAlarmDbStorageSync : IGrainWithStringKey
    {
        Task StoreAlarm(DeviceAlarmResult deviceStatus);
    }
    public interface IStatusDbStorageSync:IGrainWithStringKey
    {
        Task Store(DeviceUpdateStatus deviceStatus);
       
    }
}
