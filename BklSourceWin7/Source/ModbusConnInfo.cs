using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bkl.Models
{
    public class ModbusConnInfo 
    {
        public ModbusConnInfo() 
        {
        }
        public long Id { get; set; }
        /// <summary>
        /// modbusrtu modbusrtuovertcp modbustcp
        /// </summary>
         public string ModbusType { get; set; }
        /// <summary>
        /// serialport
        /// tcpclient
        /// tcpserver
        /// udp
        /// </summary>
         public string ConnType { get; set; }
        /// <summary>
        /// COM1,9600,N,8,1
        ///192.168.3.100:2020 主动连接
        ///127.0.0.1:8000     被动接收
        ///127.0.0.1:4500 
        /// </summary>
         public string ConnStr { get; set; }

          public string Uuid { get; set; }

    }
}
