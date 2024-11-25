using System;

namespace Bkl.Models
{
    public class FuseTaskInfo
    {
        public long TaskId { get; set; }
        public long FacilityId { get; set; }
        public DateTime LastTime { get; set; }
        public string TaskType { get; set; }
        public FuseTaskInfo()
        {

        }
    }
    public class SegTaskInfo:FuseTaskInfo
    {
        public int Total { get; set; }
        public int Procced { get; set; }
        public int Error { get; set; }
        public SegTaskInfo():base()
        {

        }
        //public static explicit operator SegTaskInfo(DetectTaskInfo task)
        //{
        //    return new SegTaskInfo
        //    {
        //        TaskId = task.TaskId,
        //        FacilityId = task.FacilityId,
        //        LastTime = task.LastTime,
        //        Total = task.Total,
        //        Procced = task.Procced,
        //        Error = task.Error
        //    };
        //}
    }
    public class DetectTaskInfo : SegTaskInfo
    {
        public DetectTaskInfo():base()
        {

        }
    }
    public class ELDetectTaskInfo:DetectTaskInfo
    {

    }


}
