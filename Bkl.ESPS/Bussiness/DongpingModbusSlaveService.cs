using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NModbus;
using NModbus.IO;
using Orleans.Runtime;
using StackExchange.Redis;
public class DongpingModbusSlaveService : BackgroundService
{
    private ILogger<DongpingModbusSlaveService> _logger;
    private IServiceProvider _serviceProvider;
    private BklConfig _config;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LongBits
    {
        public ushort H1;
        public ushort H2;
        public ushort H3;
        public ushort H4;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Int32Bits
    {
        public ushort H1;
        public ushort H2;
    }
    public DongpingModbusSlaveService(ILogger<DongpingModbusSlaveService> logger, BklConfig config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;
    }
    static Dictionary<string, int> slaveOrder = new Dictionary<string, int>
   {
        {"机舱干粉",1},
        {"机舱烟感",2},
        {"塔底气溶胶",3},
        {"七氟丙烷",4},
        {"机舱气溶胶",5},
        {"定转子气溶胶",6},
        {"塔筒干粉",7},
        {"机舱风扇",8},
   };

    static Dictionary<string, int> normalMap = new Dictionary<string, int>
   {
        {"机舱干粉",0},
        {"机舱烟感",1},
        {"塔底气溶胶",1},
        {"七氟丙烷",0},
        {"机舱气溶胶",1},
        {"定转子气溶胶",1},
        {"塔筒干粉",0},
        {"机舱风扇",0},
   };



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var dbcontext = scope.ServiceProvider.GetService<BklDbContext>();
        var factory = new ModbusFactory();
        TcpListener listener = null;

        try
        {
            listener = new TcpListener(IPAddress.Any, 8234);
            listener.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        //var listener = new TcpListener(IPAddress.Any, 8234);

        List<BklDeviceMetadata> devices = null;
        List<ModbusDevicePair> dbdevpair = null;
        List<ModbusNodeInfo> dbdevpro = null;
        while (true)
        {
            try
            {
                devices = dbcontext.BklDeviceMetadata.ToList();
                dbdevpair = dbcontext.ModbusDevicePair.ToList();
                dbdevpro = dbcontext.ModbusNodeInfo.ToList();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                await Task.Delay(1000);
                continue;
            }
        }

        var slaveNet = factory.CreateSlaveNetwork(listener);
        var slave = factory.CreateSlave(1);
        slaveNet.AddSlave(slave);
        var listenerTask = slaveNet.ListenAsync(stoppingToken);
        var ptr = Marshal.AllocHGlobal(sizeof(long));

        var last = DateTime.Now;
        while (!stoppingToken.IsCancellationRequested)
        {
            //var lg = Yitter.IdGenerator.YitIdHelper.IdGenInstance.NewLong();
            //var bts = BitConverter.GetBytes(lg);
            //Marshal.Copy(bts, 0, ptr, bts.Length);
            //LongBits lb = (LongBits)Marshal.PtrToStructure(ptr, typeof(LongBits));
            //_logger.LogInformation($"slave {i} {lg} {lg.ToString("x16")}  {lb.H1.ToString("x4")} {lb.H2.ToString("x4")} {lb.H3.ToString("x4")} {lb.H4.ToString("x4")}");
            //slave.DataStore.HoldingRegisters.WritePoints(100, new ushort[] { lb.H1, lb.H2, lb.H3, lb.H4 });
            //slave.DataStore.HoldingRegisters.WritePoints(104, new ushort[] { lb.H1, lb.H2, lb.H3, lb.H4 });
            //await Task.Delay(500);
            ushort address = 100;
            if (DateTime.Now.Subtract(last).TotalSeconds > 60)
            {
                last = DateTime.Now;
                try
                {
                    devices = dbcontext.BklDeviceMetadata.ToList();
                    dbdevpair = dbcontext.ModbusDevicePair.ToList();
                    dbdevpro = dbcontext.ModbusNodeInfo.ToList();

                }
                catch (Exception ex)
                {
                    _logger.LogError($"{_config.MySqlString} {ex}");
                    devices = null;
                    dbdevpair = null;
                    dbdevpro = null;
                }
            }
            if (devices == null)
            {

                await Task.Delay(1000);
                _logger.LogError($"devices null ");
                continue;
            }
            try
            {
                var redis = scope.ServiceProvider.GetService<IRedisClient>();
                foreach (var devs in devices.GroupBy(s => s.FacilityId).OrderBy(s => s.Key))
                {
                    foreach (var dev in devs.OrderBy(s => s.Id).ToList())
                    {
                        try
                        {
                            if (dev.DeviceType == "ThermalCamera") 
                                continue;

                            var paris = dbdevpair.Where(s => s.DeviceId == dev.Id).ToList();
                            var protos = dbdevpro.Where(s => s.ProtocolName == dev.DeviceType).ToList();
                            if (paris.Count <= 0 || protos.Count <= 0)
                            {
                                _logger.LogError("PairsNoData");
                                continue;
                            }
                            var datas = (from p in paris
                                         join q in protos on p.NodeId equals q.Id
                                         where p.DeviceId == dev.Id && (
                                         q.ReadType == ModbusReadType.ReadCoils ||
                                         q.ReadType == ModbusReadType.ReadHoldingRegister ||
                                         q.ReadType == ModbusReadType.ReadInputRegister ||
                                         q.ReadType == ModbusReadType.ReadInputs)
                                         select new
                                         {
                                             index = p.Id,
                                             protocol = p.ProtocolName,
                                             busId = p.BusId,
                                             attrId = q.Id,
                                             name = q.StatusName,
                                             nameCN = q.StatusNameCN,
                                             startAddress = q.StartAddress,
                                             map = q.ValueMap
                                         }).ToList();

                            var dics = redis.GetValuesFromHash($"DeviceStatus:{dev.Id}")
                                 .ToDictionary(s => long.Parse(s.Key),
                                 s => JsonSerializer.Deserialize<DeviceUpdateStatus>((string)s.Value));

                            foreach (var item in datas.OrderBy(s => slaveOrder.TryGetValue(s.nameCN, out var v) ? v : s.index))
                            {
                                var map = JsonSerializer.Deserialize<KeyNamePair[]>(item.map);

                                if (!dics.TryGetValue(item.index, out var status))
                                {
                                    //_logger.LogError($"DeviceNullData deviceId:{dev.Id} {item.name} {item.index}");
                                    address = WriteModbusData(slave, ptr, address, redis, dev, new DeviceUpdateStatus
                                    {
                                        Index = item.index,
                                        AttributeId = item.attrId,
                                        Type = "uint16",
                                        Value = normalMap[item.nameCN].ToString(),
                                        Name = item.name,
                                        NameCN = item.nameCN,
                                    });
                                }
                                else
                                {
                                    var itemNormal = map.FirstOrDefault(s => s.name == "正常" || s.name == "开");
                                    var nowStatus = map.FirstOrDefault(s => int.Parse(s.key) == int.Parse(status.Value));
                                    //Console.WriteLine($"{dev.FacilityName} {status.Value} {nowStatus.name} {nowStatus.name == "正常"} {nowStatus.name == "开"} {normalMap[item.nameCN]}");
                                    if (nowStatus.name == "正常" || nowStatus.name == "开")
                                    {
                                        status.Value = normalMap[item.nameCN].ToString();
                                    }
                                    else
                                    {
                                        status.Value = (normalMap[item.nameCN] == 1 ? 0 : 1).ToString();
                                    }
                                    //Console.WriteLine($"{dev.FacilityName} {item.nameCN} {status.Value}");
                                    address = WriteModbusData(slave, ptr, address, redis, dev, status);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.ToString());
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            await Task.Delay(1000);
        }
        await listenerTask;
        Marshal.FreeHGlobal(ptr);

    }

    private ushort WriteModbusData(IModbusSlave slave, IntPtr ptr, ushort address, IRedisClient redis, BklDeviceMetadata dev, DeviceUpdateStatus status)
    {
        Dictionary<string, RedisValue> dic = new Dictionary<string, RedisValue>
        {
            { "name", $"{status.Name}" },
            { "nameCN", $"{status.NameCN}" },
            { "info", $"{dev.FactoryName}/{dev.FacilityName}/{dev.FullPath}" },
            { "address", address.ToString()},
            { "did", $"{dev.Id}" },
            { "fid", $"{dev.FactoryId}" },
            { "faid", $"{dev.FacilityId}" },
            { "nodeId", $"{status.AttributeId}" },
            { "pairId", $"{status.Index}" },

        };
        switch (status.Type)
        {
            case "float":
                {
                    try
                    {
                        var bts = BitConverter.GetBytes(float.Parse(status.Value));
                        Marshal.Copy(bts, 0, ptr, bts.Length);
                        Int32Bits lb = (Int32Bits)Marshal.PtrToStructure(ptr, typeof(Int32Bits));
                        slave.DataStore.HoldingRegisters.WritePoints(address, new ushort[] { lb.H1, lb.H2 });
                        dic.Add("value", $"{lb.H1.ToString("x4")}-{lb.H2.ToString("x4")}");

                        redis.SetRangeInHash($"Modbus:{address}", dic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
                address += 2;
                break;
            case "int16":
            case "uint16":
                {
                    try
                    {
                        slave.DataStore.HoldingRegisters.WritePoints(address, new ushort[] { UInt16.Parse(status.Value) });
                        dic.Add("value", $"{UInt16.Parse(status.Value).ToString("x4")}");
                        redis.SetRangeInHash($"Modbus:{address}", dic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }

                address += 1;
                break;
            case "int32":
            case "uint32":
                {
                    try
                    {
                        var bts1 = BitConverter.GetBytes(UInt32.Parse(status.Value));
                        Marshal.Copy(bts1, 0, ptr, bts1.Length);
                        Int32Bits bits = (Int32Bits)Marshal.PtrToStructure(ptr, typeof(Int32Bits));
                        slave.DataStore.HoldingRegisters.WritePoints(address, new ushort[] { bits.H1, bits.H2 });
                        dic.Add("value", $"{bits.H1.ToString("x4")}-{bits.H2.ToString("x4")}");
                        redis.SetRangeInHash($"Modbus:{address}", dic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
                address += 2;
                break;
        }

        return address;
    }
}
