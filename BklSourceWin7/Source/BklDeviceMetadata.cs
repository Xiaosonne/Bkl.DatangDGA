using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    public partial class BklDeviceMetadata  
    { 
        
        public string GroupName { get; set; }
        
        public string ProbeName { get; set; }
        
        public string DeviceType { get; set; }
        
        public string DeviceName { get; set; }
        
        public string PDeviceType { get; set; }
        
        public string PDeviceName { get; set; }
        
        public string PathType { get; set; }
        
        public string FullPath { get; set; }
        
        public string Path1 { get; set; }
        
        public string Path2 { get; set; }
        
        public string Path3 { get; set; }
        
        public string Path4 { get; set; }
        
        public string Path5 { get; set; }
        
        public string Path6 { get; set; }
        /// <summary>
        /// ip设备分组 ip设备mac地址
        /// </summary>
        
        public string MacAddress { get; set; }
        
        public string ConnectionString { get; set; }

        
        public string ConnectionType { get; set; }
        
        public string DeviceMetadata { get; set; }
        
        public string FactoryName { get; set; }
        
        public string FacilityName { get; set; }
        
        public string AreaName { get; set; }
                public long FactoryId { get; set; }
        public long FacilityId { get; set; }
        public long CreatorId { get; set; }
        public long Id { get;  set; }
    }
}
