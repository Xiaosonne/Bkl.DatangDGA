using System.Collections.Generic;

public class ChannelData<TService, TData>
{
    public TData Data { get; set; }
    public string Topic { get; set; }
}
public class HubChannelData
{
    public string Action { get; set; }
    public string Data { get; set; }
}

public class DeviceWebMeta
{
    public long FactoryId { get; set; }
    public long FacilityId { get; set; }
    public long DeviceId { get; set; }
}


public class DeviceWebStatus<T>
{
    public DeviceWebMeta meta { get; set; }
    public IEnumerable<T> status { get; set; }
}
