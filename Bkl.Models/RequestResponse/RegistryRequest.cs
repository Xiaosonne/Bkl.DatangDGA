namespace Bkl.Models
{
    public class RegistryRequest
    {
        public string account { get; set; }
        public string password { get; set; }

        public string username { get; set; }
        public string phone { get; set; }

        public long factoryId { get; set; }

        public string roles { get; set; }
        public string positions { get; set; }

    }
}
