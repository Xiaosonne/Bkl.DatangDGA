using Bkl.Models;

namespace Bkl.Dst.Interfaces
{

    public class AlarmThresholdRuleId
    {
        public long DeviceId { get; set; }
        public static implicit operator AlarmThresholdRuleId(BklDeviceMetadataRef ruleId)
        {
            return new AlarmThresholdRuleId { DeviceId = ruleId.Id };
        }
        public static implicit operator string(AlarmThresholdRuleId ruleId)
        {
            return $"devrule{ruleId.DeviceId}";
        }
        public static implicit operator AlarmThresholdRuleId(string str)
        {
            long devid = long.Parse(str.TrimStart("devrule".ToCharArray()));
            return new AlarmThresholdRuleId { DeviceId = devid };
        }
    }
    public class LinkageActionId
    {
        public long Id { get; set; }
        public LinkageActionId(long id)
        {
            Id = id;
        }

        public static implicit operator string(LinkageActionId ruleId)
        {
            return $"linkageid{ruleId.Id}";
        }
        public static implicit operator LinkageActionId(string str)
        {
            long devid = long.Parse(str.TrimStart("linkageid".ToCharArray()));
            return new LinkageActionId(devid) ;
        }
    }
}
