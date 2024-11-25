using Bkl.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Bkl.Infrastructure;
using NModbus;
using System.Net;
using Bkl.Models.Std;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Text.Json;
using static StateSourceService;
using Makaretu.Dns.Resolving;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Xml.Linq;
using System.Net.Sockets;
public class ModbusTask : IReadTask
{
    private ModbusConnInfo _connection;
    private List<DeviceReadRequest> _queryRequests;
    private ILogger<ModbusTask> _logger;
    private IModbusMaster _master;

    public BklDeviceMetadata Device { get; set; }
    public List<ModbusDevicePair> Pairs { get; set; }
    public List<ModbusNodeInfo> Nodes { get; set; }
    public string Uuid { get; set; }

    public ModbusTask(ILogger<ModbusTask> logger)
    {
        _logger = logger;
    }
    public async Task Init(DgaReadingContext context)
    {
        Device = context.Device;
        Uuid = context.Connection.Uuid;
        _connection = context.Connection;
        //connectionId busId protocol  device
        Pairs = context.Pairs.ToList();
        var protocols = Pairs.Select(s => s.ProtocolName).Distinct();
        Nodes = context.Nodes.ToList();
        _queryRequests = GetQueryRequests();
    }
    DateTime _lastQuery = DateTime.MinValue;

    public DateTime LastQuery { get => _lastQuery; }
    public async Task<DeviceState[]> QueryAsync(CancellationToken token)
    {
        _lastQuery = DateTime.Now;

        if (_master == null)
        {
            try
            {
                _master = await GetMasterAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ReadQueue {_connection.ConnStr} {_connection.Uuid} {_lastQuery} Connection Error {ex.ToString()}");
                _master = null;
            }
        }
        if (_master == null)
        {
            _logger.LogError($"ReadQueue {_connection.ConnStr} {_connection.Uuid} {_lastQuery} ConnectionNull");
            return null;
        }
        List<DeviceState> lis = new List<DeviceState>();
        foreach (var sameBus in _queryRequests.GroupBy(s => s.BusId))
        {
            foreach (var sameReadType in sameBus.GroupBy(s => s.Node.ReadType))
            {
                await Task.Delay(100);
                DeviceState[] status = null;
                try
                {
                    status = await AggregateRead(_master, sameReadType.ToArray());
                    if (status != null && status.Length > 0)
                        lis.AddRange(status);
                }
                catch (Exception ex)
                {
                    _master = null;
                    _logger.LogError(ex.ToString());
                }
            }
        }
        return lis.ToArray();
    }
    private async Task<DeviceState[]> AggregateRead(IModbusMaster master, DeviceReadRequest[] readRequests)
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
        return results.Select(s => GetDeviceUpdateStatusFromBytes(s.Item1, s.Item2)).ToArray();
    }
    private DeviceState GetDeviceUpdateStatusFromBytes(DeviceReadRequest request, HexString data)
    {
        var node = request.Node;
        DeviceState statusItem = new DeviceState
        {
            ProtocolName = request.ProtocolName,

            DataId = SnowId.NextId(),

            Name = node.StatusName,
            NameCN = node.StatusNameCN,
            Type = node.DataType.ToString().Substring(3),
            Unit = node.Unit,
            UnitCN = node.UnitCN,
            Value = "",

            ValueMap = TryCatchExtention.TryCatch((string str) => JsonSerializer.Deserialize<KeyNamePair[]>(str), node.ValueMap),

            FacilityId = Device.FacilityId,
            FactoryId = Device.FactoryId,
            DeviceId = request.DeviceId,


            AttributeId = request.AttributeId,
            PairId = request.PairId,
            ConnId = _connection.Id,
            ConnUuid = Uuid,
            BusId = request.BusId,

            CreateTime = DateTime.Now,
        };
        var scale = (node.Scale.Empty() || node.Scale == "1") ? 1.0f : float.Parse(node.Scale);
        switch ((ModbusDataType)node.DataType)
        {
            case ModbusDataType.dt_float:
                statusItem.Value = (data.GetFloat(node.DataOrder) * (scale)).ToString();
                break;
            case ModbusDataType.dt_int16:
                statusItem.Value = (((Int16)(data.GetInt16(node.DataOrder))) * scale).ToString();
                break;
            case ModbusDataType.dt_uint16:
                statusItem.Value = (((UInt16)(data.GetUInt16(node.DataOrder))) * scale).ToString();
                break;
            case ModbusDataType.dt_int32:
                statusItem.Value = (((Int32)(data.GetInt32(node.DataOrder))) * scale).ToString();
                break;
            case ModbusDataType.dt_uint32:
                statusItem.Value = (((UInt32)(data.GetUInt32(node.DataOrder))) * scale).ToString();
                break;
            default:
                break;
        }
        return statusItem;
    }
    private List<DeviceReadRequest> GetQueryRequests()
    {
        List<DeviceReadRequest> requests = new List<DeviceReadRequest>();
        foreach (var sameBusId in Pairs.GroupBy(s => s.BusId))
        {
            foreach (var sameProtos in sameBusId.GroupBy(s => s.ProtocolName))
            {
                foreach (var sameProto in sameProtos)
                {
                    var node = Nodes.FirstOrDefault(s => s.Id == sameProto.NodeId);
                    if (node == null)
                        continue;
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


    private async Task<IModbusMaster> GetMasterAsync(CancellationToken token)
    {

        IModbusMaster master = null;
        try
        {
            if (master == null)
            {
                _logger.LogInformation($"{_connection.ConnStr} now:{DateTime.Now}  connecting");
                master = await ConnectAsync(token);
            }
            if (master == null)
            {
                _logger.LogError($"{_connection.ConnStr} now:{DateTime.Now}  master connection timeout ");
                return null;
            }
        }
        catch (Exception ex)
        {
            master = null;
            _logger.LogError($"{_connection.ConnStr} now:{DateTime.Now}  master connection {ex} ");
            return null; ;
        }
        return master;
    }
    private async Task<IModbusMaster> ConnectAsync(CancellationToken token)
    {
        DateTime now = DateTime.Now;

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
                    }.ConnectAsync(IPAddress.Parse(ip), port, token);
                }
                if (_connection.ConnType == "udp")
                {
                    master = await new UdpClientMaster().ConnectAsync(IPAddress.Parse(ip), port, token);
                }
            }
            else
            {
                master = await new TcpClientMaster
                {
                    ModbusTCP = true,
                    ReadTimeout = 3000,
                    WriteTimeout = 3000,
                }.ConnectAsync(IPAddress.Parse(ip), port, token);
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
    private void CloseConnection(IModbusMaster master)
    {
        if (master != null)
        {
            try { master.Dispose(); } catch { }
            master = null;
        }
    }
    public void Unload()
    {
        CloseConnection(_master);

    }
}
