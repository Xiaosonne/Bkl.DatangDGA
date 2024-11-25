using System.Collections.Generic;

namespace Bkl.Models
{
    public class DeviceAnalysisContext
    {
        public BklDeviceMetadata Device { get; set; }
        public CaculateContext CaculateContext { get; set; }
    }
}
