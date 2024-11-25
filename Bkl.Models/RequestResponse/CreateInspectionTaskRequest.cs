namespace Bkl.Models
{
    public class CreateInspectionTaskRequest
	{
		public long FactoryId { get; set; }
		public string FactoryName { get; set; }
		public string TaskName { get; set; }
		public string TaskType { get; set; }
		public string Description { get; set; }
	}
}
