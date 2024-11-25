namespace Bkl.Models
{
    public class ChangeErrorResult
	{
		public long facilityId { get; set; }
		public long taskId { get; set; }
		public long taskDetailId { get; set; }
		public string position { get; set; }
		public string type { get; set; }
		public string oldInfo { get; set; }
		public string newInfo { get; set; }
		public string facilityName { get; set; }
		public string damageType { get; set; }
		public long resultId { get; set; }
		public string error { get; set; }
	}
}
