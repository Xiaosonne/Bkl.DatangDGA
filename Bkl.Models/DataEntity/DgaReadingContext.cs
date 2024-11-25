using System.Collections.Generic;

namespace Bkl.Models
{
    public class DgaReadingContext
    {
        public BklDeviceMetadata Device { get; set; }
        public ModbusConnInfo Connection { get; set; }
        public List<ModbusDevicePair> Pairs { get; set; }
        public List<ModbusNodeInfo> Nodes { get; set; }
    }

}
