using Bkl.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{
    public interface IAnalysisDGAGrain :IGrainWithStringKey
    {
        Task Subscribe(long deviceId);
        Task<DeviceStatusItem[]> GetStatus();
    }
}
