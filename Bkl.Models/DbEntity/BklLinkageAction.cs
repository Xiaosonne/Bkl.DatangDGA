using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_linkage_action")]
    public partial class BklLinkageAction : BaseEntity
    {
        public BklLinkageAction() : base()
        {
        } 
        [MaxLength(50), Required] public string Name { get; set; }
        [Required]
        public long LinkageActionId { get; set; }

        [Required] public int Order { get; set; }

        [Required] public int Sleep { get; set; }

        [Required] public long PairId { get; set; }
        [MaxLength(50), Required] public string ConnectionUuid { get; set; }
        [Required] public long AttributeId { get; set; }
        [Required] public int WriteType { get; set; }

        [MaxLength(50), Required] public string WriteStatusName { get; set; }

        [MaxLength(50), Required] public string WriteStatusNameCN { get; set; }

        [MaxLength(50), Required] public string ValueHexString { get; set; }
        [MaxLength(50), Required] public string ValueCN { get; set; }

        [Required] public long CreatorId { get; set; } 


    }
}
