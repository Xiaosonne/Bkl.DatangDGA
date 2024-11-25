// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    public class BklDeviceMetadataRef
    {
        public long Id { get; set; }
        public string FullPath { get; set; }
        public long FactoryId { get; set; }
        public long FacilityId { get; set; }
        public long CreatorId { get; set; }

        public static implicit operator BklDeviceMetadataRef(BklDeviceMetadata dev)
        {
            return new BklDeviceMetadataRef
            {
                Id = dev.Id,
                FullPath = dev.FullPath,
                FactoryId = dev.FactoryId,
                FacilityId = dev.FacilityId,
                CreatorId = dev.CreatorId,
            };
        }
        public static implicit operator BklDeviceMetadata(BklDeviceMetadataRef dev)
        {
            return new BklDeviceMetadata
            {
                Id = dev.Id,
                FullPath = dev.FullPath,
                FactoryId = dev.FactoryId,
                FacilityId = dev.FacilityId,
                CreatorId = dev.CreatorId,
            };
        }
    }
}
