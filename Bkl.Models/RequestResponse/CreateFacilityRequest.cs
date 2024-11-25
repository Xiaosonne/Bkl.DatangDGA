namespace Bkl.Models
{
    public class CreateFacilityRequest
    {
        public class FacilityKeyValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        public long FactoryId { get; set; }
        public string FactoryName { get; set; }
        public string FacilityName { get; set; }
        public string FacilityType { get; set; }
        public string GPS { get; set; }
        public FacilityKeyValue[] Metadata { get; set; }
    }
}
