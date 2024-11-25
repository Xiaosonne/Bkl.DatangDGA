namespace Bkl.Models
{
	public class CreateTaskImage
	{
		/// <summary>
		/// bucket/image_name.jpg
		/// </summary>
		public string Name { get; set; }
		public int Size { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public string Type { get; set; }
		public string Url { get; set; }
		public string CreateTime { get; set; }
	}
	public class BatchCreateELTaskDetailRequest
	{
		public long TaskId { get; set; }
		public long FactoryId { get; set; }
		//el光伏面板厂家
		public string Position { get; set; }
		public string Bucket { get; set; }
		//服务器照片临时路径
		public string Path{get;set;}
	}
	public class CreateELTaskDetailRequest
	{

		public long TaskId { get; set; }
		public long FactoryId { get; set; }
		//el光伏面板厂家
		public string Position { get; set; }
		public string Bucket { get; set; }
		public CreateTaskImage[] PictureList { get; set; }
	}
	public class CreateInspectionTaskDetailRequest
	{

		public long TaskId { get; set; }
		public string Position { get; set; }
		public long FacilityId { get; set; }
		public string FacilityName { get; set; }
		public long FactoryId { get; set; }
		public string FactoryName { get; set; }
		public string Bucket { get; set; }
		public CreateTaskImage[] PictureList { get; set; }

		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
		public int OverLap { get; set; }
	}
}
