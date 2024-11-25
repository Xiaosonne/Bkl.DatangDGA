using System;
using System.Linq;
using static Bkl.Models.DGAModel;
using static Bkl.Models.DGAModel.GasName;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    public class DGAModel
    {
        public class UnnormalContext
        {
            public double KsTest { get; set; }
            public double Var { get; set; }
            public double Mean { get; set; }
            public double StdDev { get; set; }
            public double CofVar { get; set; }
            public bool UnnormalCofVar { get; set; }
            public double Proper { get; set; }
            public bool UnnormalProper { get; set; }
            public DateTime UnnormalTime { get; set; }
            public string UnnormalText { get; set; }
        }
        public class AlarmThreeCodeRule
        {
            public string ErrorCode { get; set; }
            public int Level { get; set; }
            public string LevelCN { get; set; }
            public string ErrorType { get; set; }
            public string ErrorReason { get; set; }
            public long RuleId { get; set; }
            public string RuleName { get; set; }
            public string RuleProbeName { get; set; }
            public string RuleStatusName { get; set; }
        }
        public class AlarmGPRRule
        {
            public string GasName { get; set; }
            public int Level { get; set; }
            public string LevelCN { get; set; }
            public double Low { get; set; }
            public double High { get; set; }
            public string Method { get; set; }
            public string RateType { get; set; }
            public long RuleId { get; set; }
            public string RuleName { get; set; }
            public string RuleProbeName { get; set; }
            public string RuleStatusName { get; set; }
        }

        public class DgaAlarmResult
        {
            public string AlarmValue { get; set; }
            public int Level { get; set; }
            public string LevelCN { get; set; }

            public string ErrorCode { get; set; }
            public string ErrorType { get; set; }
            public string ErrorReason { get; set; }
            public DateTime AlarmTime { get; set; }
            public long RuleId { get; set; }
            public long DeviceId { get; set; }
            public long FacilityId { get; set; }
            public long FactoryId { get; set; }
            public string DeviceName { get; set; }
            public string FactoryName { get; set; }
            public string FacilityName { get; set; }
            public string RuleName { get; set; }
            public string RuleProbeName { get; set; }
            public string RuleStatusName { get; set; }
            public DateTime StartTime { get; set; }

            public static DgaAlarmResult Normal = new DgaAlarmResult
            {
                Level = 40,
                LevelCN = "正常"
            };

        }
        public class DgaPushData
        {
            public long DeviceId { get; set; }

            public long FacilityId { get; set; }
            public long FactoryId { get; set; }
            public GasPushData[] GasData { get; set; }
            public ThreeRatioEntity ThreeRatio { get; set; }
            public DateTime Createtime { get; set; }
            public UnnormalContext RealUnnormal { get; set; }
            public UnnormalContext HistoryUnnormal { get; set; }

            public DeviceDgaUpdateStatus[] ToThreeStatus()
            {
                return new DeviceDgaUpdateStatus[]
                {
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.C2H2_C2H4_Code),Value=ThreeRatio.C2H2_C2H4_Code .ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.C2H4_C2H6_Code),Value=ThreeRatio.C2H4_C2H6_Code.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.CH4_H2_Code),Value=ThreeRatio.CH4_H2_Code.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.C2H2_C2H4)+"_Tatio",Value=ThreeRatio.C2H2_C2H4.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.C2H4_C2H6)+"_Tatio",Value=ThreeRatio.C2H4_C2H6.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.CH4_H2)+"_Tatio",Value=ThreeRatio.CH4_H2.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.CO2_CO)+"_Tatio",Value=ThreeRatio.CO2_CO.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.O2_N2)+"_Tatio",Value=ThreeRatio.O2_N2.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.C2H6_CH4)+"_Tatio",Value=ThreeRatio.C2H6_CH4.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= nameof(ThreeRatio.C2H2_H2)+"_Tatio",Value=ThreeRatio.C2H2_H2.ToString()},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= "ThreeErrorCode",Value=ThreeRatio.ErrorCode},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= "ThreeErrorType",Value=ThreeRatio.ErrorType},
                    new DeviceDgaUpdateStatus {DeviceId=DeviceId ,FacilityId=FacilityId,FactoryId=FactoryId,
                        Name= "ThreeErrorReason",Value=ThreeRatio.ErrorReason},

                };
            }
            public DeviceDgaUpdateStatus[] ToStatus()
            {
                return ToThreeStatus()
                    .Concat(GasData.SelectMany(s => s.ToStatus()))
                .ToArray();
            }

        }
        public class GasPushData
        {
            public DateTime UtcTime { get; set; }
            public string GasName { get; set; }
            public double GasValue { get; set; }
            public double AGPR { get; set; }
            public double RGPR { get; set; }
            public double GasIncValue { get; set; }
            public GasMetadata GasMetadata { get; set; }

            public DeviceDgaUpdateStatus[] ToStatus()
            {
                return new DeviceDgaUpdateStatus[]{
                    new DeviceDgaUpdateStatus
                    {
                        Name = GasName,
                        NameCN = GasMetadata.GasNameCN,
                        AttributeId = GasMetadata.AttributeId,
                        FactoryId = GasMetadata.FactoryId,
                        FacilityId = GasMetadata.FacilityId,
                        DeviceId = GasMetadata.DeviceId,
                        Index = GasMetadata.Index,
                        Unit = GasMetadata.Unit,
                        UnitCN = GasMetadata.UnitCN,
                        Value = GasValue.ToString(),
                    },
                     new DeviceDgaUpdateStatus
                    {
                        Name = GasName+"_Inc",
                        NameCN = GasMetadata.GasNameCN+"增量",
                        FactoryId = GasMetadata.FactoryId,
                        FacilityId = GasMetadata.FacilityId,
                        DeviceId = GasMetadata.DeviceId,
                        //Index = GasMetadata.Index,
                        Unit = GasMetadata.Unit,
                        UnitCN = GasMetadata.UnitCN,
                        Value = GasIncValue.ToString(),
                    },
                     new DeviceDgaUpdateStatus
                    {
                        Name = GasName+"_AGPR",
                        NameCN = GasMetadata.GasNameCN+"绝对产期率",
                        FactoryId = GasMetadata.FactoryId,
                        FacilityId = GasMetadata.FacilityId,
                        DeviceId = GasMetadata.DeviceId,
                        Unit = "L/d",
                        UnitCN = "L/d",
                        Value = AGPR.ToString(),
                    },
                      new DeviceDgaUpdateStatus
                    {
                        Name = GasName+"_RGPR",
                        NameCN = GasMetadata.GasNameCN+"相对产期率",
                        FactoryId = GasMetadata.FactoryId,
                        FacilityId = GasMetadata.FacilityId,
                        DeviceId = GasMetadata.DeviceId,
                        Unit = "L/d",
                        UnitCN = "L/d",
                        Value = RGPR.ToString(),
                    }

                };
            }
        }
        public class GasMetadata
        {
            //public long Index { get; set; }
            //public long AttributeId { get; set; }
            public string GasNameCN { get; set; }
            public string ProtocolName { get; set; }
            public string Unit { get; set; }
            public string UnitCN { get; set; }
            public long DeviceId { get; set; }
            public long FactoryId { get; set; }
            public long FacilityId { get; set; }
            public long Index { get; set; }
            public long AttributeId { get; set; }
        }

        public class GasYearData
        {
            public int Year { get; set; }

            public string GasName { get; set; }
            public GasMonthData[] MonthData { get; set; }

            public GasMetadata Metadata { get; set; }

            public GasYearData()
            {
                MonthData = new GasMonthData[12];
                foreach (var i in Enumerable.Range(0, 12))
                {
                    MonthData[i] = new GasMonthData { Month = i + 1 };
                }
            }
        }
        public class GasName
        {

            public class H2 : GasName { }
            public class CO : GasName { }
            public class CO2 : GasName { }
            public class CH4 : GasName { }
            public class C2H2 : GasName { }
            public class C2H4 : GasName { }
            public class C2H6 : GasName { }
            public class N2 : GasName { }
            public class O2 : GasName { }
            public class TotHyd : GasName { }
            public class CmbuGas : GasName { }


            public class C2H2_C2H4_Code : GasName { }
            public class CH4_H2_Code : GasName { }
            public class C2H4_C2H6_Code : GasName { }
            public class C2H2_C2H4_Tatio : GasName { }
            public class CH4_H2_Tatio : GasName { }
            public class C2H4_C2H6_Tatio : GasName { }
            public class C2H6_CH4_Tatio : GasName { }

            public class C2H2_H2_Tatio : GasName { }
            public class O2_N2_Tatio : GasName { }
            public class CO2_CO_Tatio : GasName { }
        }
        public class GasDayData
        {
            public int Day { get; set; }
            public DateTime UtcTime { get; set; }
            public double Value { get; set; }
        }
        public class AbsoluteGasProductionRate
        {
            public double Rate { get; set; }
            public int Day { get; set; }

        }
        public class RelativeGasProductionRate
        {
            public double Rate { get; set; }
            public int Day { get; set; }
        }
        public class GasMonthData
        {
            public int Year { get; set; }

            public int Month { get; set; }

            public GasDayData[] DaysGasData { get; set; }

            public GasDayData FirstDayGasData { get; set; }

            public GasDayData CurrentDayGasData { get; set; }

            public AbsoluteGasProductionRate[] DaysAGPR { get; set; }

            public RelativeGasProductionRate[] DaysRGPR { get; set; }

            public GasMonthData()
            {
                DaysGasData = new GasDayData[31];
                DaysAGPR = new AbsoluteGasProductionRate[30];
                DaysRGPR = new RelativeGasProductionRate[30];
                foreach (var item in Enumerable.Range(0, 30))
                {
                    DaysAGPR[item] = new AbsoluteGasProductionRate() { Day = item + 1, Rate = 0 };
                    DaysRGPR[item] = new RelativeGasProductionRate() { Day = item + 1, Rate = 0 };
                }
                foreach (var item in Enumerable.Range(0, 31))
                {
                    DaysGasData[item] = new GasDayData() { Day = item + 1 };
                }
            }
        }

        public class ThreeRatioEntity
        {
            public double C2H2_C2H4 { get; set; }
            public double CH4_H2 { get; set; }
            public double C2H4_C2H6 { get; set; }
            public double CO2_CO { get; set; }
            public double C2H2_H2 { get; set; }
            public double O2_N2 { get; set; }
            public double C2H6_CH4 { get; set; }
            public DateTime CreateTime { get; set; }
            public int YearMonthDay { get; set; }

            public double[] GasValue { get; set; }
            public double[] GasRatioValue { get; set; }
            public int C2H2_C2H4_Code { get; set; }
            public int CH4_H2_Code { get; set; }
            public int C2H4_C2H6_Code { get; set; }
            public string ThreeTatio_Code { get; set; }
            /// <summary>
            /// 低温过热 
            /// </summary>
            public string ErrorType { get; set; }
            /// <summary>
            /// 预警原因
            /// </summary>
            public string ErrorReason { get; set; }
            /// <summary>
            /// t1 t2 t3 pd 
            /// </summary>
            public string ErrorCode { get; set; }

        }
    }
}
