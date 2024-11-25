using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bkl.Models 
{
    /// <summary>
    /// 设备类型-设备属性列表
    /// </summary>
    [Table("modbus_node_info")]
    public class ModbusNodeInfo
    {
        public ModbusNodeInfo()
        {
            Id = SnowId.NextId();
        }
        [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        [MaxLength(20), Required] public string ProtocolName { get; set; }
        [Required] public ModbusReadType ReadType { get; set; }
        [Required] public short StartAddress { get; set; }
        [Required] public byte DataSize { get; set; }
        [Required] public ModbusDataType DataType { get; set; }
        [Required] public ModbusByteDataOrder DataOrder { get; set; }
        [Required] public byte NodeCount { get; set; }
        [MaxLength(10), Required] public string Scale { get; set; }
        [MaxLength(30), Required] public string StatusName { get; set; }
        [MaxLength(30), Required] public string StatusNameCN { get; set; }
        [MaxLength(30), Required] public string Unit { get; set; }
        [MaxLength(30), Required] public string UnitCN { get; set; }
        [MaxLength(100), Required] public string ValueMap { get; set; }
    }
}
