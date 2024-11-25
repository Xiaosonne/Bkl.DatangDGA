using MySql.EntityFrameworkCore.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_inspection_task_detail")]
    
    
    public class BklInspectionTaskDetail
    {
        public BklInspectionTaskDetail()
        {
            Id = SnowId.NextId();
        }
        [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        [Required]
        public long TaskId { get; set; }
        [Required]
        public long FactoryId { get; set; }
        [MaxLength(100), Required]
        public string FactoryName { get; set; }
        public long FacilityId { get; set; }
        [MaxLength(100), Required]
        public string FacilityName { get; set; }
        [MaxLength(50), Required]
        public string Position { get; set; }
        [MaxLength(150), Required]
        public string LocalImagePath { get; set; }

         [ Required]
        public int Error { get; set; }

        [MaxLength(150), Required]
        public string RemoteImagePath { get; set; }
        [MaxLength(10), Required]
        public string ImageType { get; set; }
        [MaxLength(20), Required]
        public string ImageWidth { get; set; }
        [MaxLength(20), Required]
        public string ImageHeight { get; set; }
        [ Required]
        public DateTime Createtime { get; set; }
    }
}
