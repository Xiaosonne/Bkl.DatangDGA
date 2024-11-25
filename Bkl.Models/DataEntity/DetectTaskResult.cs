namespace Bkl.Models
{
    public class DetectTaskResult
	{
		public int TaskId { get; set; }
		public int TaskDetailId { get; set; }
		public int FacilityId { get; set; }
		public int Total { get; set; }
		public int Procced { get; set; }
		public YoloResult[] Results { get; set; }
	}
}
