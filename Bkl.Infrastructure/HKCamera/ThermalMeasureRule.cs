using System.Collections.Generic;

namespace Bkl.Infrastructure
{
    public class ThermalMeasureRule
    {
        public enum RuleCalibTypeEnum
        {
            dot = 0,
            line = 2,
            region = 1
        }
        public int ruleId { get; set; }

        public byte enabled { get; set; }
        public string ruleName { get; set; }
        //byRuleCalibType
        /// <summary>
        /// byRuleCalibType 0 dot 1 region 2 line
        /// </summary>
        public int regionType { get; set; }
        /// <summary>
        /// double[]:index,fX,fY
        /// </summary>
        public List<double[]> regionPoints { get; set; }
    }
}
