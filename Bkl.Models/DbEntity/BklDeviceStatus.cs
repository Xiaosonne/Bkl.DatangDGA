using MySql.EntityFrameworkCore.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_device_status")]


    public partial class BklDeviceStatus : BaseEntity
    {
        public BklDeviceStatus() : base()
        {
            
        }
        
        [Required] public long Time { get; set; }
        [MaxLength(20), Required] public string TimeType { get; set; }
        [MaxLength(20), Required] public string StatusName { get; set; }
        [MaxLength(20), Required] public string GroupName { get; set; }
        [Required] public double StatusValue { get; set; }
        [Required] public long FacilityRelId { get; set; }
        [Required] public long FactoryRelId { get; set; }
        [Required] public long DeviceRelId { get; set; } 
    }
}
