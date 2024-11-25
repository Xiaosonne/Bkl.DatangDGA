using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bkl.Models
{
    public class CaculateContext
    {
        public List<BklAnalysisRule> Rules { get; set; }

        public LinkedList<DeviceCalculateStatus> Status { get; set; }

        /// <summary>
        /// 计算窗口大小 毫秒数 
        /// </summary>
        public int WindowSize { get; set; }
        /// <summary>
        /// 滚动窗口大小 毫秒数 
        /// </summary>
        public int RollingSize { get; set; }
        public string Method { get; set; }

        public void OnNewStatus(DeviceCalculateStatus deviceStatus)
        {
            Status.AddLast(deviceStatus);
        }
        public struct AnalysisResult
        {
            public static AnalysisResult False = new AnalysisResult { success = false,msg="calculate window to small" };
            public static AnalysisResult CalculateWindowTooSmall = new AnalysisResult { success = false, msg =nameof(CalculateWindowTooSmall) };
            public static AnalysisResult RollingWindowTooSmall = new AnalysisResult { success = false, msg = nameof(RollingWindowTooSmall) };

            public double calcResult;
            public bool success;
            public DateTime timeMin;
            public DateTime timeMax;
            public long fromOffset;
            public long toOffset;
            public string msg;
        }
        /// <summary>
        /// 保证WindowSize毫秒内的所有数据均收到 计算该WindowSize内的所有数据 并从WindowSize开始处滚动RollingSize大小
        /// </summary>
        /// <returns></returns>
        public AnalysisResult DoAnalysis()
        {
            //var now = DateTime.Now;
            //RollingWindow(now);
            int count = Status.Count;
            var toCalculate = new List<DeviceCalculateStatus>();
            var first = Status.First;
            var loop = Status.First;
            toCalculate.Add(first.Value);
            //小于等于计算窗口的序列的 加入到计算列表内
            while (loop.Next != null && (loop.Next.Value.Time - first.Value.Time).TotalMilliseconds < WindowSize)
            {
                loop = loop.Next;
                toCalculate.Add(loop.Value);
            }
            //保证完整接收到整个窗口内的数据
            if (loop.Next == null)
            {
                Console.WriteLine($"firstTime:{first.Value.Time} loopTime:{loop.Value.Time} ts:{(loop.Value.Time - first.Value.Time).TotalMilliseconds}  wz:{WindowSize}");
                return AnalysisResult.CalculateWindowTooSmall;
            }
            first = loop.Next;
            loop = loop.Next;
            DateTime maxTime=DateTime.MinValue;
            while (loop != null && (loop.Value.Time - first.Value.Time).TotalMilliseconds < RollingSize)
            {
                maxTime = loop.Value.Time;
                loop = loop.Next;
            }
            if (loop == null)
            {
                Console.WriteLine($"firstTime:{first.Value.Time} loopTime:{maxTime} ts:{(maxTime - first.Value.Time).TotalMilliseconds}  rz:{RollingSize}");
                return AnalysisResult.RollingWindowTooSmall;
            }
            //向前滚动RollingSize毫秒的窗口 
            first = Status.First;
            while (Status.First != null && (Status.First.Value.Time - first.Value.Time).TotalMilliseconds <= RollingSize)
            {
                //Console.WriteLine($"rolling time {first.Value.Time} {Status.First.Value.Time} {Status.First.Value.SequenceId}");
                Status.RemoveFirst();
            }
            //var lis = Status.Take(count).ToList();
            var time = toCalculate.OrderBy(q => q.Time).FirstOrDefault().Time;
            var timeMax = toCalculate.OrderByDescending(q => q.Time).FirstOrDefault().Time;
            double calcValue = 0;
            switch (Method)
            {
                case "max":
                    calcValue = toCalculate.Max(p => p.Value);
                    break;
                case "min":
                    calcValue = toCalculate.Min(p => p.Value);
                    break;
                case "average":
                    calcValue = toCalculate.Average(p => p.Value);
                    break;
                default:
                    break;
            }
            return new AnalysisResult
            {
                fromOffset = first.Value.SequenceId,
                toOffset = toCalculate.Last().SequenceId,
                calcResult =
                calcValue,
                success = true,
                timeMin = time,
                timeMax = timeMax
            };

            //Console.WriteLine($"calcResult {Status.First?.Value.SequenceId} {Rule.Id} {calcValue} {time} {timeMax} ");
            //if (calcValue > double.Parse(Rule.Min) && calcValue <= double.Parse(Rule.Max))
            //{
            //}
            //return AnalysisResult.False;
        }
        //private void RollingWindow(DateTime now)
        //{
        //    var earlist = now.Subtract(TimeSpan.FromMilliseconds(MaxCalcInterval));
        //    while (true)
        //    {
        //        if (Status.TryPeek(out var status) && status.Time < earlist)
        //        {
        //            Status.TryDequeue(out status);
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //}
    }
}
