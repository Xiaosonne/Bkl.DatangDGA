using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bkl.Models
{
    public class ModbusDevicePair
    {
        public ModbusDevicePair()
        {
        }
        
        public long Id { get; set; } 
         public long DeviceId { get; set; }
         public byte BusId { get; set; }
         public long ConnectionId { get; set; }
        /// <summary>
        /// 设备类型-设备属性 attribute id
        /// </summary>
         public long NodeId { get; set; }
        /// <summary>
        /// 起始地址偏移量
        /// </summary>
         public short NodeIndex { get; set; }
          public string ProtocolName { get; set; }
       
         
        public string ConnUuid { get; set; }
    }
}
