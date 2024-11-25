//using Bkl.Models;
//using System.Net;
//using System.Net.Sockets;

//public class UDPSerialTask : IReadTask
//{
//    public BklDeviceMetadata Device { get; set; }
//    public List<ModbusDevicePair> Pairs { get; set; }
//    public List<ModbusNodeInfo> Nodes { get; set; }

//    public string Uuid { get; set; }

//    private ModbusConnInfo _connection;

//    public async Task Init(DgaReadingContext context)
//    {
//        Device = context.Device;
//        Uuid = context.Connection.Uuid;
//        _connection = context.Connection;
//        //connectionId busId protocol  device
//        Pairs = context.Pairs.ToList();
//        Nodes = context.Nodes.ToList();

//    }


//    public async IAsyncEnumerable<DeviceState[]> QueryAsync(int readInterval, CancellationToken token)
//    {
//        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//        await socket.ConnectAsync(IPEndPoint.Parse(_connection.ConnStr));
//        yield return null;
//    }
//    static byte[] ReadFormat = new byte[] {
//        0x6a, 0x80, 0x5a, 0x91, 0x01,
//        0x01,//addr,

//        0x48,//code,
//        0x03,//length
//        0x50,
//        0x00,//ll
//        0x00,//lh
//        0x00,//hl
//        0x00,//hh
//        0x90,//suml
//        0x00,//sumh
//        0x02,
//    };
//    //public struct Response {
//    //    byte x6a;
//    //    byte x80;
//    //    byte x5a;
//    //    byte x91;
//    //    byte x01;
//    //    byte x01;
//    //}
//    public void Unload()
//    {
//    }

//}
