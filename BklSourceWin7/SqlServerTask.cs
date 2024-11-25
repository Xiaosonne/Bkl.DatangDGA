using Bkl.Infrastructure;
using Bkl.Models;
using BklSourceWin7;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

public class SqlServerTask : IReadTask
{
    public BklDeviceMetadata Device { get; set; }
    public List<ModbusDevicePair> Pairs { get; set; }
    public List<ModbusNodeInfo> Nodes { get; set; }
    public List<DeviceReadRequest> Requests { get; set; }

    private NameValueCollection _config;

    public string Uuid { get; set; }

    private ModbusConnInfo Connection;

    public SqlServerTask(NameValueCollection config)
    {
        _config = config;
    }


    public void Init(DgaReadingContext context)
    {
        Device = context.Device;
        Uuid = context.Connection.Uuid;
        Connection = context.Connection;
        //connectionId busId protocol  device
        Pairs = context.Pairs.ToList();
        Nodes = context.Nodes.ToList();
        Requests = GetQueryRequests();
    }


    DateTime _lastQuery = DateTime.MinValue;

    public DateTime LastQuery { get => _lastQuery; }

    private Dictionary<string, string> GetData()
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        try
        {


            var databasetype = _config["DGA:DataType"].ToString();
            var cmdstr = _config["DGA:SqlReadCmd"].ToString();
            var mqserver = _config["AppSetting:StateSinkServer"].ToString();
            var connStr = Connection.ConnStr;
            if (Connection.ConnStr.StartsWith("base64"))
            { 
                connStr = Encoding.UTF8.GetString(Convert.FromBase64String(Connection.ConnStr.Substring(7)));
                Console.WriteLine("connStr:" + connStr);
            }
            Enum.TryParse<FreeSql.DataType>(databasetype, out var datatype);
            var fsql = new FreeSql.FreeSqlBuilder()
                          //.UseConnectionString(FreeSql.DataType.Sqlite, "Data Source=document.db; Pooling=true;Min Pool Size=1")
                          .UseConnectionString(datatype, connStr)  //如果提示Microsoft.ACE.OLEDB.12.0未注册下载安装 https://download.microsoft.com/download/E/4/2/E4220252-5FAE-4F0A-B1B9-0B48B5FBCCF9/AccessDatabaseEngine_X64.exe
                          .UseAutoSyncStructure(false) //自动同步实体结构【开发环境必备】
                          .UseMonitorCommand(cmd => Console.Write(cmd.CommandText))
                          .Build();


            using (var datatable = fsql.Ado.CommandFluent(cmdstr).ExecuteDataTable())
            {
                if (datatable.Rows.Count > 0)
                {
                    DataColumn[] cols = new DataColumn[datatable.Columns.Count];
                    datatable.Columns.CopyTo(cols, 0);
                    var colsName = cols.Select(s => s.ColumnName).ToList();
                    var reader = datatable.CreateDataReader();
                    reader.Read();
                    foreach (var col in cols)
                    {
                        result.Add(col.ColumnName, reader.GetValue(col.Ordinal).ToString());
                    }
                }
            }
        }

        catch (Exception ex)
        {
            return result;
        }
        return result;
    }

    public DeviceState[] QueryAsync(CancellationToken token)
    {
        _lastQuery = DateTime.Now;
        var cmd = _config.Get("DGA:SqlReadCmd");

        var result = GetData();

        Console.WriteLine("ReadSqlData:" + JsonConvert.SerializeObject(result));
        var tquery = typeof(QueryResult);
        List<DeviceState> lis = new List<DeviceState>();
        foreach (var request in Requests)
        {
            var node = request.Node;
            if (false == result.ContainsKey(node.StatusName))
                continue;
            DeviceState statusItem = new DeviceState
            {
                ProtocolName = request.ProtocolName,
                DataId = 0,
                Name = node.StatusName,
                NameCN = node.StatusNameCN,
                Type = node.DataType.ToString().Substring(3),
                Unit = node.Unit,
                UnitCN = node.UnitCN,
                Value = result[node.StatusName],
                //ValueMap = "[]",
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
