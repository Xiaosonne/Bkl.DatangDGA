namespace Bkl.Models
{
    public class CreateFactoryRequest
    {
        public class SelectPCA
        {
            public string Type { get; set; }
            public string Value { get; set; }
            public int NodeId { get; set; }
        }
        public class KVPair{
            public string Name{get;set;}
            public string Value{get;set;}
        }
        public SelectPCA[] Pcas { get; set; }
        public string FactoryName { get; set; }
        public string DetailAddress { get; set; }
        public KVPair[] Metadata{get;set;}
        public long FactoryId { get; set; }
    }
}
