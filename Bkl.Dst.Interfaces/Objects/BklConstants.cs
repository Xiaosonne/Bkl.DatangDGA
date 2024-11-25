namespace Bkl.Dst.Interfaces
{
    public static class BklConstants
    {
        public const string RedisConnectionString = "127.0.0.1:6379,password=Etor0070x01";

        public const string RedisProvider = "redis1"; 


        public const int RedisClusteringDb = 3;
        public const int RedisStorageDb=10;
        public const int RedisRuleStorageDb=11;
        public const int RedisPubSubStoreDb = 12;
        public const int RedisModbusStorageDb=13;
        public const int RedisReminderDb = 3;



        public const string ClusterId = "bkl";
        public const string ServiceId = "esps";

        public const string ZookeeperConnectionString = "127.0.0.1:2181";
        public const string MySQLConnectionString= "Server=127.0.0.1;Uid=root;Pwd=bkl123...;Database=bacara;";
        public const string StreamProvider = "SMS";
    }
}
