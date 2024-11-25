using System.Drawing;

namespace Bkl.Models
{
    public class ThermalRule
    {
        public Point[] Points { get; set; }
        public string Shape { get; set; }
        public Point RuleImageSize { get; set; }
        public int MaxTemperature { get; set; }
        public int MinTemperature { get; set; }
        public int AverageTemperature { get; set; }
    }
    #region 类定义
    public enum ModbusReadType
    {
        ReadCoils = 0x01,
        ReadInputs = 0x02,
        ReadHoldingRegister = 0x03,
        ReadInputRegister = 0x04,
        WriteSingleCoil = 0x05,
        WriteSingleInput = 0x06,
        WriteCoils = 0x0F,
        WriteInputs = 0x10
    }
    public enum ModbusDataType
    {
        dt_float = 10,
        dt_int16 = 20,
        dt_uint16 = 30,
        dt_int32 = 40,
        dt_uint32 = 50
    }
    public enum ModbusByteDataOrder
    {

        ABCD = 1234,
        BADC = 2143,
        CDAB = 3412,
        DCBA = 4321,
        AB = 12,
        BA = 21,
        None = 70
    }
    public class ModbusInfo
    {
        public ModbusInfo(string modbusType, ModbusReadType readType, byte busid, ushort startAddress, ushort dataOffset)
        {
            this.modbusType = modbusType;
            this.readType = readType;
            this.busid = busid;
            this.startAddress = startAddress;
            this.dataOffset = dataOffset;
        }

        public string modbusType { get; private set; }

        public ModbusReadType readType { get; private set; }
        public byte busid { get; private set; }
        public ushort startAddress { get; private set; }
        public ushort dataOffset { get; private set; }
        public void Deconstruct(out string modbusType,out ModbusReadType readType,out byte busid,out ushort startAddress,ushort dataOffset)
        {
            modbusType = this.modbusType;
            readType = this.readType;
            busid = this.busid;
            startAddress = this.startAddress;
            dataOffset = this.dataOffset;
        }
    }
    

    public class ThermalInfo
    {
        public ThermalInfo(string ip, int port, string username, string password, int channel, string thermalUrl, string visibleUrl)
        {
            this.port = port;
            this.ip = ip;
            this.username = username;
            this.password = password;
            this.thermalUrl = thermalUrl;
            this.visibleUrl = visibleUrl;
            this.channel = channel;
        }
        public string ip { get; set; }
        public int port { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int channel { get; set; }
        public string thermalUrl { get; set; }
        public string visibleUrl { get; set; }

        public void Deconstruct(out string ip, out int port, out string username, out string password, out int channel, out string thermalUrl, out string visibleUrl)
        {
            ip = this.ip;
            port = this.port;
            username = this.username;
            password = this.password;
            channel = this.channel;
            thermalUrl = this.thermalUrl;
            visibleUrl = this.visibleUrl;

        }
    }
    public class IPInfo
    {
        public IPInfo(string ipkind, string ip, int port)
        {
            this.ipKind = ipkind;
            this.ip = ip;
            this.port = port;
        }
        public string ipKind { get; private set; }
        public string ip { get; private set; }
        public int port { get; private set; }
        public void Deconstruct(out string ipKind, out string ip, out int port)
        {
            ipKind = this.ipKind;
            ip = this.ip;
            port = this.port;
        }
    }
    #endregion
    public class DeviceConnectionString
    {
        /// <summary>
        /// camera visible 
        /// </summary>
        public string local { get; set; } 
        /// <summary>
        /// camera thermal
        /// </summary>
        public string remote { get; set; }

        public string proto { get; set; }

        #region  方法定义
        public IPInfo GetIPInfo()
        {
            var arr = local.Split(':');
            return new IPInfo(arr[0], arr[1].TrimStart('/'), int.Parse(arr[2]));
        }
        //"modbus://03/21/1501/04"
        ///"modbus://03/21/1501/04" readType/busid/startaddress/nodeid
        public ModbusInfo GetModbusInfo()
        {
            var arr = proto.Split('/');
            return new ModbusInfo(proto.Split(':')[0], (ModbusReadType)int.Parse(arr[2]), byte.Parse(arr[3]), ushort.Parse(arr[4]), ushort.Parse(arr[5]));

        }
        //{
        //  "local":"rtsp://admin:admin12345@192.168.50.100:554/Streaming/Channels/201?transportmode=unicast",
        //  "visible":"rtsp://admin:admin12345@192.168.50.100:554/Streaming/Channels/101?transportmode=unicast",
        //  "proto":"hkthermal://admin/admin12345/192.168.50.100/8000/2/rule1/rule2"
        //}
        //"proto":"hkthermal://admin/bakala123456/192.168.1.64/8000/2/rule1/rule2"
        public ThermalInfo GetThermalInfo()
        {
            var arr = proto.Split('/');
            return new ThermalInfo(arr[4], int.Parse(arr[5]), arr[2], arr[3], int.Parse(arr[6]),  remote, local);
        }
        #endregion
    }
}
