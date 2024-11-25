using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bkl.Models
{
    [Table("modbus_device_pair")]
    public class ModbusDevicePair
    {
        public ModbusDevicePair()
        {
            Id = SnowId.NextId();
        }
        [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; } 
        [Required ]public long DeviceId { get; set; }
        [Required] public byte BusId { get; set; }
        [Required] public long ConnectionId { get; set; }
        /// <summary>
        /// 设备类型-设备属性 attribute id
        /// </summary>
        [Required] public long NodeId { get; set; }
        /// <summary>
        /// 起始地址偏移量
        /// </summary>
        [Required] public short NodeIndex { get; set; }
        [MaxLength(20), Required] public string ProtocolName { get; set; }
       
        [Required]
        public string ConnUuid { get; set; }
    }
}
