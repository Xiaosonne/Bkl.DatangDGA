namespace Bkl.Models
{
    public class CountGroupByProbeName
    {
        public string ProbeName { get; set; }
        public string Level { get; set; }
        public int Count { get; set; }
    }
    public class CountGroupByProbeNameDetail
    {
        public string groupName { get; set;  }
        public string ProbeName { get; set; }
        public string Level { get; set; }
        public int Count { get; set; }
        public string TimeGroup { get; set; }

        public long facilityId{get;set;}
        public string fullPath{get;set;}
    }
}
