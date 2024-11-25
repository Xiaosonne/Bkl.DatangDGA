using System;
using System.Collections.Generic;
using System.Linq;
using static Bkl.Models.DGAModel;

public class ThreeRatioCode : ThreeRatioEntity
{
    public static Dictionary<string, int> NAME_MAP = new Dictionary<string, int>
        {
            {nameof(GasName.CH4),0 },
            {nameof(GasName.C2H2),1},
            {nameof(GasName.C2H4),2 },
            {nameof(GasName.C2H6),3 },
            {nameof(GasName.H2),4 },
            {nameof(GasName.O2),5 },
            {nameof(GasName.N2),6 },
            {nameof(GasName.CO),7 },
            {nameof(GasName.CO2),8},
        };

    public static Dictionary<(int, int, int), (string type, string reason, string code)> threeTatioCode = new Dictionary<(int, int, int), (string type, string reason, string code)>
        {
            {(0,0,0),("低温过热(t<150℃)","纸包绝缘导线过热，注意CO和CO2的增量和CO2/CO值","T1") },
            {(0,2,0),("低温过热(150℃<t<300℃)","分接头开关接触不良；引线连接不良；道线接头焊接不良；股间短路引起过热；铁芯多点接地，矽钢片间局部短路等等","T1")},
            {(0,2,1),("中温过热(300℃<t<700℃)" ,"分接头开关接触不良；引线连接不良；道线接头焊接不良；股间短路引起过热；铁芯多点接地，矽钢片间局部短路等等","T2")},
            {(0,-2,2),("高温过热(t>700℃)","分接头开关接触不良；引线连接不良；道线接头焊接不良；股间短路引起过热；铁芯多点接地，矽钢片间局部短路等等","T3") },
            {(0,1,0),("局部放电","高湿，气隙，毛刺，漆瘤，杂质等所引起的地能量密度的放电","PD") },
            {(2,-1,-2),("低能放电","不同电位之间的火花放电，引线与穿缆套管之间的环流","D1") },
            {(2,2,-2),("低能放电兼过热","不同电位之间的火花放电，引线与穿缆套管之间的环流","D1") },
            {(1,-1,-2),("电弧放电","线圈匝间，层间放电，相见闪络；分接引线间油隙闪络，选择开关拉弧；引线对箱壳或其他接地体放电","D2") },
            {(1,2,-2),("电弧放电兼过热" ,"线圈匝间，层间放电，相见闪络；分接引线间油隙闪络，选择开关拉弧；引线对箱壳或其他接地体放电","D2") },
        };


    public static Dictionary<(string left, string right), (int label, double low, double high)[]> threeTatioMap = new Dictionary<(string, string), (int, double, double)[]>
        {
            {("C2H2","C2H4"),new (int, double, double)[]{
                (0      ,double.MinValue  ,0.1),
                (1      ,0.1              ,3.0),
                (2      ,3.0              ,double.MaxValue),
            } },
             {("CH4","H2"),new (int, double, double)[]{
                (1      ,double.MinValue  ,0.1),
                (0      ,0.1              ,1),
                (2      ,1.0              ,double.MaxValue),
            } },
              {("C2H4","C2H6"),new (int, double, double)[]{
                (0      ,double.MinValue  ,1.0),
                (1      ,1                ,3),
                (2      ,3.0              ,double.MaxValue),
            } }
        };



    public ThreeRatioCode()
    {
        GasValue = new double[9];
        GasRatioValue = new double[9];
    }

    public static Dictionary<string, int> NAME_RATIO_MAP = new Dictionary<string, int>
        {
            {nameof(GasName.C2H2_C2H4_Tatio),0 },
            {nameof(GasName.CH4_H2_Tatio),1 },
            {nameof(GasName.C2H4_C2H6_Tatio),2 },
            {nameof(GasName.CO2_CO_Tatio),3 },
            {nameof(GasName.O2_N2_Tatio),4},
            {nameof(GasName.C2H2_H2_Tatio),5 },
            {nameof(GasName.C2H6_CH4_Tatio),6 },
        };

    public void OnNewState(DeviceState state)
    {
        if (false == NAME_MAP.ContainsKey(state.Name))
            return;
        GasValue[NAME_MAP[state.Name]] = double.Parse(state.Value);
        C2H2_C2H4 = Div<GasName.C2H2, GasName.C2H4>();
        CH4_H2 = Div<GasName.CH4, GasName.H2>();
        C2H4_C2H6 = Div<GasName.C2H4, GasName.C2H6>();
        CO2_CO = Div<GasName.CO2, GasName.CO>();

        O2_N2 = Div<GasName.O2, GasName.N2>();
        C2H2_H2 = Div<GasName.C2H2, GasName.H2>();
        C2H6_CH4 = Div<GasName.C2H6, GasName.CH4>();

        this.CreateTime = DateTime.Now;
        try
        {
            var tatio1 = Ratio<GasName.C2H2, GasName.C2H4>();
            var tatio2 = Ratio<GasName.CH4, GasName.H2>();
            var tatio3 = Ratio<GasName.C2H4, GasName.C2H6>();

            if (tatio1 != C2H2_C2H4_Code || tatio2 != CH4_H2_Code || tatio3 != C2H4_C2H6_Code)
            {
                C2H2_C2H4_Code = tatio1;
                CH4_H2_Code = tatio2;
                C2H4_C2H6_Code = tatio3;

                ThreeTatio_Code = $"{tatio1}{tatio2}{tatio3}";

                var first = threeTatioCode.Where(item =>
                Compare(C2H2_C2H4_Code, item.Key.Item1) &&
                Compare(CH4_H2_Code, item.Key.Item2) &&
                Compare(C2H4_C2H6_Code, item.Key.Item3))
                .FirstOrDefault();

                ErrorType = first.Value.type;
                ErrorReason = first.Value.reason;
                ErrorCode = first.Value.code;
                Console.WriteLine($"ThreeCode {ThreeTatio_Code} {tatio1} {tatio2} {tatio3} {ErrorType} {ErrorReason} {ErrorCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

    }
    private bool Compare(int value, int code)
    {
        return (code == value || code < 0 && value <= Math.Abs(code));
    }
    private int Ratio<T1, T2>() where T1 : GasName where T2 : GasName
    {
        var ratio = GasRatioValue[NAME_RATIO_MAP[$"{typeof(T1).Name}_{typeof(T2).Name}_Tatio"]];

        return threeTatioMap[(typeof(T1).Name, typeof(T2).Name)]
            .FirstOrDefault(item => ratio >= item.low && ratio < item.high)
            .label;
    }
    private double Div<T1, T2>() where T1 : GasName where T2 : GasName
    {
        if (GasValue[NAME_MAP[typeof(T2).Name]] == 0)
            return -1;
        try
        {
            var val = GasValue[NAME_MAP[typeof(T1).Name]] / GasValue[NAME_MAP[typeof(T2).Name]];
            if (double.IsInfinity(val) || double.IsNaN(val))
            {
                val = -100;
            }
            GasRatioValue[NAME_RATIO_MAP[$"{typeof(T1).Name}_{typeof(T2).Name}_Tatio"]] = val;
            return val;
        }
        catch
        {
            GasRatioValue[NAME_RATIO_MAP[$"{typeof(T1).Name}_{typeof(T2).Name}_Tatio"]] = -1;
            return -1;
        }
    }
}
