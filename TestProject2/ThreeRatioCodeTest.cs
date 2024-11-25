using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Bkl.Models.DGAModel;
using static DGAAnalysis;

namespace TestProject2
{
    public class ThreeRatioCodeTest
    {
        private ITestOutputHelper _log;

        public ThreeRatioCodeTest(ITestOutputHelper console)
        {
            _log = console;
        }

        [Fact]
        public void RebuildDb()
        {
            SnowId.SetIdGenerator(new Yitter.IdGenerator.IdGeneratorOptions { });

            var host = "192.168.3.100";
            var port = 15400;
            var username = "test";
            var password = "YHBLsqt1!2@3#";
            //var password = "asd123...";
            //var database = "test";
            var database = "esps";
            var builder = new DbContextOptionsBuilder<BklDbContext>().UseOpenGauss($"host={host};port={port};username={username};password={password};database={database}");
            BklDbContext context = new BklDbContext((DbContextOptions<BklDbContext>)builder.Options);
            var all = context.BklDGAStatus.ToList();
            foreach (var da in all)
            {
                ThreeRatioCode code = new ThreeRatioCode();
                code.OnNewState(new DeviceState
                {
                    Name = "H2",
                    Value = da.H2.ToString()
                });
                code.OnNewState(new DeviceState
                {
                    Name = "CH4",
                    Value = da.CH4.ToString(),
                });
                code.OnNewState(new DeviceState
                {
                    Name = "C2H6",
                    Value = da.C2H6.ToString(),
                });
                code.OnNewState(new DeviceState
                {
                    Name = "C2H4",
                    Value = da.C2H4.ToString(),
                });
                code.OnNewState(new DeviceState
                {
                    Name = "CO",
                    Value = da.CO.ToString(),
                });
                code.OnNewState(new DeviceState
                {
                    Name = "CO2",
                    Value = da.CO2.ToString(),
                });
                code.OnNewState(new DeviceState
                {
                    Name = "O2",
                    Value = da.O2.ToString(),
                });
                code.OnNewState(new DeviceState
                {
                    Name = "N2",
                    Value = da.N2.ToString(),
                });
                da.ThreeTatio_Code = code.ThreeTatio_Code;
                da.CO2_CO_Tatio = code.CO2_CO;
                da.O2_N2_Tatio = code.O2_N2;
                da.C2H2_H2_Tatio = code.C2H2_H2;
                da.C2H2_C2H4_Tatio = code.C2H2_C2H4;
                da.C2H2_C2H4_Code = code.C2H2_C2H4_Code;
                da.CH4_H2_Tatio = code.CH4_H2;
                da.CH4_H2_Code = code.CH4_H2_Code;
                da.C2H4_C2H6_Tatio = code.C2H4_C2H6;
                da.C2H4_C2H6_Code = code.C2H4_C2H6_Code;
            }
            context.SaveChanges();

        }
        public class TestChecker : IThreeCodeChecker
        {
            public DGAAlarmConfig AlarmConfig => DGAAlarmConfig.Default;

            Dictionary<string, double> gasValue = new Dictionary<string, double>
        {
            {nameof(GasName.CH4),11.03},
            {nameof(GasName.C2H2),0},
            {nameof(GasName.C2H4),2.49 },
            {nameof(GasName.C2H6),3.49 },
            {nameof(GasName.H2),38.48 },
            {nameof(GasName.O2),5 },
            {nameof(GasName.N2),6 },
            {nameof(GasName.CO),1541 },
            {nameof(GasName.CO2),9418},
            {nameof(GasName.TotHyd),17.02},
        };

            Dictionary<string, double> gasAbsValue = new Dictionary<string, double>
        {
            {nameof(GasName.CH4),1 },
            {nameof(GasName.C2H2),1},
            {nameof(GasName.C2H4),2 },
            {nameof(GasName.C2H6),3 },
            {nameof(GasName.H2),4 },
            {nameof(GasName.O2),5 },
            {nameof(GasName.N2),6 },
            {nameof(GasName.CO),7 },
            {nameof(GasName.CO2),8},
            {nameof(GasName.TotHyd),9},

        };

            public double ReadGasAbsValue(string namestr)
            {
                return gasAbsValue[namestr];
            }

            public double ReadGasValue(string namestr)
            {
                return gasValue[namestr];
            }

            public double ReadTotHyd()
            {
                return gasValue[typeof(GasName.TotHyd).Name];
            }
        }
        [Fact]
        public void TestThreeCodeIsImportant()
        {
            _log.WriteLine("important " + DGAAnalysis.ThreeCodeIsImportant(new TestChecker()));
        }
        [Fact]
        public void TestFact()
        {

            ThreeRatioCode code = new ThreeRatioCode();
            code.OnNewState(new DeviceState
            {
                Name = "H2",
                Value = "5.19"
            });
            code.OnNewState(new DeviceState
            {
                Name = "CH4",
                Value = "6.91"
            });
            code.OnNewState(new DeviceState
            {
                Name = "C2H6",
                Value = "2.07"
            });
            code.OnNewState(new DeviceState
            {
                Name = "C2H4",
                Value = "1.01"
            });
            Console.Write(code.ThreeTatio_Code);
        }
    }
}
