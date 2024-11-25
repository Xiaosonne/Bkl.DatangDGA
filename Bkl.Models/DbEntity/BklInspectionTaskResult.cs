using MySql.EntityFrameworkCore.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_inspection_task_result")]
    
    
    public class BklInspectionTaskResult
    {
        public BklInspectionTaskResult()
        {
            Id = SnowId.NextId();
        }
        [Required]
        public long TaskDetailId { get; set; }
        [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        [Required]
        public long TaskId { get; set; }
        [Required]
        public long FactoryId { get; set; }

        [MaxLength(100), Required] public string FactoryName { get; set; }
        [Required]  public long FacilityId { get; set; }

        [MaxLength(100), Required] public string FacilityName { get; set; }

        [MaxLength(50), Required] public string Position { get; set; }

        [MaxLength(50), Required] public string DamageSize { get; set; }
        [MaxLength(500),Column(TypeName = "VARCHAR(500)"), Required] public string DamageX { get; set; }
        [MaxLength(500), Column(TypeName = "VARCHAR(500)"), Required] public string DamageY { get; set; }
        [MaxLength(500), Column(TypeName = "VARCHAR(500)"), Required] public string DamageWidth { get; set; }
        [MaxLength(500), Column(TypeName = "VARCHAR(500)"), Required] public string DamageHeight { get; set; }

        [MaxLength(20), Required] public string DamageType { get; set; }
        [MaxLength(20), Required] public string DamageLevel { get; set; }
        [MaxLength(50), Required] public string DamagePosition { get; set; }
        [MaxLength(50), Required] public string DamageDescription { get; set; }
        [MaxLength(100), Required] public string TreatmentSuggestion { get; set; }

        [Required] public bool Deleted { get; set; }

        [Required] public DateTime Createtime { get; set; }
    }
}
