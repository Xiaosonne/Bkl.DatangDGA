using Bkl.Models;
using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
namespace Bkl.Dst.Interfaces
{
    public interface IDeviceGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 更新modbus类设备的状态
        /// </summary>
        Task UpdateStatus(DeviceUpdateStatus deviceStatusItem);
        Task UpdateManyStatus(DeviceUpdateStatus[] deviceStatusItem);
        Task SetStatus(WriteDeviceStatusRequest writeRequest);

        Task SetAlarm(DeviceAlarmResult item);

        Task<DeviceUpdateStatus[]> GetStatus();
        Task<BklDeviceMetadata> GetDevice();
        Task<DeviceAlarmEntry[]> GetAlarms();
    }
}
