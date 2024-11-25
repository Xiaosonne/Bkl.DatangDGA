using Bkl.Infrastructure;
using Bkl.Models;
using NModbus;
using NModbus.IO;
using System;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Bkl.Models
{
    public static class ModbusHelper
    {
        static ModbusFactory modbusFactory = new ModbusFactory();
        public static ushort[] ReadFromMaster(ModbusInfo modinfo, BklDeviceMetadata device, NModbus.IModbusMaster dataReader)
        {
            var nodeCount = (ushort)(modinfo.dataOffset + 1);
            ushort[] result = null;
            try
            {

                switch (modinfo.readType)
                {

                    case ModbusReadType.ReadHoldingRegister:
                        {
                            result = dataReader.ReadHoldingRegisters(modinfo.busid, modinfo.startAddress, nodeCount);

                        }
                        break;
                    case ModbusReadType.ReadInputRegister:
                        {
                            result = dataReader.ReadInputRegisters(modinfo.busid, modinfo.startAddress, nodeCount);

                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return result;
        }
        public static async Task<IModbusMaster> ConnectAsync(string modbusType, string ip, int port, string ipKind, CancellationToken token)
        {
            object retObj = null;
            IModbusMaster master = null;
            try
            {
                switch (ipKind)
                {
                    case "udp":
                        {
                            var udpClient = new UdpClient();
                       
                            udpClient.Connect(IPAddress.Parse(ip), port);
                            udpClient.Client.SendTimeout = 1000;
                            udpClient.Client.ReceiveTimeout = 1000;
                            retObj = udpClient;
                            master = modbusFactory.CreateRtuMaster(new UdpClientAdapter(udpClient));
                            int i=udpClient.Send(new byte[0] { }, 0);
                        }
                        break;
                    case "tcp":
                    case "tcpclient":
                        {
                            var tcpClient = new TcpClient();  
                            var socket = await SocketHelper.TcpConnectAsync(IPAddress.Parse(ip), port, token);
                            if (socket == null || !socket.Connected)
                                return null;
                            tcpClient.Client = socket;
                            //await tcpClient.ConnectAsync(IPAddress.Parse(ip), port);
                            retObj = tcpClient;
                            switch (modbusType)
                            {
                                case "modbus":
                                case "modbusrtu":
                                case "modbusrtuovertcp":
                                    master = modbusFactory.CreateRtuMaster(new TcpClientAdapter(tcpClient));
                                    break;
                                case "modbusip":
                                case "modbustcp":
                                    master = modbusFactory.CreateMaster(tcpClient);
                                    break;
                                default:
                                    master = default(IModbusMaster);
                                    break;
                            }
                        }
                       
                        break;
                    default:
                        throw new ArgumentException(ipKind + " ipKind error ");
                }

                return master;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        } 
        
    }
}
