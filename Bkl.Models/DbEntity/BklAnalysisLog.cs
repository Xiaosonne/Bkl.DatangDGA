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
    [Table("bkl_analysis_log")]
    public partial class BklAnalysisLog : BaseEntity
    {
        public BklAnalysisLog() : base()
        {

        }

        [MaxLength(10), Required]
        public string Level { get; set; }
        [MaxLength(30), Required]
        public string Title { get; set; }
        [MaxLength(200), Required]
        public string Content { get; set; }
        [Required]
        public long FacilityId { get; set; }
        [Required] public long DeviceId { get; set; }
        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }
        [MaxLength(100), Required]
        public string RecordedVideo { get; set; }
        [MaxLength(100), Required]
        public string RecordedPicture { get; set; }
        [MaxLength(100), Required]
        public string RecordedData { get; set; }

        [Required] public long RuleId { get; set; }


        public int Year { get; set; }
        public int AlarmTimes { get; set; }
        public int HandleTimes { get; set; }
        public long OffsetStart { get; set; }
        public long OffsetEnd { get; set; }
        public int Day { get; set; }
        public int Week { get; set; }
        public int Month { get; set; }
        public int HourOfDay { get; set; }
        public int DayOfMonth { get; set; }
        public int DayOfWeek { get; set; }
    }
}
