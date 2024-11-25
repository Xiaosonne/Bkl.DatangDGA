namespace Bkl.Infrastructure
{
    public class ThermalRealtimeMetryResponse
    {
        public class Thermometryuploadlist
        {
            public Thermometryupload[] ThermometryUpload { get; set; }
        }

        public class Thermometryupload
        {
            public int relativeTime { get; set; }
            public int absTime { get; set; }
            public string ruleName { get; set; }
            public int ruleID { get; set; }
            public int ruleCalibType { get; set; }
            public int presetNo { get; set; }
            public Linepolygonthermcfg LinePolygonThermCfg { get; set; }
            public int thermometryUnit { get; set; }
            public int dataType { get; set; }
            public bool isFreezedata { get; set; }
            public ThermalPercentPoint HighestPoint { get; set; }
            public ThermalPercentPoint LowestPoint { get; set; }
            public long timestamp { get; set; }
            public Pointthermcfg PointThermCfg { get; set; }
        }

        public class Linepolygonthermcfg
        {
            public float MaxTemperature { get; set; }
            public float MinTemperature { get; set; }
            public float AverageTemperature { get; set; }
            public float TemperatureDiff { get; set; }
            public Region[] Region { get; set; }
        }

        public class Region
        {
            public ThermalPercentPoint Point { get; set; }
        }



        public class Pointthermcfg
        {
            public float temperature { get; set; }
            public ThermalPercentPoint Point { get; set; }
        }
        public Thermometryuploadlist ThermometryUploadList { get; set; }
    }
}
