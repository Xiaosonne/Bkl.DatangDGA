// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    public class DeviceStatus
    {
        public DeviceStatusItem[] status { get; set; }

        public long did { get; set; }
        public long fid { get; set; }
        public long faid { get; set; }
        public long time { get; set; }
    }
}
