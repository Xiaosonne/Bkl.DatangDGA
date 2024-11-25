using Bkl.Models;
using System;

public class DeviceState
{
    public DeviceState()
    {

    }
    public string Name { get; set; }

    public string Type { get; set; }

    public string Value { get; set; }

    public string Unit { get; set; }

    public string NameCN { get; set; }
    /// <summary>
    /// pair id
    /// </summary>
    public long PairId { get; set; }

    public string UnitCN { get; set; }

    public DateTime CreateTime { get; set; } 

    public long DeviceId { get; set; }
    public long AttributeId { get; set; } 
    public long FacilityId { get; set; }
    public long FactoryId { get; set; }
    public long DataId { get; set; }
    public byte BusId { get; set; }
    public long ConnId { get; set; }

    public string Level { get; set; }
    public string ProtocolName { get; set; }

    public KeyNamePair[] ValueMap { get; set; }
    public string ConnUuid { get;  set; }
}
