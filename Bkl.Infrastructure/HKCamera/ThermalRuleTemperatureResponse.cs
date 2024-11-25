using System.Text.Json.Serialization;

namespace Bkl.Infrastructure
{
    public class ThermalRuleTemperatureResponse
    {
        public class ThermalRuleTemperatureList
        {
            [JsonPropertyName("ThermometryRulesTemperatureInfo")]

            public ThermalRuleTemperature[] TempRules { get; set; }
        }

        public class ThermalRuleTemperature
        {
            public int id { get; set; }
            public float maxTemperature { get; set; }
            public float minTemperature { get; set; }
            public float averageTemperature { get; set; }
            public ThermalPercentPoint MaxTemperaturePoint { get; set; }
            public ThermalPercentPoint MinTemperaturePoint { get; set; }
            public bool isFreezedata { get; set; }
        }
        [JsonPropertyName("ThermometryRulesTemperatureInfoList")]
        public ThermalRuleTemperatureList Data { get; set; }
    }
}
