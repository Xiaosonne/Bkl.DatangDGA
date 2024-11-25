using Bkl.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{
    public interface IModbusGrain : IGrainWithStringKey
    {
        Task Weakup();
        Task WriteStatus(  WriteDeviceStatusRequest writeRequest);
        Task<List<DeviceUpdateStatus>> ReadStatus(); 
    }
}
