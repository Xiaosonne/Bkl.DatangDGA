using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_dga_status")]
    public partial class BklDGAStatus : BaseEntity
    {
        public BklDGAStatus() : base()
        {
        }
         
        /// <summary>
        /// unix time stamp
        /// </summary>
        [Required] public long Time { get; set; }
        [Required] public long FacilityRelId { get; set; }
        [Required] public long FactoryRelId { get; set; }
        [Required] public long DeviceRelId { get; set; }

        [Required] public double CO { get; set; }
        [Required] public double CO2 { get; set; }
        [Required] public double H2 { get; set; }
        [Required] public double O2 { get; set; }
        [Required] public double N2 { get; set; }
        [Required] public double CH4 { get; set; }
        [Required] public double C2H2 { get; set; }
        [Required] public double C2H4 { get; set; }
        [Required] public double C2H6 { get; set; }

        [Required] public double TotHyd { get; set; }
        [Required] public double CmbuGas { get; set; }
        [Required] public double Mst { get; set; }
        [Required] public double OilTmp { get; set; }
        [Required] public double LeakCur { get; set; }
        [Required] public double GasPres { get; set; }

        [Required] public int C2H2_C2H4_Code { get; set; }
        [Required] public int CH4_H2_Code { get; set; }
        [Required] public int C2H4_C2H6_Code { get; set; }

        /// <summary>
        /// 三比值1
        /// </summary>
        [Required] public double C2H2_C2H4_Tatio { get; set; }
        /// <summary>
        /// 三比值2
        /// </summary>
        [Required] public double CH4_H2_Tatio { get; set; }
        /// <summary>
        /// 三比值3
        /// </summary>
        [Required] public double C2H4_C2H6_Tatio { get; set; }

        /// <summary>
        /// 三比值扩充
        /// </summary>
        [Required] public double C2H6_CH4_Tatio { get; set; }

        [Required] public double CO_Inc { get; set; }
        [Required] public double CO2_Inc { get; set; }
        [Required] public double H2_Inc { get; set; }
        [Required] public double O2_Inc { get; set; }
        [Required] public double N2_Inc { get; set; }
        [Required] public double CH4_Inc { get; set; }
        [Required] public double C2H2_Inc { get; set; }
        [Required] public double C2H4_Inc { get; set; }
        [Required] public double C2H6_Inc { get; set; }
        [Required] public double TotHyd_Inc { get; set; }
        [Required] public double CmbuGas_Inc { get; set; }


        /// <summary>
        /// 有载
        /// </summary>
        [Required] public double C2H2_H2_Tatio { get; set; }
        /// <summary>
        /// 油，纸氧化
        /// </summary>
        [Required] public double O2_N2_Tatio { get; set; }
        /// <summary>
        /// 绝缘体劣化分解
        /// </summary>
        [Required] public double CO2_CO_Tatio { get; set; }

        /// <summary>
        /// 油，纸氧化
        /// </summary>
        [Required] public double O2_N2_Inc_Tatio { get; set; }
        /// <summary>
        /// 绝缘体劣化分解
        /// </summary>
        [Required] public double CO2_CO_Inc_Tatio { get; set; }


        /// <summary>
        /// 三比值
        /// </summary>
        [MaxLength(10), Required] public string ThreeTatio_Code { get; set; }

        [Required] public bool Calculated { get; set; } 
        [Required] public long DataId { get; set; }
    }
}
