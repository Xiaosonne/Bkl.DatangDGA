using Bkl.Infrastructure;
using NModbus;
using NModbus.IO;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Bkl.Infrastructure
{

    public class RtuOverIPMaster : IModbusMaster
    {
        protected Socket _socket;
        protected IModbusMaster _master;
        protected IStreamResource _streamResource;
        protected static ModbusFactory _factory = new ModbusFactory();
        public IModbusTransport Transport => _master.Transport;
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public void Dispose()
        {
            try { _master.Dispose(); _master = null; } catch { }
            try { _socket.Close(); } catch { }
            _master = null;
            _socket = null;
        }
        public virtual Task<IModbusMaster> ConnectAsync(IPAddress ip, int port, CancellationToken token)
        {
            return Task.FromResult((IModbusMaster)this);
        }
        public TResponse ExecuteCustomMessage<TResponse>(IModbusMessage request) where TResponse : IModbusMessage, new()
        {
            return _master.ExecuteCustomMessage<TResponse>(request);
        }

        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
        }

        public Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints);
        }

        public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadHoldingRegistersAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints);
        }

        public Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadInputRegistersAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadInputs(slaveAddress, startAddress, numberOfPoints);
        }

        public Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadInputsAsync(slaveAddress, startAddress, numberOfPoints);
        }

        public ushort[] ReadWriteMultipleRegisters(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        {
            return _master.ReadWriteMultipleRegisters(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData);
        }

        public Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        {
            return _master.ReadWriteMultipleRegistersAsync(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData);
        }

        public void WriteFileRecord(byte slaveAdress, ushort fileNumber, ushort startingAddress, byte[] data)
        {
            _master.WriteFileRecord(slaveAdress, fileNumber, startingAddress, data);
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        {
            _master.WriteMultipleCoils(slaveAddress, startAddress, data);
        }

        public Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] data)
        {
            return _master.WriteMultipleCoilsAsync(slaveAddress, startAddress, data);
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            _master.WriteMultipleRegisters(slaveAddress, startAddress, data);
        }

        public Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            return _master.WriteMultipleRegistersAsync(slaveAddress, startAddress, data);
        }

        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            _master.WriteSingleCoil(slaveAddress, coilAddress, value);
        }

        public Task WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, bool value)
        {
            return _master.WriteSingleCoilAsync(slaveAddress, coilAddress, value);
        }

        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            _master.WriteSingleRegister(slaveAddress, registerAddress, value);
        }

        public Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value)
        {
            return _master.WriteSingleRegisterAsync(slaveAddress, registerAddress, value);
        }
    }
    public class TcpClientMaster : RtuOverIPMaster
    {
        private TcpClient _tcpClient;
        private bool _modbusTCP;
        public bool ModbusTCP { set { _modbusTCP = value; } }

        public TcpClientMaster()
        {
        }
        public override async Task<IModbusMaster> ConnectAsync(IPAddress ip, int port, CancellationToken token)
        {
            _socket = await SocketHelper.TcpConnectAsync(ip, port, token);
            if (_socket == null || !_socket.Connected)
                return null;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Blocking = false;
            bool connected = false;
            try
            {
                _socket.Connect(ip, port);
                connected = true;
            }
            catch (Win32Exception ex)
            {
                if (ex.ErrorCode == 10035)
                {
                    while (!token.IsCancellationRequested && !connected)
                    {
                        connected = _socket.Poll(50, SelectMode.SelectWrite);
                    }
                }
                else
                {
                    Console.WriteLine(ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ip}:{port} ConnectError {ex}");
                return null;
            }
            _socket.Blocking = true;

            _tcpClient = new TcpClient();
            _tcpClient.Client = _socket;
            _streamResource = new TcpClientAdapter(_tcpClient) { ReadTimeout = ReadTimeout, WriteTimeout = WriteTimeout };
            if (_modbusTCP)
                _master = _factory.CreateMaster(_tcpClient);
            else
                _master = _factory.CreateRtuMaster(_streamResource);
            return this;
        }

    }
    public class UdpClientMaster : RtuOverIPMaster
    {
        private UdpClient _udpClient;

        private bool _modbusTCP;
        public bool ModbusTCP { set { _modbusTCP = value; } }


        public override Task<IModbusMaster> ConnectAsync(IPAddress ip, int port, CancellationToken token)
        {
            _udpClient = new UdpClient();
            _udpClient.Connect(ip, port);

            _socket = _udpClient.Client;
            _streamResource = new UdpClientAdapter(_udpClient);
            _master = _factory.CreateRtuMaster(_streamResource);
            return Task.FromResult((IModbusMaster)this);
        }
    }
}
