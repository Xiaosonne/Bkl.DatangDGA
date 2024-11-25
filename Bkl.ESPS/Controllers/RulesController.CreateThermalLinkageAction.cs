namespace Bkl.ESPS.Controllers
{
    public partial class RulesController
    {
        public class CreateThermalLinkageAction
        {
            public string ExtraInfo { get; set; }

            public string Name { get; set; }
            public string TargetDevice { get; set; }
            public long DeviceId { get; set; }
            public string DeviceType { get; set; }
            public int Level { get; set; }
            public string AreaName { get; set; }
            public int AreaId { get; set; }
            public int Max { get; set; }
            public int Min { get; set; }
            public string Method { get; set; }

            public ThermalActions[] Actions { get; set; }
         

            public class ThermalActions
            {
                public string ConnUuid { get; set; }
                public long ConnectionId { get; set; }
                public long Id { get; set; }
                public string SelectKey { get; set; }
                public string SelectValue { get; set; }
                public string StatusName { get; set; }
                public string StatusNameCN { get; set; }
                public string ProtocolName { get; set; }
                public long PairId { get; set; }
                public int ReadType { get; set; }
            }
        }
    }
}
