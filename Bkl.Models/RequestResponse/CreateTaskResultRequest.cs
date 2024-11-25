namespace Bkl.Models
{
    public class CreateTaskResultRequest
	{
		public long TaskId { get; set; }
		public long TaskDetailId { get; set; }

		public class TaskResult
		{
			public long Id { get; set; }
			public string DamageType { get; set; }
			public string DamageLevel { get; set; }
			public string DamageDescription { get; set; }
			public string DamagePosition { get; set; }
			public string DamageSize { get; set; }
			public string TreatmentSuggestion { get; set; }
			public string X { get; set; }
			public string Y { get; set; }
			public string Width { get; set; }
			public string Height { get; set; }
		}

		public TaskResult[] TaskResults { get; set; }
	}
}
