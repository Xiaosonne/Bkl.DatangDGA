namespace Bkl.Models
{
    public class CreateHttpProtoRequest
    {
        public class JsonAttribute : RequestHttpRequest
        {
            public string StatusName { get; set; }
            public string StatusNameCN { get; set; }
            public string UnitCN { get; set; }
            public string Unit { get; set; }
            public string DataType { get; set; }
            public string Scale { get; set; }
        }
        public JsonAttribute[] Attributes { get; set; }
        public string ProtoName { get; set; }
    }
}
