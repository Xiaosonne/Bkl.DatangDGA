// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

using System;

namespace Bkl.Models
{


    public class DeviceUpdateStatus
    {
        public DeviceUpdateStatus()
        {

        }

        public long DeviceId { get; set; }
        public long FacilityId { get; set; }
        public long FactoryId { get; set; }


        public long Index { get; set; }
        public long AttributeId { get; set; }
        public string ConnUuid { get; set; }
        public string ProtocolName { get; set; }


        public string Name { get; set; }
        public string NameCN { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string UnitCN { get; set; }
        public DateTime CreateTime { get; set; }
        public string Level { get; set; }
        public long DataId { get; set; }
        public string GroupName { get; set; }
        public DateTime PushTime { get; set; }
    }

    public class DeviceDgaUpdateStatus
    {
        public DeviceDgaUpdateStatus()
        {

        }

        public long DeviceId { get; set; }
        public long FacilityId { get; set; }
        public long FactoryId { get; set; }


        public long Index { get; set; }
        public long AttributeId { get; set; }
        public string ConnUuid { get; set; }
        public string ProtocolName { get; set; }


        public string Name { get; set; }
        public string NameCN { get; set; }
        //public string Type { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string UnitCN { get; set; }
        //public DateTime CreateTime { get; set; }
        //public string Level { get; set; }
        //public long DataId { get; set; }
        //public string GroupName { get; set; }
        //public DateTime PushTime { get; set; }
    }
}
