using MySql.EntityFrameworkCore.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_inspection_task")]
    
    
    public class BklInspectionTask
    {
        public BklInspectionTask()
        {
            Id = SnowId.NextId();
        }
        [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id   {   get; set;   }
        [MaxLength(100), Required]
        public string TaskName { get; set; }
        [Required]
        public long FactoryId { get; set; }
        [MaxLength(100), Required]
        public string FactoryName { get; set; }
        public long CreatorId { get; set; }
        [MaxLength(10), Required]
        public string TaskType { get; set; }
        [MaxLength(10), Required]
        public string TaskStatus { get; set; }
        [MaxLength(50), Required]
        public string TaskDescription { get; set; }
        [Required]
        public int TotalNumber { get; set; }
        [Required]
        public DateTime Createtime { get; set; }
        [Required]
        public DateTime Updatetime { get; set; }
    }
}
