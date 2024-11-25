namespace Bkl.Models
{
    public class SetBladeRequest : BladeExtraInfo
	{
		public long FacilityId { get; set; }
		public long TaskId { get; set; }
		public long FactoryId { get; set; }
		public string BladeIndex { get; set; }
	}
}
