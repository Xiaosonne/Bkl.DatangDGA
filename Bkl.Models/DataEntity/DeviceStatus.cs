// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{

    public class DgaOnlineStatus
    {
        public long FactoryId { get; set; }
        public long DeviceId { get; set; }
        public int LastHeartBeat { get; set; }
        public int ConnectNotify { get; set; }
        public string Status { get; set; }
        public int LoadContext { get; set; }

        public override string ToString()
        {
            return $"{FactoryId},{DeviceId},{LastHeartBeat},{ConnectNotify},{Status}";
        }
    }


    public class DeviceStatus
    {
        public DeviceStatusItem[] status { get; set; }

        public long did { get; set; }
        public long fid { get; set; }
        public long faid { get; set; }
        public long time { get; set; }
    }
}
