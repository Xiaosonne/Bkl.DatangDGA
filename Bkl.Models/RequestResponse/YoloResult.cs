using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bkl.Models
{
	public class UpdateYoloDataSet
	{
		public string DirName { get; set; }
		public string ClassName { get; set; }
		public long[] RectIds { get; set; }
	}
	public class YoloSetting
	{
		public string type { get; set; }
		public int value { get; set; }
		public int total { get; set; }
		public int choose { get; set; }
	}
	public class ExportYoloRequest
	{
		public long[] TaskIds { get; set; }
		public long[] FacilityIds { get; set; }
		public long[] FactoryIds { get; set; }
		public long[] TaskDetailIds { get; set; }

		public List<YoloSetting> YoloSetting { get; set; }
		public string DirName { get; set; }
	}
	public class BklRectInfo
	{
		public double[][] Points { get; set; }
		public string ClsName { get; set; }
		public string ClsId { get; set; }
		public long RectId { get;  set; }
	}
	public class YoloRectInfo
	{
		public string ClsId { get; set; }
		public string CenterX { get; set; }
		public string CenterY { get; set; }
		public string W { get; set; }
		public string H { get; set; }
	}
	public class CompareYoloResult : YoloResult
	{
		public CompareYoloResult(YoloResult result)
		{
			name = result.name;
			xmax = result.xmax;
			xmin = result.xmin;
			ymax = result.ymax;
			ymin = result.ymin;
			confidence = result.confidence;
			@class = result.@class;
			this.Result = result;
		}
		[JsonIgnore]
		public YoloResult Result { get; set; }
		[JsonIgnore]
		public int order { get; set; }
		[JsonIgnore] 
		public string path { get; set; }
		[JsonIgnore]
		public string pic { get; set; }
		[JsonIgnore]
		public BladeExtraInfo info { get; set; }
		[JsonIgnore] 
		public bool ok { get; set; }
	}
	public class YoloResult
	{
		public long id { get; set; }
		public int @class { get; set; }
		public float confidence { get; set; }
		public string name { get; set; }
		public float xmax { get; set; }
		public float xmin { get; set; }
		public float ymax { get; set; }
		public float ymin { get; set; }
	}

	public class AlignmentResult
	{
		public class XY
		{
			public double X { get; set; }
			public double Y { get; set; }
		}
		public string ratio { get; set; }
		public double[][] perfMat { get; set; }

		public XY[] points { get; set; }

		public string error { get; set; }
	}
}
