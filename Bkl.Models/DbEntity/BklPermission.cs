using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_permission")]
    public class BklPermission : BaseEntity
    {
        public BklPermission() : base()
        {
        }

        [MaxLength(20), Required] public string Role { get; set; }
        //read write readwrite 
        [MaxLength(20), Required] public string Control { get; set; }
        //allow deny
        [MaxLength(20), Required] public string Access { get; set; }

        [MaxLength(20), Required] public string TargetType { get; set; }

        [Required] public long TargetId { get; set; }

        [MaxLength(100), Required] public string TargetName { get; set; }

        [Required] public long FactoryId { get; set; }

        [MaxLength(100), Required] public string FactoryName { get; set; }

        [Required] public long CreatorId { get; set; }

    }
}
