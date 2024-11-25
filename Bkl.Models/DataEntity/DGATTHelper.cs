using System;
using System.Collections.Generic;
using System.Linq;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    public static class DGATTHelper
    {
        public static BklDGAStatus CalculateThreeTatio(BklDbContext context, DeviceStatus status)
        {
            BklDGAStatus bklDGAStatus = new BklDGAStatus();
            var TotHydItem = status.status.FirstOrDefault(s => s.name == "TotHyd");
            var CmbuGasItem = status.status.FirstOrDefault(s => s.name == "CmbuGas");
            var MstItem = status.status.FirstOrDefault(s => s.name == "Mst");
            var OilTmpItem = status.status.FirstOrDefault(s => s.name == "OilTmp");
            var LeakCurItem = status.status.FirstOrDefault(s => s.name == "LeakCur");
            var GasPresItem = status.status.FirstOrDefault(s => s.name == "GasPres");

            var COItem = status.status.FirstOrDefault(s => s.name == "CO");
            var CO2Item = status.status.FirstOrDefault(s => s.name == "CO2");
            var H2Item = status.status.FirstOrDefault(s => s.name == "H2");
            var O2Item = status.status.FirstOrDefault(s => s.name == "O2");
            var N2Item = status.status.FirstOrDefault(s => s.name == "N2");
            var CH4Item = status.status.FirstOrDefault(s => s.name == "CH4");
            var C2H2Item = status.status.FirstOrDefault(s => s.name == "C2H2");
            var C2H4Item = status.status.FirstOrDefault(s => s.name == "C2H4");
            var C2H6Item = status.status.FirstOrDefault(s => s.name == "C2H6");

            bklDGAStatus.TotHyd = double.Parse(TotHydItem?.value ?? "-1");
            bklDGAStatus.CmbuGas = double.Parse(CmbuGasItem?.value ?? "-1");
            bklDGAStatus.Mst = double.Parse(MstItem?.value ?? "-1");
            bklDGAStatus.OilTmp = double.Parse(OilTmpItem?.value ?? "-1");
            bklDGAStatus.LeakCur = double.Parse(LeakCurItem?.value ?? "-1");
            bklDGAStatus.GasPres = double.Parse(GasPresItem?.value ?? "-1");
            bklDGAStatus.CO = double.Parse(COItem?.value ?? "-1");
            bklDGAStatus.CO2 = double.Parse(CO2Item?.value ?? "-1");
            bklDGAStatus.H2 = double.Parse(H2Item?.value ?? "-1");
            bklDGAStatus.O2 = double.Parse(O2Item?.value ?? "-1");
            bklDGAStatus.N2 = double.Parse(N2Item?.value ?? "-1");
            bklDGAStatus.CH4 = double.Parse(CH4Item?.value ?? "-1");
            bklDGAStatus.C2H2 = double.Parse(C2H2Item?.value ?? "-1");
            bklDGAStatus.C2H4 = double.Parse(C2H4Item?.value ?? "-1");
            bklDGAStatus.C2H6 = double.Parse(C2H6Item?.value ?? "-1");

            bklDGAStatus.TotHyd = bklDGAStatus.TotHyd == double.NaN ? -1 : bklDGAStatus.TotHyd;
            bklDGAStatus.CmbuGas = bklDGAStatus.CmbuGas == double.NaN ? -1 : bklDGAStatus.CmbuGas;
            bklDGAStatus.Mst = bklDGAStatus.Mst == double.NaN ? -1 : bklDGAStatus.Mst;
            bklDGAStatus.OilTmp = bklDGAStatus.OilTmp == double.NaN ? -1 : bklDGAStatus.OilTmp;
            bklDGAStatus.LeakCur = bklDGAStatus.LeakCur == double.NaN ? -1 : bklDGAStatus.LeakCur;
            bklDGAStatus.GasPres = bklDGAStatus.GasPres == double.NaN ? -1 : bklDGAStatus.GasPres;
            bklDGAStatus.CO = bklDGAStatus.CO == double.NaN ? -1 : bklDGAStatus.CO;
            bklDGAStatus.CO2 = bklDGAStatus.CO2 == double.NaN ? -1 : bklDGAStatus.CO2;
            bklDGAStatus.H2 = bklDGAStatus.H2 == double.NaN ? -1 : bklDGAStatus.H2;
            bklDGAStatus.O2 = bklDGAStatus.O2 == double.NaN ? -1 : bklDGAStatus.O2;
            bklDGAStatus.N2 = bklDGAStatus.N2 == double.NaN ? -1 : bklDGAStatus.N2;
            bklDGAStatus.CH4 = bklDGAStatus.CH4 == double.NaN ? -1 : bklDGAStatus.CH4;
            bklDGAStatus.C2H2 = bklDGAStatus.C2H2 == double.NaN ? -1 : bklDGAStatus.C2H2;
            bklDGAStatus.C2H4 = bklDGAStatus.C2H4 == double.NaN ? -1 : bklDGAStatus.C2H4;
            bklDGAStatus.C2H6 = bklDGAStatus.C2H6 == double.NaN ? -1 : bklDGAStatus.C2H6;


            //计算三比值
            bklDGAStatus.C2H2_C2H4_Tatio = bklDGAStatus.C2H2 / (bklDGAStatus.C2H4 == 0 ? -1 : bklDGAStatus.C2H4);
            bklDGAStatus.CH4_H2_Tatio = bklDGAStatus.CH4 / (bklDGAStatus.H2 == 0 ? -1 : bklDGAStatus.H2);
            bklDGAStatus.C2H4_C2H6_Tatio = bklDGAStatus.C2H4 / (bklDGAStatus.C2H6 == 0 ? -1 : bklDGAStatus.C2H6);
            //三比值扩充
            bklDGAStatus.C2H6_CH4_Tatio = bklDGAStatus.C2H6 / (bklDGAStatus.CH4 == 0 ? -1 : bklDGAStatus.CH4);
            //其它辅助气体
            bklDGAStatus.C2H2_H2_Tatio = bklDGAStatus.C2H2 / (bklDGAStatus.H2 == 0 ? -1 : bklDGAStatus.H2);
            bklDGAStatus.O2_N2_Tatio = bklDGAStatus.O2 / (bklDGAStatus.N2 == 0 ? -1 : bklDGAStatus.N2);
            bklDGAStatus.CO2_CO_Tatio = bklDGAStatus.CO2 / (bklDGAStatus.CO == 0 ? -1 : bklDGAStatus.CO);

            bklDGAStatus.ThreeTatio_Code = "none";
            bklDGAStatus.Calculated = true;
            bklDGAStatus.DataId = 0;
            bklDGAStatus.DeviceRelId = status.did;
            bklDGAStatus.FacilityRelId = status.faid;
            bklDGAStatus.FactoryRelId = status.fid;
            //bklDGAStatus.Time = long.Parse(status.time.UnixEpochBack().ToString("yyyyMMddHHmmss"));
            bklDGAStatus.Time = status.time;
            bklDGAStatus.Createtime = status.time.UnixEpochBack();

            var last = context.BklDGAStatus.OrderByDescending(q => q.Id).FirstOrDefault();
            if (last != null)
            {
                bklDGAStatus.CO_Inc = bklDGAStatus.CO - last.CO;
                bklDGAStatus.CO2_Inc = bklDGAStatus.CO2 - last.CO2;
                bklDGAStatus.H2_Inc = bklDGAStatus.H2 - last.H2;
                bklDGAStatus.O2_Inc = bklDGAStatus.O2 - last.O2;
                bklDGAStatus.N2_Inc = bklDGAStatus.N2 - last.N2;
                bklDGAStatus.CH4_Inc = bklDGAStatus.CH4 - last.CH4;
                bklDGAStatus.C2H2_Inc = bklDGAStatus.C2H2 - last.C2H2;
                bklDGAStatus.C2H4_Inc = bklDGAStatus.C2H4 - last.C2H4;
                bklDGAStatus.C2H6_Inc = bklDGAStatus.C2H6 - last.C2H6;
                bklDGAStatus.TotHyd_Inc = bklDGAStatus.TotHyd - last.TotHyd;
                bklDGAStatus.CmbuGas_Inc = bklDGAStatus.CmbuGas - last.CmbuGas;
                bool gasIncreamentNotZero = bklDGAStatus.CO_Inc != 0 || bklDGAStatus.CO2_Inc != 0 || bklDGAStatus.H2_Inc != 0 || bklDGAStatus.O2_Inc != 0 || bklDGAStatus.N2_Inc != 0 || bklDGAStatus.CH4_Inc != 0 || bklDGAStatus.C2H2_Inc != 0 || bklDGAStatus.C2H4_Inc != 0 || bklDGAStatus.C2H6_Inc != 0 || bklDGAStatus.TotHyd_Inc != 0 || bklDGAStatus.CmbuGas_Inc != 0;
                if (gasIncreamentNotZero)
                {
                    if (bklDGAStatus.N2_Inc != 0)
                        bklDGAStatus.O2_N2_Inc_Tatio = bklDGAStatus.O2_Inc / bklDGAStatus.N2_Inc;

                    if (bklDGAStatus.CO_Inc != 0)
                        bklDGAStatus.CO2_CO_Inc_Tatio = bklDGAStatus.CO2_Inc / bklDGAStatus.CO_Inc;
                    //todo 需要调试
                    try
                    {
                        var tatio1 = DGATTHelper.threeTatioMap[(nameof(bklDGAStatus.C2H2), nameof(bklDGAStatus.C2H4))]
                              .FirstOrDefault(item => bklDGAStatus.C2H2_C2H4_Tatio >= item.low && bklDGAStatus.C2H2_C2H4_Tatio < item.high);

                        var tatio2 = DGATTHelper.threeTatioMap[(nameof(bklDGAStatus.CH4), nameof(bklDGAStatus.H2))]
                        .FirstOrDefault(item => bklDGAStatus.CH4_H2_Tatio >= item.low && bklDGAStatus.CH4_H2_Tatio < item.high);

                        var tatio3 = DGATTHelper.threeTatioMap[(nameof(bklDGAStatus.C2H4), nameof(bklDGAStatus.C2H6))]
                       .FirstOrDefault(item => bklDGAStatus.C2H4_C2H6_Tatio >= item.low && bklDGAStatus.C2H4_C2H6_Tatio < item.high);
                        bklDGAStatus.C2H2_C2H4_Code = tatio1.label;
                        bklDGAStatus.CH4_H2_Code = tatio2.label;
                        bklDGAStatus.C2H4_C2H6_Code = tatio3.label;
                        bklDGAStatus.ThreeTatio_Code = $"{tatio1.label}{tatio2.label}{tatio3.label}";
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.ToString());
                        return last;
                    }
               
                    context.BklDGAStatus.Add(bklDGAStatus);
                    context.SaveChanges();
                    return bklDGAStatus;
                }
                return last;
            }
            context.BklDGAStatus.Add(bklDGAStatus);
            context.SaveChanges();
            return bklDGAStatus;
        }

        public static Dictionary<(string left, string right), (int label, double low, double high)[]> threeTatioMap = new Dictionary<(string, string), (int, double, double)[]>
        {
            {("C2H2","C2H4"),new (int, double, double)[]{
                (0,double.MinValue,0.1),
                (1,0.1,3.0),
                (2,3.0,double.MaxValue),
            } },
             {("CH4","H2"),new (int, double, double)[]{
                (1,double.MinValue,0.1),
                (0,0.1,1),
                (2,1.0,double.MaxValue),
            } },
              {("C2H4","C2H6"),new (int, double, double)[]{
                (0,double.MinValue,1.0),
                (1,1,3),
                (2,3.0,double.MaxValue),
            } }
        };
        public static Dictionary<(int, int, int), (string, string)> threeTatioCode = new Dictionary<(int, int, int), (string, string)>
        {
            {(0,0,0),("低温过热（低于150℃）","纸包绝缘导线过热，注意CO和CO2的增量和CO2/CO值") },
            {(0,2,0),("低温过热（150℃~300℃）","分接头开关接触不良；引线连接不良；道线接头焊接不良；股间短路引起过热；铁芯多点接地，矽钢片间局部短路等等")},
            {(0,2,1),("中温过热（300℃~700℃）" ,"分接头开关接触不良；引线连接不良；道线接头焊接不良；股间短路引起过热；铁芯多点接地，矽钢片间局部短路等等")},

            {(0,-2,2),("高温过热（高于700℃）","分接头开关接触不良；引线连接不良；道线接头焊接不良；股间短路引起过热；铁芯多点接地，矽钢片间局部短路等等") },

            {(0,1,0),("局部放电","高湿，气隙，毛刺，漆瘤，杂质等所引起的地能量密度的放电") },
            //value <0  => calculateValue <= Math.Abs(value）
            {(2,-1,-2),("低能放电","不同电位之间的火花放电，引线与穿缆套管之间的环流") },
            {(2,2,-2),("低能放电兼过热","不同电位之间的火花放电，引线与穿缆套管之间的环流") },
            {(1,-1,-2),("电弧放电","线圈匝间，层间放电，相见闪络；分接引线间油隙闪络，选择开关拉弧；引线对箱壳或其他接地体放电") },
            {(1,2,-2),("电弧放电兼过热" ,"线圈匝间，层间放电，相见闪络；分接引线间油隙闪络，选择开关拉弧；引线对箱壳或其他接地体放电") },
        };
        public static Dictionary<(string left, string right), (double, double, string, int)[]> asideTatioMap = new Dictionary<(string left, string right), (double, double, string, int)[]>
        {
            {("CO2","CO"),new (double, double, string, int)[]{(double.MinValue,3.0,"固体绝缘劣化分解",0) ,(3.0,7.0,"固体绝缘劣化分解",1),(7.0,double.MaxValue,"固体绝缘劣化分解",2) } },
            {("O2","N2"),new (double, double, string, int)[]{(double.MinValue,0.3,"油/纸氧化",2) ,(0.45,0.55,"油/纸氧化",1),(0.45,0.55,"油/纸氧化",2) } },
        };
    }
}
