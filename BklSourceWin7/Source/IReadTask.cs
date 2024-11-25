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
    DateTime LastQuery { get; }

    DeviceState[] QueryAsync(CancellationToken token);
    void Init(DgaReadingContext context);
    void Unload();
}
