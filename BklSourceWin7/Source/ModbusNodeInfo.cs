using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bkl.Models 
{

    public enum ModbusDataType
    {
        dt_float = 10,
        dt_int16 = 20,
        dt_uint16 = 30,
        dt_int32 = 40,
        dt_uint32 = 50
    }
    /// <summary>
    /// 设备类型-设备属性列表
    /// </summary>
    public class ModbusNodeInfo
    {
        public ModbusNodeInfo()
        {
        }
        public long Id { get; set; }
         public string ProtocolName { get; set; }
         public int ReadType { get; set; }
         public short StartAddress { get; set; }
         public byte DataSize { get; set; }
         public ModbusDataType DataType { get; set; }
         public int DataOrder { get; set; }
         public byte NodeCount { get; set; }
         public string Scale { get; set; }
         public string StatusName { get; set; }
         public string StatusNameCN { get; set; }
         public string Unit { get; set; }
         public string UnitCN { get; set; }
         public string ValueMap { get; set; }
    }
}
