using Bkl.Models;
using System;
using System.Linq;

namespace Bkl.Dst.Interfaces
{

    public class WriteDeviceStatusRequest
    {
        public WriteDeviceStatusRequest()
        {
            Createtime = DateTime.Now;
        }
        public string ConnUuid { get; set; }
        public byte[] Data { get; set; }
        public long DeviceId { get; set; }
        public long AttributeId { get; set; }
        public byte BusId { get; set; }
        public string ProtocolName { get; set; }
        public long PairId { get; set; }
        public string SourceId { get; set; }
        public DateTime Createtime { get;  set; }
    } 

    public class DeviceReadRequest
    {
        public long DeviceId { get; set; }
        public ushort StartAddress { get; set; }
        public ushort NumberOfPoints  { get; set; }
        public long AttributeId { get; set; }
        public long PairId { get; set; }
        public byte BusId { get; set; }
        public ModbusNodeInfo Node { get; set; }
        public string ProtocolName { get; set; }
    
    }
}
