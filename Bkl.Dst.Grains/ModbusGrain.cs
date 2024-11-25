using Bkl.Dst.Interfaces;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bkl.Infrastructure;
using Microsoft.Extensions.Logging;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Bkl.Models.Std;
using System.Threading;
using NModbus;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Microsoft.EntityFrameworkCore.Internal;
using Makaretu.Dns.Resolving;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Reflection.Metadata.Ecma335;

namespace Bkl.Dst.Grains
{
    public class ModbusGrain : Grain, IModbusGrain, IRemindable
    {

        private readonly ILogger<ModbusGrain> _logger;
        private ModbusConnInfo _connection;
        private List<ModbusDevicePair> _devicePairs;

        private IDisposable _timerHandle = null;
        private List<ModbusNodeInfo> _nodes;

        private IModbusMaster _master;
        private bool _initialized;

        private Task _preQueryTask;
        private uint _currentIndex = 0;

        private Dictionary<long, DeviceUpdateStatus> _hotStatus = new Dictionary<long, DeviceUpdateStatus>();
        private Queue<WriteDeviceStatusRequest> _writeQueue = new Queue<WriteDeviceStatusRequest>();
        private List<DeviceReadRequest> _queryRequests;

        DateTime _lastTimer = DateTime.MinValue;
        DateTime _lastConnect = DateTime.MinValue;
        DateTime _lastQuery = DateTime.MinValue;


        public ModbusGrain(ILogger<ModbusGrain> logger) : base()
        {
            _logger = logger;
        }


        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }


        public Task Weakup()
        {
            _logger.LogInformation($"Modbus Connection {_connection.ConnStr} {_connection.ConnStr} Weakup");
            return Task.CompletedTask;
        }

        public override async Task OnActivateAsync()
        {
            await LoadConnection();
            if (_connection == null)
            {
                this.DeactivateOnIdle();
                _logger.LogError($"Modbus ID Not Found {this.GetPrimaryKeyString()} OnActivateAsync");
                return;
            }
            //_logger.LogInformation($"Modbus Connection {_connection.ConnStr} {_connection.ConnStr} OnActivateAsync");
            await this.RegisterOrUpdateReminder("modbusWeakUp", TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
        }
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            _logger.LogInformation($"{_connection.ConnStr} {_connection.Uuid} reminder {DateTime.Now}");

            if (_timerHandle != null)
            {
                try { _timerHandle.Dispose(); } catch { }
                _timerHandle = null;
            }
            _timerHandle = this.RegisterTimer(TimerReadWriteWork, _queryRequests, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100));
            return Task.CompletedTask;

        }


        private async Task TimerReadWriteWork(object state)
        {
            if (DateTime.Now.Subtract(_lastConnect).TotalSeconds > 30)
            {
                _lastConnect = DateTime.Now;
                if (_master != null)
                    CloseConnection(_master);
                (_initialized, _master) = await InititializeConnection();
                if (!_initialized || _master == null)
                    _logger.LogError($"{_connection.ConnStr} {_connection.Uuid} {_lastConnect} ConnectionError NoTimer");
            }

            if (!_initialized || _master == null)
            {
                return;
            }

            await ProcessWriteQueue(state);

            await ProcessReadQueue(state);

        }
        private async Task ProcessWriteQueue(object state)
        {
            _lastTimer = DateTime.Now;
            if (_writeQueue.Count > 0)
            {
                if (_master == null)
                {
                    _logger.LogError($"WriteQueue {_connection.ConnStr} {_connection.Uuid} {_lastTimer} ConnectionNull");
                    return;
                }
                var write = _writeQueue.Peek();
                try
                {
                    await InternalWriteStatus(_master, write);
                    _writeQueue.Dequeue();
                    _lastQuery = DateTime.MinValue;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"WriteStatusError {_connection.ConnStr}  {_lastTimer}  {ex} ");
                }
            }
        }
        List<DeviceReadRequest[]> _readRequestList = new List<DeviceReadRequest[]>();
        private int _lastQueryIndex = 0;
        private async Task ProcessReadQueue(object state)
        {
            var sleep = (new Random()).Next(3000, 5000);
            List<DeviceReadRequest> requests = state as List<DeviceReadRequest>;
            //_currentIndex++;
            if (DateTime.Now.Subtract(_lastQuery).TotalMilliseconds < sleep)
                return;
            _lastQuery = DateTime.Now;
            List<DeviceUpdateStatus> statusLis = new List<DeviceUpdateStatus>();

            if (_readRequestList == null)
            {
                _readRequestList = new List<DeviceReadRequest[]>();
                foreach (var sameBus in requests.GroupBy(s => s.BusId))
                {
                    foreach (var sameReadType in sameBus.GroupBy(s => s.Node.ReadType))
                    {
                        if (_master == null)
                        {
                            _logger.LogError($"ReadQueue {_connection.ConnStr} {_connection.Uuid} {_lastQuery} ConnectionNull");
                            return;
                        }
                        _readRequestList.Add(sameReadType.ToArray());
                    }
                }
            }
            var reqs = _readRequestList[_lastQueryIndex % _readRequestList.Count];
            int tryCount = 0;
            bool exception = true;
            while (exception)
            {
                try
                {
                    //var request = requests[(int)(_currentIndex % requests.Count)];
                    statusLis.AddRange(await InternalAggregateReadStatus(_master, reqs));
                    exception = false;
                }
                catch (Exception ex)
                {
                    exception = true;
                    _logger.LogError($"ReadStatusError {_connection.ConnStr} {_lastQuery} {ex}");
                    try
                    {
                        (_initialized, _master) = await InititializeConnection();
                    }
                    catch (Exception ex1)
                    {
                        _logger.LogError($"InitError {_connection.ConnStr} {_lastQuery} {ex}");
                    }
                }

                await Task.Delay(5);
                tryCount++;

                if (tryCount > 10)
                {
                    break;
                }
            }

            _lastQueryIndex++;
            if (_lastQueryIndex < 0)
                _lastQueryIndex = 0;

            foreach (var statusSame in statusLis.GroupBy(s => s.DeviceId))
            {
                IDeviceGrain device = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(new BklDeviceMetadataRef { Id = statusSame.Key }));
                foreach (var status in statusSame)
                {
                    if (!_hotStatus.TryAdd(status.Index, status))
                    {
                        _hotStatus[status.Index] = status;
                    }
                }
                await device.UpdateManyStatus(statusSame.ToArray());
            }
        }
        private List<DeviceReadRequest> GetQueryRequests()
        {
            List<DeviceReadRequest> requests = new List<DeviceReadRequest>();
            foreach (var sameBusId in _devicePairs.GroupBy(s => s.BusId))
            {
                foreach (var sameProtos in sameBusId.GroupBy(s => s.ProtocolName))
                {
                    foreach (var sameProto in sameProtos)
                    {
                        var node = _nodes.First(s => s.Id == sameProto.NodeId);
                        if ((int)node.ReadType > 4)
                        {
                            continue;
                        }
                        requests.Add(new DeviceReadRequest
                        {
                            BusId = sameBusId.Key,
                            ProtocolName = sameProtos.Key,
                            Node = node,
                            PairId = sameProto.Id,
                            DeviceId = sameProto.DeviceId,
                            StartAddress = Convert.ToUInt16(node.StartAddress + sameProto.NodeIndex),
                            NumberOfPoints = node.DataSize,
                            AttributeId = sameProto.NodeId
                        });
                    }
                }
            }
            return requests;
        }

        private async Task<(bool, IModbusMaster)> InititializeConnection()
        {
            _logger.LogInformation($"{_connection.ConnStr} now:{DateTime.Now}  connecting");
            IModbusMaster master = null;
            try
            {
                if (master == null)
                {
                    master = await ConnectAsyncWithTimeout(1000);
                }
                if (master == null)
                {
                    _logger.LogError($"{_connection.ConnStr} now:{DateTime.Now}  master connection timeout ");
                    return (false, null);
                }
            }
            catch (Exception ex)
            {
                master = null;
                _logger.LogError($"{_connection.ConnStr} now:{DateTime.Now}  master connection {ex} ");
                return (false, null); ;
            }
            return (true, master);
        }
        private async Task<IModbusMaster> ConnectAsyncWithTimeout(int tsMs)
        {
            DateTime now = DateTime.Now;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(tsMs);
            IModbusMaster master = new TcpClientMaster();
            var ip = _connection.ConnStr.Split(':')[0];
            var port = int.Parse(_connection.ConnStr.Split(':')[1]);
            try
            {
                if (_connection.ModbusType.Contains("modbusrtu"))
                {
                    if (_connection.ConnType == "tcp")
                    {
                        master = await new TcpClientMaster
                        {
                            ReadTimeout = 3000,
                            WriteTimeout = 3000,
                        }.ConnectAsync(IPAddress.Parse(ip), port, cts.Token);
                    }
                    if (_connection.ConnType == "udp")
                    {
                        master = await new UdpClientMaster().ConnectAsync(IPAddress.Parse(ip), port, cts.Token);
                    }
                }
                else
                {
                    master = await new TcpClientMaster
                    {
                        ModbusTCP = true,
                        ReadTimeout = 3000,
                        WriteTimeout = 3000,
                    }.ConnectAsync(IPAddress.Parse(ip), port, cts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ConnectAsyncWithTimeout {_connection.ConnStr} time:{DateTime.Now.Subtract(now).TotalMilliseconds} ERROR {ex.Message}  ");
                CloseConnection(master);
                master = null;
            }
            return master;

        }
        private async Task LoadConnection()
        {
            var key = this.GetPrimaryKeyString();
            var builder = this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>();
            using (BklDbContext context = new BklDbContext(builder.Options))
            {
                _connection = await context.ModbusConnInfo.FirstOrDefaultAsync(s => s.Uuid == key);
                //connectionId busId protocol  device
                _devicePairs = await context.ModbusDevicePair.Where(s => s.ConnectionId == _connection.Id).AsNoTracking().ToListAsync();
                var protocols = _devicePairs.Select(s => s.ProtocolName).Distinct();
                _nodes = await context.ModbusNodeInfo.Where(s => protocols.Contains(s.ProtocolName)).AsNoTracking().ToListAsync();
            }
            _queryRequests = GetQueryRequests();
        }
        private void CloseConnection(IModbusMaster master)
        {
            if (master != null)
            {
                try { master.Dispose(); } catch { }
                master = null;
            }
        }


        public Task WriteStatus(WriteDeviceStatusRequest writeRequest)
        {
            _logger.LogInformation($"WriteStatusQueued  {_connection.ConnStr} {writeRequest.AttributeId} {DateTime.Now}");
            _writeQueue.Enqueue(writeRequest);
            return Task.CompletedTask;
        }

        private async Task InternalWriteStatus(IModbusMaster _master, WriteDeviceStatusRequest writeRequest)
        {

            var node = _nodes.First(s => s.Id == writeRequest.AttributeId);
            if (writeRequest.BusId <= 0)
            {
                var pair = _devicePairs.FirstOrDefault(s => s.Id == writeRequest.PairId);
                if (pair == null)
                {
                    _logger.LogError($"WriteStatusError {writeRequest.ProtocolName} {writeRequest.DeviceId} control busid zero ");
                    return;
                }
                writeRequest.BusId = pair.BusId;
            }
            _logger.LogInformation($"WriteStatus {node.ReadType} {_connection.ConnStr}  busid:{writeRequest.BusId} start:{node.StartAddress} " +
                $"data:{string.Join("", writeRequest.Data.Select(s => s.ToString("x2")).ToArray())} {BitConverter.ToBoolean(writeRequest.Data, 0)} {DateTime.Now}");
            switch (node.ReadType)
            {

                case ModbusReadType.WriteSingleCoil:
                    await _master.WriteSingleCoilAsync(writeRequest.BusId, Convert.ToUInt16(node.StartAddress), BitConverter.ToBoolean(writeRequest.Data, 0));
                    break;
                case ModbusReadType.WriteSingleInput:
                    await _master.WriteSingleRegisterAsync(writeRequest.BusId, Convert.ToUInt16(node.StartAddress), BitConverter.ToUInt16(writeRequest.Data, 0));
                    break;
                case ModbusReadType.WriteCoils:
                    var data = writeRequest.Data.Select(s => s == 0 ? false : true).ToArray();
                    await _master.WriteMultipleCoilsAsync(writeRequest.BusId, Convert.ToUInt16(node.StartAddress), data);
                    break;
                case ModbusReadType.WriteInputs:
                    var data1 = Enumerable.Range(0, writeRequest.Data.Length / 2)
                          .Select(s => BitConverter.ToUInt16(writeRequest.Data, s * 2))
                          .ToArray();
                    await _master.WriteMultipleRegistersAsync(writeRequest.BusId, Convert.ToUInt16(node.StartAddress), data1);
                    break;
                default:/**/
                    break;
            }
        }

        public async Task<List<DeviceUpdateStatus>> ReadStatus()
        {
            List<DeviceUpdateStatus> lis = new List<DeviceUpdateStatus>();
            (var bbb, var master) = await InititializeConnection();
            if (bbb == false || master == null)
            {
                _logger.LogError($"ConnectionError {_connection.ConnStr} {_connection.Uuid} init:{bbb} master==null:{master == null}");
                return lis;
            }
            if (_writeQueue.Count > 0)
            {
                var writeRequest = _writeQueue.Dequeue();
                await InternalWriteStatus(master, writeRequest);
                _logger.LogInformation($"WriteStatus {writeRequest.BusId} {writeRequest.AttributeId} ");
            }

            _logger.LogInformation($"BeginRead {_connection.ConnStr}  {_connection.Uuid} {DateTime.Now}");
            List<(long, IDeviceGrain)> devs = new List<(long, IDeviceGrain)>();
            foreach (var request in _queryRequests)
            {
                //var request = requests[(int)(_currentIndex % requests.Count)];
                try
                {
                    var status = await InternalReadStatus(master, request);
                    if (status == null)
                        continue;
                    IDeviceGrain device = devs.FirstOrDefault(s => s.Item1 == request.DeviceId).Item2;
                    if (device == null)
                    {
                        device = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(new BklDeviceMetadataRef { Id = request.DeviceId }));
                        devs.Add((request.DeviceId, device));
                    }

                    lis.Add(status);
                    await device.UpdateStatus(status);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ReadStatus {_connection.ConnStr} {ex.ToString()}");
                }
            }
            CloseConnection(master);
            _logger.LogInformation($"EndRead {_connection.ConnStr}  {_connection.Uuid} {DateTime.Now}");
            return lis;
        }

        private async Task<DeviceUpdateStatus[]> InternalAggregateReadStatus(IModbusMaster master, DeviceReadRequest[] readRequests)
        {
            readRequests = readRequests.OrderBy(s => s.StartAddress).ToArray();
            var first = readRequests.First();
            var last = readRequests.Last();
            ushort count = Convert.ToUInt16(last.StartAddress - first.StartAddress + last.NumberOfPoints);
            var busId = first.BusId;
            List<(DeviceReadRequest, HexString)> results = new List<(DeviceReadRequest, HexString)>();
            switch (first.Node.ReadType)
            {
                case ModbusReadType.ReadCoils:
                    {
                        var bools = await master.ReadCoilsAsync(busId, first.StartAddress, count);
                        var nodeIndex = 0;
                        foreach (var read in readRequests)
                        {
                            var bytesHex = new HexString(new ushort[] { Convert.ToUInt16(bools[nodeIndex]) }).ToString();
                            results.Add((read, bytesHex));
                        }
                    }
                    break;
                case ModbusReadType.ReadInputs:
                    {
                        var bools = await master.ReadInputsAsync(busId, first.StartAddress, count);
                        var nodeIndex = 0;
                        foreach (var read in readRequests)
                        {
                            var bytesHex = new HexString(new ushort[] { Convert.ToUInt16(bools[nodeIndex]) }).ToString();
                            results.Add((read, bytesHex));
                            nodeIndex++;
                        }
                    }
                    break;
                case ModbusReadType.ReadHoldingRegister:
                    {
                        var shorts = await master.ReadHoldingRegistersAsync(busId, first.StartAddress, count);
                        foreach (var read in readRequests)
                        {
                            var startIndex = read.StartAddress - first.StartAddress;
                            var bytesHex = new HexString(shorts.Skip(startIndex).Take(read.NumberOfPoints).ToArray());
                            results.Add((read, bytesHex));
                        }
                    }
                    break;
                case ModbusReadType.ReadInputRegister:
                    {
                        var shorts = await master.ReadInputRegistersAsync(busId, first.StartAddress, count);
                        foreach (var read in readRequests)
                        {
                            var startIndex = read.StartAddress - first.StartAddress;
                            var bytesHex = new HexString(shorts.Skip(startIndex).Take(read.NumberOfPoints).ToArray());
                            results.Add((read, bytesHex));
                        }
                    }
                    break;
                default:
                    return null;
            }
            _logger.LogInformation($"conn:{_connection.ConnStr} \r\n" + string.Join("", results.Select(s => $"devId:{s.Item1.DeviceId} busId:{s.Item1.BusId} name:{s.Item1.ProtocolName} aid:{s.Item1.AttributeId} sname:{s.Item1.Node.StatusName} value:{s.Item2}\r\n")));

            return results.Select(s => GetDeviceUpdateStatusFromBytes(s.Item1, s.Item2)).ToArray();
        }
        private async Task<DeviceUpdateStatus> InternalReadStatus(IModbusMaster master, DeviceReadRequest request)
        {
            byte busId = request.BusId;
            ModbusNodeInfo node = request.Node;
            ModbusReadType type = (ModbusReadType)node.ReadType;
            DateTime now = DateTime.Now;
            HexString bytesHex = "";

            switch (type)
            {
                case ModbusReadType.ReadCoils:
                    {
                        var bools = await master.ReadCoilsAsync(busId, request.StartAddress, request.NumberOfPoints);
                        bytesHex = new HexString(bools.Select(s => Convert.ToUInt16(s)).ToArray()).ToString();
                    }

                    break;
                case ModbusReadType.ReadInputs:

                    {
                        var bools = await master.ReadInputsAsync(busId, request.StartAddress, request.NumberOfPoints);
                        bytesHex = new HexString(bools.Select(s => Convert.ToUInt16(s)).ToArray()).ToString();
                    }
                    break;
                case ModbusReadType.ReadHoldingRegister:
                    {
                        var shorts = await master.ReadHoldingRegistersAsync(busId, request.StartAddress, request.NumberOfPoints);
                        bytesHex = new HexString(shorts);
                    }
                    break;
                case ModbusReadType.ReadInputRegister:

                    {
                        var shorts = await master.ReadInputRegistersAsync(busId, request.StartAddress, request.NumberOfPoints);
                        bytesHex = new HexString(shorts);
                    }
                    break;
                default:
                    return null;
            }

            if (bytesHex == null)
            {
                _logger.LogError($"ReadTaskNull {node.ProtocolName} {node.StatusName} {node.ReadType} ");
                return null;
            }
            //_logger.LogInformation($"ReadResult {_connection.ConnStr} {readBundle.DeviceId} busId:{busId} start:{(ushort)(readBundle.StartAddressOffset + node.StartAddress)} size:{readBundle.Count} nid:{node.Id} name:{node.ProtocolName} state:{node.StatusName} value:{bytesHex} time:{DateTime.Now.Subtract(now).TotalMilliseconds}ms ");
            return GetDeviceUpdateStatusFromBytes(request, bytesHex);
        }
        private DeviceUpdateStatus GetDeviceUpdateStatusFromBytes(DeviceReadRequest request, HexString data)
        {
            var node = request.Node;
            DeviceUpdateStatus statusItem = new DeviceUpdateStatus
            {

                DataId = SnowId.NextId(),
                DeviceId = request.DeviceId,  
                Name = node.StatusName,
                NameCN = node.StatusNameCN,
                Type = node.DataType.ToString().Substring(3),
                Unit = node.Unit,
                UnitCN = node.UnitCN,
                Value = "",
                AttributeId = request.AttributeId,
                Index = request.PairId,
                ConnUuid = this.GetPrimaryKeyString(),
            };
            switch ((ModbusDataType)node.DataType)
            {
                case ModbusDataType.dt_float:
                    statusItem.Value = (data.GetFloat(node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1.0f : float.Parse(node.Scale))).ToString();
                    break;
                case ModbusDataType.dt_int16:
                    statusItem.Value = ((Int16)(data.GetInt16(node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                case ModbusDataType.dt_uint16:
                    statusItem.Value = ((UInt16)(data.GetUInt16(node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                case ModbusDataType.dt_int32:
                    statusItem.Value = ((Int32)(data.GetInt32(node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                case ModbusDataType.dt_uint32:
                    statusItem.Value = ((UInt32)(data.GetUInt32(node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                default:
                    break;
            }
            return statusItem;
        }
    }
}
