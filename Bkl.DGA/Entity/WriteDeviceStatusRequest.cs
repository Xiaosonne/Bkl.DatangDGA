using System;

public class WriteDeviceStatusRequest
{
    public WriteDeviceStatusRequest()
    {
        Createtime = DateTime.Now;
    }
    public string ConnUuid { get; set; }
    public byte[] Data { get; set; }
    public long DeviceId { get; set; }
    public long AttributeId { get; set; }
    public byte BusId { get; set; }
    public string ProtocolName { get; set; }
    public long PairId { get; set; }
    public string SourceId { get; set; }
    public DateTime Createtime { get; set; }
}
