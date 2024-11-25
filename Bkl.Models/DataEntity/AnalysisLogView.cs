namespace Bkl.Models
{

    public class KeyNamePair
    {
        public string key { get; set; }
        public string name { get; set; }
    }
    public class AnalysisLogView
    {
        public string DeviceName { get; set; }
        public string GroupName { get; set; }

        public string DeviceType { get; set; }
        public string CalculateResult { get; set; }

        public string FacilityName { get; set; }

        public string FacilityPosition { get; set; }

        public string FacilityDetailPosition { get; set; }
        public string Level { get; set; }
        public int RuleId { get; set; }
        public string FactoryName { get; set; }
        public int LogId { get; set; }
        public string StatusName { get; set; }
    }

    public static class PermissionConstants
    {
        public const string TargetFactory = "factory";
        public const string TargetFacility = "facility";
        public const string TargetDevice = "device";
        public const string TargetTask = "task";
    }

}
