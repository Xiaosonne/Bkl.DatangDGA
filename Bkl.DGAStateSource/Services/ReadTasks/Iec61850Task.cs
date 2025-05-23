using Bkl.Models;
using Bkl.Infrastructure;
using System.Text.Json;
using IEC61850.Client;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

public class Iec61850Task : IReadTask
{
    public Iec61850Task(IConfiguration config)
    {

        _config = config;
    }
    private ModbusConnInfo Connection;
    public BklDeviceMetadata Device { get; set; }
    public List<ModbusNodeInfo> Nodes { get; set; }
    public List<ModbusDevicePair> Pairs { get; set; }
    public string Uuid { get; set; }
    IedConnection _iecConnection;
    private string _ip;
    private int _port;
    private List<DeviceReadRequest> _queryRequests;
    private IConfiguration _config;

    public Task Init(DgaReadingContext context)
    {
        Device = context.Device;
        Connection = context.Connection;
        Nodes = context.Nodes.ToList();
        Pairs = context.Pairs.ToList();
        var arr = Connection.ConnStr.Split(":");
        _ip = arr[0];
        _port = int.Parse(arr[1]);
        _queryRequests = GetQueryRequests();
        return Task.CompletedTask;
    }
    DateTime _lastQuery = DateTime.MinValue;

    public DateTime LastQuery { get => _lastQuery; }
    public async Task<DeviceState[]> QueryAsync(CancellationToken token)
    {
        _lastQuery = DateTime.Now;

        Dictionary<string, string> dic = new Dictionary<string, string>();
        _config.GetSection("Iec61850:GasMap").Bind(dic);
        if (_iecConnection == null)
        {
            _iecConnection = new IedConnection();
            _iecConnection.Connect(_ip, _port);
        }
        List<DeviceState> lis = new List<DeviceState>();
        foreach (var kv in dic)
        {
            await Task.Delay(50);
            try
            {
                var request = _queryRequests.FirstOrDefault(s => s.Node.StatusName == kv.Key);
                var node = request.Node;
                var val = _iecConnection.ReadFloatValue(kv.Value, IEC61850.Common.FunctionalConstraint.MX);
                DeviceState statusItem = new DeviceState
                {
                    ProtocolName = request.ProtocolName,

                    DataId = SnowId.NextId(),

                    Name = node.StatusName,
                    NameCN = node.StatusNameCN,
                    Type = node.DataType.ToString().Substring(3),
                    Unit = node.Unit,
                    UnitCN = node.UnitCN,
                    Value = val.ToString(),

                    ValueMap = TryCatchExtention.TryCatch((string str) => JsonSerializer.Deserialize<KeyNamePair[]>(str), node.ValueMap),
                    FacilityId = Device.FacilityId,
                    FactoryId = Device.FactoryId,
                    DeviceId = Device.Id,
                    AttributeId = request.AttributeId,
                    PairId = request.PairId,
                    ConnId = Connection.Id,
                    ConnUuid = Connection.Uuid,
                    BusId = request.BusId,

                    CreateTime = DateTime.Now,
                };
                lis.Add(statusItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _iecConnection = null;
                Unload();
                return null;
            }
        }
        return lis.ToArray();
    }

    public void Unload()
    {
        try
        {
            _iecConnection.Dispose();
        }
        catch
        {

        }
        _iecConnection = null;
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

}
