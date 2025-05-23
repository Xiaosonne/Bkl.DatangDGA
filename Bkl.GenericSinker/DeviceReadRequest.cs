using Bkl.Models;

public class DeviceReadRequest
{
    public long DeviceId { get; set; }
    public ushort StartAddress { get; set; }
    public ushort NumberOfPoints { get; set; }
    public long AttributeId { get; set; }
    public long PairId { get; set; }
    public byte BusId { get; set; }
    public ModbusNodeInfo Node { get; set; }
    public string ProtocolName { get; set; }

}
