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

        [Fact]
        public void AESTest()
        {
            var str = SecurityHelper.AESDecrypt("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");
            _log.WriteLine(str);

        }
    }
}
