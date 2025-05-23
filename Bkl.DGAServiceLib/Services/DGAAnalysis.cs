using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Paddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using static Bkl.Models.DGAModel;
public class AlarmServiceGasRatio
{
    public DeviceMeta DeviceMeta { get; set; }
    public ThreeRatioCode GasRatio { get; set; }
}
public class AlarmServiceFeatureGas
{
    public DeviceMeta DeviceMeta { get; set; }
    public string GasName { get; set; }
    public GasDayData[] GasData { get; set; }
}



public class DGAAnalysis : IThreeCodeChecker
{
    static string[] GasNameList = new string[]
{
        nameof(GasName.CH4),
        nameof(GasName.C2H2),
        nameof(GasName.C2H4),
        nameof(GasName.C2H6),
        nameof(GasName.H2),
        nameof(GasName.O2),
        nameof(GasName.N2),
        nameof(GasName.CO),
        nameof(GasName.CO2),
        nameof(GasName.CmbuGas),
        nameof(GasName.TotHyd),
};
    private AlarmThreeCodeRule[] _threeRules;
    private AlarmGPRRule[] _gprRules;
    private BklDeviceMetadata _device;
    private DateTime _loadTime;
    public DGAAlarmConfig _alarmConfig { get; set; }
    public long DeviceId { get; set; }

    public int VolteLevel { get; set; }

    public BklDeviceMetadata Device => _device;
    public List<GasYearData> YearsGasData { get; set; }

    public List<ThreeRatioCode> YearsThreeCodeData { get; set; }
    public long FacilityId { get; set; }
    public long FactoryId { get; set; }
    public DateTime LoadTime { get => _loadTime; }

    public DGAAlarmConfig AlarmConfig { get => _alarmConfig; }

    List<double> H2GasList = new List<double>();

    public UnnormalContextCalc UnnormalState { get; set; } = new UnnormalContextCalc();
    public UnnormalContextCalc UnnormalHistoryState { get; set; } = new UnnormalContextCalc();
    public DGAAnalysis()
    {
        YearsGasData = new List<GasYearData>();
        YearsThreeCodeData = new List<ThreeRatioCode>();
    }
    /// <summary>
    /// 绝对产气率
    /// </summary>
    AbsoluteGasProductionRate CalcAGPR(GasDayData start, GasDayData stop, string name = null)
    {
        var val = ((stop.Value - start.Value) / (stop.UtcTime - start.UtcTime).TotalDays) * (this._alarmConfig.OilBulk);

        //Console.WriteLine($"CalcAGPR  {Device.FactoryName} {Device.DeviceName} {name} Start:{start.UtcTime} StartGas:{start.Value} Stop:{stop.UtcTime} StopGas:{stop.Value} value:{val}");


        if (double.IsInfinity(val) || double.IsNaN(val))
        {
            val = -1;
        }
        return new AbsoluteGasProductionRate
        {
            Day = stop.Day,
            Rate = val
        };
    }
    /// <summary>
    /// 相对产气率
    /// </summary> 
    RelativeGasProductionRate CalcRGPR(GasDayData start, GasDayData stop, string name = null)
    {

        var val = ((stop.Value - start.Value) / (start.Value * (stop.UtcTime - start.UtcTime).TotalDays));

        //Console.WriteLine($"CalcRGPR  {Device.FactoryName}  {Device.DeviceName} {name} Start:{start.UtcTime} StartGas:{start.Value} Stop:{stop.UtcTime} StopGas:{stop.Value} value:{val}");

        if (double.IsInfinity(val) || double.IsNaN(val))
        {
            val = -1;
        }
        return new RelativeGasProductionRate
        {
            Day = stop.Day,
            Rate = val,
        };
    }

    public double ReadGasValue(string namestr)
    {
        var year = YearsGasData.FirstOrDefault(s => s.GasName == namestr && s.Year == DateTime.Now.Year);
        if (year == null)
            return 0;
        var mon = year.MonthData[DateTime.Now.Month - 1];
        if (mon.CurrentDayGasData == null)
            return 0;
        return mon.CurrentDayGasData.Value;
    }
    public double ReadGasAbsValue(string namestr)
    {
        var year = YearsGasData.FirstOrDefault(s => s.GasName == namestr && s.Year == DateTime.Now.Year);
        if (year == null)
            return 0;
        var mon = year.MonthData[DateTime.Now.Month - 1];
        return mon.DaysAGPR[DateTime.Now.Day - 1].Rate;
    }
    public double ReadTotHyd()
    {
        var tot = ReadGasValue(typeof(GasName.TotHyd).Name);
        if (tot != 0)
            return tot;
        return ReadGasValue(typeof(GasName.CH4).Name) +
         ReadGasValue(typeof(GasName.C2H2).Name) +
         ReadGasValue(typeof(GasName.C2H4).Name) +
         ReadGasValue(typeof(GasName.C2H6).Name);
    }
    public class ThreeCodeValidation
    {
        public double h2 { get; set; }
        public double c2h2 { get; set; }
        public double tot { get; set; }
        public double absH2 { get; set; }
        public double absC2H2 { get; set; }
        public double absTotHyd { get; set; }
        public double absCO { get; set; }
        public double absCO2 { get; set; }
        public bool important { get; set; }
        public Dictionary<string, double[]> calcResult { get; set; }

        public override string ToString()
        {
            //if (important)
            //{
            StringBuilder sb = new StringBuilder();
            sb.Append(calcResult[nameof(h2)][1] > -1 ? $"{nameof(h2)},{h2.ToString("0.0")},{calcResult[nameof(h2)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(c2h2)][1] > -1 ? $"{nameof(c2h2)},{c2h2.ToString("0.0")},{calcResult[nameof(c2h2)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(tot)][1] > -1 ? $"{nameof(tot)},{tot.ToString("0.0")},{calcResult[nameof(tot)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(absH2)][1] > -1 ? $"{nameof(absH2)},{absH2.ToString("0.0")},{calcResult[nameof(absH2)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(absC2H2)][1] > -1 ? $"{nameof(absC2H2)},{absC2H2.ToString("0.0")},{calcResult[nameof(absC2H2)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(absTotHyd)][1] > -1 ? $"{nameof(absTotHyd)},{absTotHyd.ToString("0.0")},{calcResult[nameof(absTotHyd)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(absCO)][1] > -1 ? $"{nameof(absCO)},{absCO.ToString("0.0")},{calcResult[nameof(absCO)][1].ToString("0.0")} " : "");
            sb.Append(calcResult[nameof(absCO2)][1] > -1 ? $"{nameof(absCO2)},{absCO2.ToString("0.0")},{calcResult[nameof(absCO2)][1].ToString("0.0")} " : "");
            return sb.ToString();
            //}
            //else
            //{
            //}
        }

    }
    public static ThreeCodeValidation ThreeCodeIsImportant(IThreeCodeChecker checker)
    {
        var h2 = checker.ReadGasValue(typeof(GasName.H2).Name);
        var c2h2 = checker.ReadGasValue(typeof(GasName.C2H2).Name);
        var tot = checker.ReadTotHyd();

        var absH2 = checker.ReadGasAbsValue(typeof(GasName.H2).Name);
        var absC2H2 = checker.ReadGasAbsValue(typeof(GasName.C2H2).Name);
        var absTotHyd = checker.ReadGasAbsValue(typeof(GasName.TotHyd).Name);
        var absCO = checker.ReadGasAbsValue(typeof(GasName.CO).Name);
        var absCO2 = checker.ReadGasAbsValue(typeof(GasName.CO2).Name);

        var a1 = Math.Min(absH2 > 0 ? absH2 / checker.AlarmConfig.H2ar : -1, 1);
        var a2 = Math.Min(absC2H2 > 0 ? absC2H2 / checker.AlarmConfig.C2H2ar : -1, 1);
        var a3 = Math.Min(absTotHyd > 0 ? absTotHyd / checker.AlarmConfig.TotHydar : -1, 1);
        var a4 = Math.Min(absCO > 0 ? absCO / checker.AlarmConfig.COar : -1, 1);
        var a5 = Math.Min(absCO2 > 0 ? absCO2 / checker.AlarmConfig.CO2ar : -1, 1);

        var b1 = Math.Min(h2 > 0 ? h2 / checker.AlarmConfig.H2 : -1, 1);
        var b2 = Math.Min(c2h2 > 0 ? c2h2 / checker.AlarmConfig.C2H2 : -1, 1);
        var b3 = Math.Min(tot > 0 ? tot / checker.AlarmConfig.TotHyd : -1, 1);


        //var thre = a1 * a2 * a3 * a4 * a5 * checker.AlarmConfig.AbsMulTh;
        //var thre2 = checker.AlarmConfig.GasMulTh * b1 * b2 * b3;
        //Console.WriteLine($"ThreeThreshold Gas:{thre} {thre > checker.AlarmConfig.MulTh} GasRatio:{thre2} {thre2 > checker.AlarmConfig.MulTh} ");
        //return thre > checker.AlarmConfig.MulTh || thre2 > checker.AlarmConfig.MulTh;




        var ps1 = new double[] { a1, a2, a3, a4, a5 };

        var ps2 = new double[] { b1, b2, b3 };
        var gas = new Dictionary<string, double[]>
        {
             {"absH2",new double[]{absH2 ,a1 } },
             {"absC2H2",new double[]{absC2H2 ,a2}},
             {"absTotHyd",new double[]{absTotHyd ,a3}},
             {"absCO",new double[]{absCO ,a4}},
             {"absCO2",new double[]{absCO2 ,a5}},
             {"h2",new double[]{h2 ,b1}},
             {"c2h2",new double[]{c2h2 ,b2}},
             {"tot",new double[]{tot ,b3}},
        };


        var countAr = ps1.Where(s => s > -1 && s > checker.AlarmConfig.MulTh).Count();

        var countGas = ps2.Where(s => s > -1 && s > checker.AlarmConfig.MulTh).Count();
        Console.WriteLine($"absH2:{absH2}-{a1}，absC2H2:{absC2H2}-{a2}，absTotHyd:{absTotHyd}-{a3}，absCO:${a4}，absCO2:${a5}，H2:${b1}，c2h2:${b2}，tot:${b3}{countAr},{countGas}");

        return new ThreeCodeValidation
        {
            calcResult = gas,
            important = countAr > 0 || countGas > 0,
            h2 = h2,
            c2h2 = c2h2,
            tot = tot,
            absH2 = absH2,
            absTotHyd = absTotHyd,
            absCO = absCO,
            absCO2 = absCO2,
        };
        //return countAr > 0 || countGas > 0;


    }
    public void OnNewState(DeviceState state)
    {
        var year = YearsGasData.FirstOrDefault(s => s.GasName == state.Name && s.Year == state.CreateTime.Year);
        if (year == null)
        {
            year = new GasYearData()
            {
                Year = state.CreateTime.Year,
                GasName = state.Name,
                Metadata = new GasMetadata
                {
                    //Index = state.PairId,
                    DeviceId = state.DeviceId,
                    FactoryId = state.FactoryId,
                    FacilityId = state.FacilityId,
                    //AttributeId = state.AttributeId,
                    GasNameCN = state.NameCN,
                    ProtocolName = state.ProtocolName,
                    Unit = state.Unit,
                    UnitCN = state.UnitCN,
                }
            };
            YearsGasData.Add(year);
        }

        var mon = year.MonthData[state.CreateTime.Month - 1];
        if (mon.FirstDayGasData == null)
        {
            mon.FirstDayGasData = new GasDayData
            {
                Day = state.CreateTime.Day,
                UtcTime = state.CreateTime.ToUniversalTime(),
                Value = double.Parse(state.Value)
            };
        }
        mon.CurrentDayGasData = new GasDayData
        {
            Day = state.CreateTime.Day,
            UtcTime = state.CreateTime.ToUniversalTime(),
            Value = double.Parse(state.Value)
        };
        mon.DaysGasData[state.CreateTime.Day - 1] = mon.CurrentDayGasData;

        if (mon.CurrentDayGasData.Day != mon.FirstDayGasData.Day || mon.CurrentDayGasData.Value != mon.FirstDayGasData.Value)
        {
            mon.DaysRGPR[mon.CurrentDayGasData.Day - 1] = CalcRGPR(mon.FirstDayGasData, mon.CurrentDayGasData, state.Name);
            mon.DaysAGPR[mon.CurrentDayGasData.Day - 1] = CalcAGPR(mon.FirstDayGasData, mon.CurrentDayGasData, state.Name);
        }
        if (state.Name == "H2")
        {
            var val = double.Parse(state.Value);
            if (H2GasList.Count > 15)
            {
                var vals = H2GasList.Take(10).ToArray();
                UnnormalState.Load(vals);
                UnnormalState.Detect(val);
                //Console.WriteLine($"mean:{mean}" +
                //    $" variance:{variance} " +
                //    $"stdDev:{stdDev} {mean - 3 * stdDev}<={val}<={mean + 3 * stdDev} " +
                //    $"coefficientOfVariation:{coefficientOfVariation} dataRange:{coefficientOfVariation > 0.3} " +
                //    $"KolmogorovSmirnovTest:{ksTestResult < 0.483} " +
                //    $"Normal.CDF:{(p1)} ");
            }
            else
            {
                H2GasList.Add(val);
            }
            UnnormalHistoryState.Detect(val);
        }

        SetThreeSate(state);
    }



    public void SetThreeSate(DeviceState state)
    {
        var ymd = int.Parse(state.CreateTime.ToString("yyyyMMdd"));

        var three = YearsThreeCodeData.FirstOrDefault(s => s.YearMonthDay == ymd);
        if (three == null)
        {
            three = new ThreeRatioCode
            {
                YearMonthDay = ymd
            };
            YearsThreeCodeData.Add(three);
        }
        three.OnNewState(state);
    }


    private AlarmThreeCodeRule[] GetThreeRules(BklDbContext context)
    {
        return context.BklAnalysisRule.AsNoTracking().Where(s => s.DeviceId == DeviceId && s.ProbeName == "三比值法").ToList().Select(s =>
          {
              var reason = ThreeRatioCode.threeTatioCode.Values.FirstOrDefault(t => t.type == s.StatusName);
              return new AlarmThreeCodeRule
              {
                  RuleId = s.Id,
                  RuleName = s.RuleName,
                  RuleProbeName = s.ProbeName,
                  RuleStatusName = s.StatusName,
                  ErrorCode = reason.code,
                  ErrorType = s.StatusName,
                  ErrorReason = reason.reason,
                  Level = s.Level,
                  LevelCN = ((MatchRuleLevelCN)s.Level).ToString()
              };
          }).ToArray();
    }

    private AlarmGPRRule[] GetGPRRules(BklDbContext context)
    {
        return context.BklAnalysisRule.AsNoTracking().Where(s => s.DeviceId == DeviceId && s.ProbeName == "产气速率").ToList().Select(s =>
        {
            return new AlarmGPRRule
            {
                RuleId = s.Id,
                RateType = "AGPR",

                RuleName = s.RuleName,
                RuleProbeName = s.ProbeName,
                RuleStatusName = s.StatusName,


                GasName = s.StatusName,
                Low = double.Parse(s.Min),
                High = double.Parse(s.Max),
                Method = s.Method,
                Level = s.Level,
                LevelCN = ((MatchRuleLevelCN)s.Level).ToString()
            };
        }).ToArray();
    }

    public DgaAlarmResult[] GetGPRAlarmResults()
    {
        AlarmGPRRule[] rules = _gprRules;
        if (rules.Length == 0)
            return new DgaAlarmResult[0];
        return rules.GroupBy(s => s.GasName)
            .Select(gasAlarm =>
            {
                var rate = this.YearsGasData.First(s => s.Year == DateTime.Now.Year)
                  .MonthData.First(s => s.Month == DateTime.Now.Month)
                  .DaysAGPR.First(s => s.Day == DateTime.Now.Day)
                  .Rate;

                var gasA = gasAlarm.Where(rule => rule.RateType == "AGPR" &&
                (rule.Method == "range" && rule.Low <= rate && rate <= rule.High ||
                rule.Method == "biggerThan" && rule.High <= rate ||
                rule.Method == "lessThan" && rule.Low > rate))
                    .FirstOrDefault();
                if (gasA == null)
                    return new DgaAlarmResult[] { DgaAlarmResult.Normal };
                return new DgaAlarmResult[]
                {
                  new DgaAlarmResult {
                      DeviceId = this.DeviceId,
                      FacilityId = this.FacilityId,
                      FactoryId = this.FactoryId,
                      RuleName=gasA.RuleName,
                      RuleProbeName=gasA.RuleProbeName,
                      RuleStatusName=gasA.RuleStatusName,

                      DeviceName=this._device.DeviceName,
                      FacilityName=this._device.FacilityName,
                      FactoryName=this._device.FactoryName,
                        AlarmValue = rate.ToString(),
                        ErrorCode = gasA.GasName,
                        RuleId=gasA.RuleId,
                        ErrorReason = $"{gasA.GasName}产气率{gasA.LevelCN},超过预设区间[{gasA.Low},{gasA.High}]",
                        ErrorType = $"{gasA.GasName}产气率",
                        LevelCN=gasA.LevelCN,
                        Level=gasA.Level,
                        AlarmTime=DateTime.Now,
                    }
                };
            })
            .SelectMany(s => s)
            .ToArray();
    }
    public DgaAlarmResult GetThreeCodeAlarmResults()
    {
        AlarmThreeCodeRule[] rules = _threeRules;

        var three = this.YearsThreeCodeData.OrderByDescending(s => s.YearMonthDay).FirstOrDefault();


        var first = rules.FirstOrDefault(s => s.ErrorType == three.ErrorType);
        if (first == null)
            return DgaAlarmResult.Normal;
        var vaildation = DGAAnalysis.ThreeCodeIsImportant(this);
        if (vaildation.important == false)
        {
            return new DgaAlarmResult
            {
                RuleId = first.RuleId,
                RuleName = first.RuleName,
                RuleProbeName = first.RuleProbeName,
                RuleStatusName = first.RuleStatusName,

                DeviceId = this.DeviceId,
                FacilityId = this.FacilityId,
                FactoryId = this.FactoryId,
                ErrorCode = "正常",
                ErrorReason = "三比值故障不明显",
                ErrorType = "正常",
                AlarmValue = three.ThreeTatio_Code,
                AlarmTime = three.CreateTime,
                Level = (int)MatchRuleLevel.Normal,
                LevelCN = MatchRuleLevelCN.正常.ToString(),
                DeviceName = this._device.DeviceName,
                FactoryName = this._device.FactoryName,
                FacilityName = this._device.FacilityName,
            };
        }
        else
        {
            return new DgaAlarmResult
            {
                RuleId = first.RuleId,
                RuleName = first.RuleName,
                RuleProbeName = first.RuleProbeName,
                RuleStatusName = first.RuleStatusName,

                DeviceId = this.DeviceId,
                FacilityId = this.FacilityId,
                FactoryId = this.FactoryId,
                ErrorCode = three.ErrorCode,
                ErrorReason = three.ErrorReason + " " + vaildation.ToString(),
                ErrorType = three.ErrorType,
                AlarmValue = three.ThreeTatio_Code,
                AlarmTime = three.CreateTime,
                Level = first.Level,
                LevelCN = first.LevelCN,
                DeviceName = this._device.DeviceName,
                FactoryName = this._device.FactoryName,
                FacilityName = this._device.FacilityName,
            };
        }


    }

    public void LoadFromDatabase(BklDbContext context)
    {
        var start = DateTime.Now.Subtract(TimeSpan.FromDays(180)).UnixEpoch();
        var end = DateTime.Now.UnixEpoch();
        var nodes = context.ModbusNodeInfo.AsNoTracking().ToList();
        var pais = context.ModbusDevicePair.Where(s => s.DeviceId == this.DeviceId).AsNoTracking().ToList();
        var t1 = typeof(BklDGAStatus);

        var olddata = context.BklDGAStatus.Where(s => s.DeviceRelId == this.DeviceId && s.Time >= start && s.Time <= end)
            .AsNoTracking()
            .ToList()
            .GroupBy(s => s.Time.UnixEpochBack().ToString("yyyyMM"))
            .Select(s => new
            {
                key = s.Key,
                end = s.OrderByDescending(s => s.Time).FirstOrDefault(),
                start = s.OrderBy(s => s.Time).FirstOrDefault(),
            })
            .ToArray();

        foreach (var data in olddata)
        {
            foreach (var gasname in GasNameList)
            {
                var time = data.start.Createtime;
                var year = YearsGasData.FirstOrDefault(s => s.GasName == gasname && s.Year == time.Year);
                if (year == null)
                {
                    var nodeid = pais.Where(s => s.DeviceId == data.start.DeviceRelId).Select(s => s.NodeId).ToArray();
                    var node = nodes.Where(s => nodeid.Contains(s.Id) && s.StatusName == gasname).FirstOrDefault();
                    if (node == null)
                    {
                        Console.WriteLine($"LoadFromDatabase {this.DeviceId} {gasname} gas null");
                        continue;
                    }
                    var pair = pais.Where(s => s.DeviceId == this.DeviceId && s.ProtocolName == node.ProtocolName && s.NodeId == node.Id).FirstOrDefault();
                    if (pair == null)
                    {
                        Console.WriteLine($"LoadFromDatabase {this.DeviceId} {gasname} pair null");
                        continue;
                    }
                    year = new GasYearData
                    {
                        Year = time.Year,
                        GasName = gasname,
                        Metadata = new GasMetadata
                        {
                            Index = pair.Id,
                            DeviceId = data.start.DeviceRelId,
                            FactoryId = data.start.FactoryRelId,
                            FacilityId = data.start.FacilityRelId,
                            AttributeId = node.Id,
                            GasNameCN = node.StatusNameCN,
                            ProtocolName = node.ProtocolName,
                            Unit = node.Unit,
                            UnitCN = node.UnitCN,
                        }
                    };
                    YearsGasData.Add(year);
                }
                try
                {
                    var method = t1.GetProperty(gasname).GetGetMethod();
                    var valstr = method.Invoke(data.start, null)?.ToString();
                    var valstr2 = method.Invoke(data.end, null)?.ToString();
                    var mon = year.MonthData.FirstOrDefault(s => s.Month == time.Month);
                    mon.FirstDayGasData = new GasDayData
                    {
                        Day = data.start.Createtime.Day,
                        UtcTime = data.start.Createtime.ToUniversalTime(),
                        Value = double.TryParse(valstr, out var val) ? val : 0,
                    };
                    mon.CurrentDayGasData = new GasDayData
                    {
                        Day = data.end.Createtime.Day,
                        UtcTime = data.end.Createtime.ToUniversalTime(),
                        Value = double.TryParse(valstr2, out var val1) ? val1 : 0,
                    };
                    mon.DaysGasData[time.Day - 1] = new GasDayData
                    {
                        Day = time.Day,
                        UtcTime = time.ToUniversalTime(),
                        Value = val1
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

            }
        }
        var gas = olddata.Take(10).Select(q => q.start.H2).ToArray();
        UnnormalHistoryState.Load(gas);

        _device = context.BklDeviceMetadata.Where(s => s.Id == this.DeviceId).AsNoTracking().FirstOrDefault();
        _alarmConfig = TryCatchExtention.TryCatch(() => JsonSerializer.Deserialize<DGAAlarmConfig>(_device.DeviceMetadata), DGAAlarmConfig.Default, null);
        _threeRules = GetThreeRules(context);
        _gprRules = GetGPRRules(context);
        _loadTime = DateTime.Now;
    }

    public DgaPushData Serialized()
    {
        var ratio = YearsThreeCodeData.OrderBy(s => s.YearMonthDay).LastOrDefault();
        var push = new DgaPushData
        {
            DeviceId = DeviceId,
            FacilityId = FacilityId,
            FactoryId = FactoryId,
            Createtime = DateTime.Now,
            GasData = YearsGasData.Where(s => s.Year == DateTime.Now.Year)
            .GroupBy(s => s.GasName)
            .Select(s => s.First())
            .Select(s => new
            {
                GasName = s.GasName,
                GasMeta = s.Metadata,
                Gas = s.MonthData.Where(s => s.CurrentDayGasData != null && s.CurrentDayGasData.Day == DateTime.Now.Day).FirstOrDefault()
            })
            .Select(s => TryCatchExtention.TryCatch(() =>
            {
                var da = new GasPushData
                {
                    GasMetadata = s.GasMeta,
                    UtcTime = s.Gas.CurrentDayGasData.UtcTime,
                    GasValue = s.Gas.CurrentDayGasData.Value,
                    GasIncValue = s.Gas.CurrentDayGasData.Value - s.Gas.FirstDayGasData.Value,
                    GasName = s.GasName,
                    AGPR = s.Gas.DaysAGPR[DateTime.Now.Day - 1].Rate,
                    RGPR = s.Gas.DaysRGPR[DateTime.Now.Day - 1].Rate,
                };
                return da;
            }))
            .Where(s => s != null)
            .ToArray(),
            ThreeRatio = ratio,
            RealUnnormal = UnnormalState,
            HistoryUnnormal = UnnormalHistoryState,
        };
        return push;
        //return JsonSerializer.Serialize(push);
    }
}