using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_dga_gas_production")]
    public class BklDGAGasProduction : BaseEntity
    {
        public BklDGAGasProduction() : base()
        {
            TaskId = "system"; 
        }
        [MaxLength(20), Required] public string TaskId { get; set; } 
        [Required] public long Time { get; set; }
        [Required] public long FacilityRelId { get; set; }
        [Required] public long FactoryRelId { get; set; }
        [Required] public long DeviceRelId { get; set; }
        [MaxLength(20), Required] public string GasName { get; set; }
        [Required] public double Rate { get; set; }
        [MaxLength(20), Required] public string RateType { get; set; } 
    }
}
