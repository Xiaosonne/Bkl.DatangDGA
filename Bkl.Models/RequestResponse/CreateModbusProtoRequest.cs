using System.Collections.Generic;
using static Bkl.Models.CreateModbusProtoRequest;

namespace Bkl.Models
{
    public class ModbusAttribute
    {
        public string StatusName { get; set; }
        public string StatusNameCN { get; set; }
        public string UnitCN { get; set; }
        public string Unit { get; set; }
        public string ReadType { get; set; }
        public ushort StartAddress { get; set; }
        public byte DataSize { get; set; }
        public string DataType { get; set; }
        public string DataOrder { get; set; }
        public string Scale { get; set; }
        public List<ValueMapItem> ValueMap { get; set; }

    }
    public class EditModbusProtoRequest
    {
        public class EditItem
        {
            public long PairId { get; set; }
            public byte BusId { get; set; }
            public ModbusAttribute Attribute { get; set; }
        }

        public EditItem[] Data { get; set; }
        public string ProtocolName { get; set; }
    }
    public class CreateModbusProtoRequest
    {
        public class ValueMapItem
        {
            public string key { get; set; }
            public string name { get; set; }
        }
       
        public ModbusAttribute[] Attributes { get; set; }
        public string ProtoName { get; set; }
    }
}
