using Bkl.Models;
using System;
namespace Bkl.Dst.Interfaces
{
    public class HttpGainId
    {
        public HttpGainId(string conStr)
        {
            ConStr = conStr;
        }
        public string ConStr { get; set; }
        public static implicit operator String(HttpGainId grain)
        {
            return $"Http{grain.ConStr}";
        }
    }
    public class ModbusGrainId
    {
        public ModbusGrainId(string uuid)
        {
            _uuid = uuid;
        }
        string _uuid;

        public string Uuid { get => _uuid; set => _uuid = value; }

        public static implicit operator String(ModbusGrainId deviceGrainId)
        {
            return $"{deviceGrainId.Uuid}";
        }
    }
    public class DeviceGrainId
    {
        private long deviceId;
        public DeviceGrainId(BklDeviceMetadataRef deviceRef)
        {
            deviceId = deviceRef.Id;
        }
        public DeviceGrainId(long deviceId)
        {
            this.deviceId = deviceId;
        }
        public static implicit operator String(DeviceGrainId deviceGrainId)
        {
            return $"device{deviceGrainId.deviceId}";
        }
    }
}
