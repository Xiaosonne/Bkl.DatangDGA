using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using static Bkl.Models.BklConfig;
using static StackExchange.Redis.Role;
using static System.Net.Mime.MediaTypeNames;

namespace TestProject2
{
    public class UnitTest1
    {
        private ITestOutputHelper _log;

        public UnitTest1(ITestOutputHelper console)
        {
            _log = console;
        }
        BklConfig.Snow snowConfig = new BklConfig.Snow
        {
            //0-63
            WorkerId = 0,
            WorkerIdBitLength = 6,
            //0-7
            DataCenterId = 0,
            DataCenterIdBitLength = 3,
            SeqBitLength = 6,
        };
        void SetSnow(Snow snowConfig)
        {
            SnowId.SetIdGenerator(new Yitter.IdGenerator.IdGeneratorOptions
            {
                WorkerId = snowConfig.WorkerId,
                DataCenterId = snowConfig.DataCenterId,
                DataCenterIdBitLength = snowConfig.DataCenterIdBitLength,
                WorkerIdBitLength = snowConfig.WorkerIdBitLength,
                SeqBitLength = snowConfig.SeqBitLength,
            });
        }
        void SetWorkerId(ushort workerId)
        {
            snowConfig.WorkerId = workerId;
            SetSnow(snowConfig);
        }
        [Fact]
        public void Dev()
        {
            //http://10.97.88.74:5000/iot/loadContext?factoryId=230720372342789&sign=8P7PDKBr1LvxDJtVCeUVhaUbIuuoefwfWq5g3gi8lnhiHnmLNfE/k65vjTwhTbUV&t=20240820103741
            var da = SecurityHelper.AESDecrypt("U/oDCTUrWmp50UKXAbvAJD87NcBOHh71e/49+HXgH8Txdz060W8M5T7s/4Dn7Dy0");
            _log.WriteLine(da);
        }
        [Fact]
        public void GenNewPassword()
        {
            _log.WriteLine("encpass " + SecurityHelper.Sha256("acdt2024"));
        }
        [Fact]
        public void SnowIdLen()
        {
            BklConfig.Snow snowConfig = new BklConfig.Snow
            {
                //0-63
                WorkerId = 0,
                WorkerIdBitLength = 6,
                //0-7
                DataCenterId = 0,
                DataCenterIdBitLength = 0,
                SeqBitLength = 6,
            };
            SnowId.SetIdGenerator(new Yitter.IdGenerator.IdGeneratorOptions());
            var id = SnowId.NextId();
            _log.WriteLine($"next {id} ");
        }
        [Fact]
        public void SnowId_max_bit_lengh_less_than_53()
        {

            var snowConfig1 = new BklConfig.Snow
            {
                //0-63
                WorkerId = 0,
                WorkerIdBitLength = 9,
                //0-7
                DataCenterId = 0,
                DataCenterIdBitLength = 0,
                SeqBitLength = 6,
            };

            SetSnow(snowConfig);
            long id = 1;
            long max = (id << 53);
            long next = SnowId.NextId();
            SetSnow(snowConfig1);
            long next1 = SnowId.NextId();
            _log.WriteLine($"next {next} {next1} max {max}");
            Assert.True(next < max, $"next id {next} > max {max}");
            Assert.True(next1 < max, $"next id {next} > max {max}");
            SetWorkerId(0);
            long w1id = SnowId.NextId();
            SetWorkerId(1);
            long w2id = SnowId.NextId();
            _log.WriteLine($"worker 1 id {w1id} worker 2 id {w2id}");
            Assert.True(w1id < max, $"next id {w1id} > max {max}");
            Assert.True(w2id < max, $"next id {w2id} > max {max}");

        }
        [Fact]
        public void Gen_100_times_same_count_equals_0()
        {
            List<long> w1lis = new List<long>();
            List<long> w2list = new List<long>();
            for (int i = 0; i <= 100; i++)
            {
                SetWorkerId(0);
                long w1id1 = SnowId.NextId();
                SetWorkerId(1);
                long w2id1 = SnowId.NextId();
                w1lis.Add(w1id1);
                w2list.Add(w2id1);
            }
            var ins = w2list.Intersect(w1lis);
            Assert.True(ins.Count() == 0, $"samecount {ins.Count()}");
        }

        [Fact]
        public void modbus_read()
        {
            //CancellationTokenSource cts = new CancellationTokenSource();
            //var task = ModbusHelper.ConnectAsync(
            //         "modbusrtu",
            //         "192.168.1.7",
            //         8234,
            //         "tcp",
            //         cts.Token);
            //var master=task.GetAwaiter().GetResult();
            //var data=master.ReadCoils(100, 20, 1);
            //Assert.True(data.Length == 1);
        }

        [Fact]
        public async void TestSetThermalBasic()
        {
            ThermalCameraISAPI thermal = new ThermalCameraISAPI("192.168.31.164", 80, "admin", "bkl666666");
            var resp = await thermal.SetThermalBasic();
            _log.WriteLine(resp.requestURL);
            _log.WriteLine(resp.statusCode.ToString());
            _log.WriteLine(resp.statusString);
            _log.WriteLine(resp.subStatusCode.ToString());
            Assert.True(resp.statusCode == 1);
        }

        [Fact]
        public async void TestSetRule()
        {
            ThermalCameraISAPI thermal = new ThermalCameraISAPI("192.168.1.64", 80, "admin", "bkl666666");
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            await foreach (var s in thermal.ReadThermalMetryAsync(cts.Token))
            {
                var resp = JsonSerializer.Deserialize<ThermalRealtimeMetryResponse>(s.ToString());
                _log.WriteLine($"Start:{s}:End");
            }
        }

        [Fact]
        public async void TestLongSetRule()
        {
            var dt = DateTime.Now;
            while (DateTime.Now.Subtract(dt).TotalSeconds < 30)
            {
                ThermalCameraISAPI thermal = new ThermalCameraISAPI("192.168.1.64", 80, "admin", "bkl666666");
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(5000);
                var resp = await thermal.ReadThermalMetryOnceAsync();

                _log.WriteLine($"Start:{resp.ToString()}:End");
            }
        }

        static string ToIP(int ip)
        {
            var bts = BitConverter.GetBytes(ip);
            return $"{bts[0]}.{bts[1]}.{bts[2]}.{bts[3]}";
        }
        static Int32 ToIP(string ip)
        {
            return BitConverter.ToInt32(ip.Split('.').Select(s => byte.Parse(s)).ToArray());
        }

        [Fact]
        public async void TestIP()
        {
            string ip = "192.168.0.123";
            string ip2 = "192.168.0.125";
            int ip1 = ToIP(ip);
            int ip21 = ToIP(ip2);
            string strip = ToIP(ip1);
            _log.WriteLine(strip);
        }



        [Fact]
        public async void TestDate()
        {
            var date = new DateTime(2023, 11, 14);
            for (var i = 0; i < 365;)
            {
                Console.WriteLine(date + "" + date.DayOfWeek);
                date = date.AddDays(31);
                i = i + 31;
            }

        }


    }
}
