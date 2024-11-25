using CommandLine;

namespace Bkl.Infrastructure
{
    public class SiloConfig
    {
        [Option('a', "advertise", Default = "127.0.0.1")]
        public string AdvertiseAddress { get; set; } = "127.0.0.1";
        [Option('b', "silo-port", Default = 11000)]
        public int SiloPort { get; set; } = 11000;
        [Option('c', "gateway-port", Default = 21000)]
        public int GatewayPort { get; set; } = 21000;


        [Option('d', "redis", Default = "127.0.0.1:6379,password=Etor0070x01")]
        public string Redis { get; set; } = "127.0.0.1:6379,password=Etor0070x01";
        [Option('e', "rdb", Default = 3)]
        public int Rdb { get; set; } = 3;

        [Option('f', "cluster-id", Default = "bkl")]
        public string ClusterId { get; set; } = "bkl";
        [Option('g', "server-id", Default = "esps")]
        public string ServiceId { get; set; } = "esps";
    }
}
