using System;

namespace Bkl.Models
{
    public class NVRFileInfo
    {
        public string fileName { get; set; }
        public uint fileIndex { get; set; }
        public uint fileSize { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
    }
}
