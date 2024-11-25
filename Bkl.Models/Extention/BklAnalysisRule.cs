using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Bkl.Models
{
    public struct DeviceCalculateStatus
    {
        public DateTime Time;
        public double Value;

        public long SequenceId { get; set; }
    }
    public enum MatchRuleLevel
    {
        Normal = 40,
        Warn = 50,
        Error = 60
    }
    public enum MatchRuleLevelCN
    {
        正常 = 40,
        预警 = 50,
        异常 = 60
    }
    public partial class BklAnalysisRule
    {
        public void UpdateFrom(BklAnalysisRule newrule)
        {
            this.ProbeName = newrule.ProbeName;
            this.DeviceId = newrule.DeviceId;
            this.RuleName = newrule.RuleName;
            this.DeviceType = newrule.DeviceType;
            this.AttributeId = newrule.AttributeId;
            this.StartTime = newrule.StartTime;
            this.EndTime = newrule.EndTime;
            this.TimeType = newrule.TimeType;
            this.Min = newrule.Min;
            this.Max = newrule.Max;
            this.Level = newrule.Level;
            //this.FactoryId = newrule.FactoryId;
            //this.CreatorId = newrule.CreatorId;

        }
        //public static async Task ProcessAnalysis(IServiceProvider serviceProvider, ConcurrentDictionary<int, DeviceAnalysisContext> calcContexts)
        //{
        //    var context = serviceProvider.GetService<BklDbContext>();
        //    while (true)
        //    {
        //        var count = calcContexts.Count;
        //        int i = 0;
        //        while (i < count)
        //        {
        //            try
        //            {
        //                Analysis(calcContexts.ElementAt(i).Value, context);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.ToString());
        //            }
        //            i++;
        //        }
        //    }
        //}


    }
}
