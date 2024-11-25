using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_thermal_camera")]
    public class BklThermalCamera : BaseEntity
    {
        public BklThermalCamera() : base()
        {
            Id = SnowId.NextId();
        } 
        [Required] public long DeviceId { get; set; }
        [MaxLength(50), Required] public string Ip { get; set; }
        [Required] public int Port { get; set; }
        [MaxLength(20), Required] public string Account { get; set; }
        [MaxLength(20), Required] public string Password { get; set; }
        [Required] public long UserId { get; set; } 
    }
}
