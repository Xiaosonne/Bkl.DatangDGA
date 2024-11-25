using Bkl.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace TestProject2
{
    public class UdpTest
    {
        private ITestOutputHelper _log;

        public UdpTest(ITestOutputHelper console)
        {
            _log = console;
        }
        [Fact]
        public void ReadUdpRTU()
        {
            UdpClientMaster udp = new UdpClientMaster();
            var task = udp.ConnectAsync(IPAddress.Parse("127.0.0.1"), 3323, System.Threading.CancellationToken.None);
            var master = task.GetAwaiter().GetResult();
            var data = master.ReadHoldingRegisters(1, 0, 10);
            _log.WriteLine(string.Join("->", data));
        }
        [Fact]
        public void LHOrder()
        {
            ushort aa = 0x1234;
            var bts = BitConverter.GetBytes(aa);
            //默认大端在前 
            _log.WriteLine(bts[0].ToString("X"));//0x34
            _log.WriteLine(bts[1].ToString("X"));//0x12

            byte[] buf = new byte[] { 0x45, 0xf8, 0x23, 0x5a };
            var b1 = BitConverter.ToSingle(buf, 0);
            var b2 = BitConverter.ToSingle(buf.Reverse().ToArray(), 0);
            var little = BitConverter.IsLittleEndian;
        }
    }
}
