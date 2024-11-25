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
    [Table("bkl_factory")]


    public partial class BklFactory : BaseEntity
    {
        public BklFactory() : base()
        { 
        } 
        [MaxLength(100), Required] public string FactoryName { get; set; }
        [MaxLength(50), Required] public string Country { get; set; }
        [MaxLength(50), Required] public string Province { get; set; }
        [MaxLength(30), Required] public string ProvinceCode { get; set; }
        [MaxLength(50), Required] public string City { get; set; }
        [MaxLength(30), Required] public string CityCode { get; set; }
        [MaxLength(50), Required] public string Distribute { get; set; }
        [MaxLength(30), Required] public string DistributeCode { get; set; }
        [Required] public long CreatorId { get; set; } 
    }
}
