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

    [Table("bkl_analysis_rule")]


    public partial class BklAnalysisRule : BaseEntity
    {
        public BklAnalysisRule() : base()
        {

        }

        /// <summary>
        /// 红外 区域名称
        /// </summary>
        [MaxLength(150), Required] public string ProbeName { get; set; }
        [MaxLength(150), Required] public string RuleName { get; set; }
        [MaxLength(150), Required] public string StatusName { get; set; }

        [MaxLength(50), Required] public string DeviceType { get; set; }
        [MaxLength(50), Required] public string StartTime { get; set; }
        [MaxLength(50), Required] public string EndTime { get; set; }
        [MaxLength(50), Required] public string TimeType { get; set; }
        [MaxLength(50), Required] public string Min { get; set; }
        [MaxLength(50), Required] public string Max { get; set; }

        [MaxLength(50), Required] public string Method { get; set; }
        [MaxLength(10), Required] public int Level { get; set; }
        /// <summary>
        /// 红外 areaRule
        /// 绑带 组名称 
        /// </summary>
        [MaxLength(200), Required] public string ExtraInfo { get; set; }


        [Required] public long AttributeId { get; set; }
        [Required] public long PairId { get; set; }
        [Required] public long LinkageActionId { get; set; }
        [Required] public long FactoryId { get; set; }
        [Required] public long CreatorId { get; set; }
        [Required] public long DeviceId { get; set; }


    }
}
