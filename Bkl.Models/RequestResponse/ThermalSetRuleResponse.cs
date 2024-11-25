using Bkl.Infrastructure;
using System.Collections.Generic;

namespace Bkl.Models
{
    public class GeneralResponse
    {
        public string msg { get; set; }
        public int error { get; set; }
        public bool success { get; set; }
        public GeneralResponse()
        {
            success = true;
            error = 0;
            msg = "";
        }
    }
    public class DataResponse<T> : GeneralResponse
    {
        public T data { get; set; }
    }
    public class ThermalSetRuleResponse : GeneralResponse
    {
        public string outStatus { get; set; }
        public int ruleId { get; set; }
        public List<ThermalMeasureRule> rules { get; set; }
    }
}
