using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bkl.Models.LocalContext
{
    public class BklLocalYoloDataSet
    {
        [Key]
        public long Id { get; set; }
        public long RectId { get; set; }
        public string Path { get; set; }
        public string DirName { get; set; }
        public string ClassName { get; set; }
        public long FacilityId { get; set; }
        public long FactoryId { get; set; }
        public long TaskId { get; set; }
        public long TaskDetailId { get; set; }
        public string RawPoints { get; set; }
        public string YoloPoints { get; set; }
       
    }
    public class BklLocalYoloPath
    {
        [Key]
        public string DirName { get; set; }
        public string YoloSetting { get; set; }
        public string ClassStatistic { get; set; }
    }
}
