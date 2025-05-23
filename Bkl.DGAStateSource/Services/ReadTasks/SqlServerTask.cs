using Bkl.Infrastructure;
using Bkl.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;


public class SqlServerTask : IReadTask
{
    public BklDeviceMetadata Device { get; set; }
    public List<ModbusDevicePair> Pairs { get; set; }
    public List<ModbusNodeInfo> Nodes { get; set; }
    public List<DeviceReadRequest> Requests { get; private set; }
    public string Uuid { get; set; }

    private ModbusConnInfo Connection;

    public SqlServerTask(IConfiguration config)
    {
        _config = config;
    }


    public async Task Init(DgaReadingContext context)
    {
        Device = context.Device;
        Uuid = context.Connection.Uuid;
        Connection = context.Connection;
        //connectionId busId protocol  device
        Pairs = context.Pairs.ToList();
        Nodes = context.Nodes.ToList();
        Requests = GetQueryRequests();
        Console.WriteLine($"InitTask DeviceId:{Device.Id} DeviceName:{Device.DeviceName}");
    }
    public class QueryResult
    {
        public double H2 { get; set; }
        public double CO { get; set; }
        public double CO2 { get; set; }
        public double CH4 { get; set; }
        public double C2H2 { get; set; }
        public double C2H4 { get; set; }
        public double C2H6 { get; set; }
        public double TotHyd { get; set; }
        public double Mst { get; set; }
        public double O2 { get; set; }
        public double N2 { get; set; }
        public double CmbuGas { get; set; }
    }
    private IConfiguration _config;

    DateTime _lastQuery = DateTime.MinValue;

    public DateTime LastQuery { get => _lastQuery; }
    public async Task<DeviceState[]> QueryAsync(CancellationToken token)
    {
        _lastQuery = DateTime.Now;
        await Task.Delay(1);
        var where = _config.GetValue<string>("DGA:SqlWhere:" + this.Device.Id);
        var cmd = "";
        if (!string.IsNullOrEmpty(where))
            cmd = _config.GetValue<string>("DGA:SqlReadCmd").Replace(" {where} ", where);
        else
            cmd = _config.GetValue<string>("DGA:SqlReadCmd").Replace(" {where} ", "1=1");
        var connStr = Connection.ConnStr;
        if (Connection.ConnStr.StartsWith("base64"))
        {
            connStr = Encoding.UTF8.GetString(Convert.FromBase64String(Connection.ConnStr.Substring(7)));
            Console.WriteLine("connStr:" + connStr);
        }
        QueryResult result = null;
        switch (Connection.ModbusType)
        {
            case "sqlserver":
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    result = conn.Query<QueryResult>(cmd).FirstOrDefault();
                }
                break;
            case "sqlite":
                using (SQLiteConnection conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();
                    result = conn.Query<QueryResult>(cmd).FirstOrDefault();
                }
                break;
            case "access":
                using (OleDbConnection conn = new OleDbConnection(connStr))
                {
                    conn.Open();
                    result = conn.Query<QueryResult>(cmd).FirstOrDefault();
                }
                break;
            default:
                break;
        }
        Console.WriteLine("ReadSqlData:" + JsonSerializer.Serialize(result));
        var tquery = typeof(QueryResult);
        List<DeviceState> lis = new List<DeviceState>();
        foreach (var request in Requests)
        {
            var node = request.Node;
            var prop = tquery.GetProperty(request.Node.StatusName);
            if (prop == null)
            {
                continue;
            }
            double data = (double)prop.GetGetMethod().Invoke(result, null);
            DeviceState statusItem = new DeviceState
            {
                ProtocolName = request.ProtocolName,

                DataId = SnowId.NextId(),

                Name = node.StatusName,
                NameCN = node.StatusNameCN,
                Type = node.DataType.ToString().Substring(3),
                Unit = node.Unit,
                UnitCN = node.UnitCN,
                Value = data.ToString(),
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

        return lis.ToArray();

    }

    public void Unload()
    {

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
                    var node = Nodes.First(s => s.Id == sameProto.NodeId);
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
