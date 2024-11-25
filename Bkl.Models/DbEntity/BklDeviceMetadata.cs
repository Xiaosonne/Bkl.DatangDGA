using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_device_metadata")]
    public partial class BklDeviceMetadata : BaseEntity
    {
        public BklDeviceMetadata() : base()
        {
           
        }
 
        [MaxLength(20), Required] public string GroupName { get; set; }
        [MaxLength(20), Required] public string ProbeName { get; set; }
        [MaxLength(20), Required] public string DeviceType { get; set; }
        [MaxLength(20), Required] public string DeviceName { get; set; }
        [MaxLength(20), Required] public string PDeviceType { get; set; }
        [MaxLength(20), Required] public string PDeviceName { get; set; }
        [MaxLength(20), Required] public string PathType { get; set; }
        [MaxLength(300), Required] public string FullPath { get; set; }
        [MaxLength(50), Required] public string Path1 { get; set; }
        [MaxLength(50), Required] public string Path2 { get; set; }
        [MaxLength(50), Required] public string Path3 { get; set; }
        [MaxLength(50), Required] public string Path4 { get; set; }
        [MaxLength(50), Required] public string Path5 { get; set; }
        [MaxLength(50), Required] public string Path6 { get; set; }
        /// <summary>
        /// ip设备分组 ip设备mac地址
        /// </summary>
        [MaxLength(20), Required] public string MacAddress { get; set; }
        [MaxLength(200), Required] public string ConnectionString { get; set; }

        [MaxLength(20), Required] public string ConnectionType { get; set; }
        [MaxLength(100), Required] public string DeviceMetadata { get; set; }
        [MaxLength(30), Required] public string FactoryName { get; set; }
        [MaxLength(30), Required] public string FacilityName { get; set; }
        [MaxLength(30), Required] public string AreaName { get; set; }
        [Required] public long FactoryId { get; set; }
        [Required] public long FacilityId { get; set; }
        [Required] public long CreatorId { get; set; } 
    }
}
