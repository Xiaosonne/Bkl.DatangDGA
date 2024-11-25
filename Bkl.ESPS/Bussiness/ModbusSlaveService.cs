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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NModbus;
using StackExchange.Redis;

public class ModbusSlaveService : BackgroundService
{
    private ILogger<ModbusSlaveService> _logger;
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
    public ModbusSlaveService(ILogger<ModbusSlaveService> logger, BklConfig config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var builder = scope.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>();

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
        using (var dbcontext = new BklDbContext(builder.Options))
        {
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
                    using (var dbcontext = new BklDbContext(builder.Options))
                    {
                        devices = dbcontext.BklDeviceMetadata.ToList();
                        dbdevpair = dbcontext.ModbusDevicePair.ToList();
                        dbdevpro = dbcontext.ModbusNodeInfo.ToList();
                    }
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

                            var stateDic = redis.GetValuesFromHash($"DeviceStatus:{dev.Id}")
                                .ToDictionary(
                                s => long.Parse(s.Key),
                                s => JsonSerializer.Deserialize<DeviceUpdateStatus>((string)s.Value));

                            foreach (var item in datas.OrderBy(s => s.name))
                            {
                                stateDic.TryGetValue(item.index, out var status);
                                KeyNamePair[] kvpairs = null;
                                try
                                {
                                    kvpairs = JsonSerializer.Deserialize<KeyNamePair[]>(item.map);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"{ex}");
                                    kvpairs = null;
                                }
                                if (kvpairs != null)
                                {
                                    var newVal = kvpairs.FirstOrDefault(s => s.key == "NoOut");
                                    if (newVal != null)
                                    {
                                        continue;
                                    }
                                }
                                if (kvpairs != null && status != null)
                                {
                                    var keyOut = "OUT-" + status.Value;
                                    var keyStatic = "STATIC";
                                    var newVal = kvpairs.FirstOrDefault(s => s.key == keyOut);
                                    if (newVal != null)
                                        status.Value = newVal.name;
                                    newVal = kvpairs.FirstOrDefault(s => s.key == keyStatic);
                                    if (newVal != null)
                                        status.Value = newVal.name;
                                }

                                if (status == null)
                                {
                                    address = WriteModbusData(slave, ptr, address, redis, dev, new DeviceUpdateStatus
                                    {
                                        Index = item.index,
                                        AttributeId = item.attrId,
                                        Type = "uint16",
                                        Value = "255",
                                        Name = item.name,
                                        NameCN = item.nameCN,
                                    });
                                }
                                else
                                {
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
            { "did", $"{dev.Id}" },
            { "fid", $"{dev.FactoryId}" },
            { "faid", $"{dev.FacilityId}" },
            { "nodeId", $"{status.AttributeId}" },
            { "pairId", $"{status.Index}" },
            { "name", $"{status.Name}" },
            { "nameCN", $"{status.NameCN}" },
            { "info", $"{dev.FactoryName}/{dev.FacilityName}/{dev.FullPath}" },
            { "address", address.ToString()},
            { "date", DateTime.Now.ToString().ToString()},
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