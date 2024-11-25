using Bkl.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IReadTask
{
    BklDeviceMetadata Device { get; set; }
    List<ModbusNodeInfo> Nodes { get; set; }
    List<ModbusDevicePair> Pairs { get; set; }
    string Uuid { get; set; }

    Task Init(DgaReadingContext context);
    Task<DeviceState[]> QueryAsync(CancellationToken token);
    void Unload();
    public DateTime LastQuery { get; }
}
