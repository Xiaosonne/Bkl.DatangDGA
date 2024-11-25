namespace Bkl.ESPS.Controllers
{
    public partial class RulesController
    {
        public class CreateLinkageActions
        {

            public class AnalysisRule
            {
                public string name { get; set; }

                public long linkageActionId { get; set; }
                public long deviceId { get; set; }
                public long attributeId { get; set; }
                public string statusName { get; set; }
                public string statusNameCN { get; set; }
                public int level { get; set; }
                public string key { get; set; }
                public string deviceType { get; set; }
                public string extraInfo { get; set; }
                public string method { get; set; }
                public long pairId { get; set; }
            }
            public class LinkageAction
            {
                public long AttributeId { get; set; }
                public long PairId { get; set; }
                public string ConnectionUuid { get; set; }
                public long LinkageActionId { get; set; }
                public string Value { get; set; }
                public string ValueCN { get; set; }
                public int Sleep { get; set; }
                public int Order { get; set; }
            }

            public LinkageAction[] Actions { get; set; }

            public AnalysisRule[] Rules { get; set; }
        }
    }
}
