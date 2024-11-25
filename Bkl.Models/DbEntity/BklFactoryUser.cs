using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_factory_user")]
    public partial class BklFactoryUser : BaseEntity
    {
        public BklFactoryUser() : base()
        { 
        } 
        [MaxLength(20), Required] public string UserName { get; set; }
        [MaxLength(20), Required] public string Account { get; set; }
        [MaxLength(50), Required] public string Password { get; set; }
        [Required] public long CreatorId { get; set; }
        [Required] public long FactoryId { get; set; } 
        [MaxLength(200), Required] public string Roles { get; set; }
        [MaxLength(200), Required] public string Positions { get; set; }
    }
}
