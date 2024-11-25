using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{

    [Table("bkl_user_granted")]
    public class BklUserGranted : BaseEntity
    {
        public BklUserGranted() : base()
        {

        }

        [Required] public long UserId { get; set; }

        [MaxLength(20), Required] public string UserName { get; set; }

        [Required] public long FactoryId { get; set; }
        [MaxLength(20), Required] public string FactoryName { get; set; }

        [Required] public long FacilityId { get; set; }
        [MaxLength(20), Required] public string FacilityName { get; set; }

        [MaxLength(200), Column(TypeName = "VARCHAR(200)"), Required] public string Roles { get; set; }


        [Required] public bool Deleted { get; set; }
        [Required] public long CreatorId { get; set; }
    }
}
