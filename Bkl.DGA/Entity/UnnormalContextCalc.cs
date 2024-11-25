using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Bkl.Models.DGAModel;
using stati = MathNet.Numerics.Statistics.Statistics;

public class UnnormalContextCalc : UnnormalContext
{

    public void Load(double[] values)
    {
        if (values.Length > 0)
        {
            try
            {
                var vals = values.ToList();
                var mean = stati.Mean(vals);
                var variance = stati.Variance(vals);
                var stdDev = stati.StandardDeviation(vals);
                var coefficientOfVariation = stdDev / mean;
                double p1 = 0;
                double ksTestResult = KolmogorovSmirnovTest(vals, mean, stdDev);


                this.KsTest = GoodNumber(ksTestResult, -1);
                this.Var = GoodNumber(variance, -1);
                this.Mean = GoodNumber(mean, -1);
                this.StdDev = GoodNumber(stdDev,0);
                this.CofVar = GoodNumber(coefficientOfVariation, 0);
                Console.WriteLine($"LoadData {variance} {mean} {StdDev} {CofVar}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }


    static double KolmogorovSmirnovTest(List<double> sample, double mean, double stdDev)
    {
        int n = sample.Count;

        // 标准化样本
        double[] standardizedSample = sample.Select(x => (x - mean) / stdDev).ToArray();

        // 对样本进行排序
        Array.Sort(standardizedSample);

        // 计算经验分布函数（ECDF）
        double[] ecdf = new double[n];
        for (int i = 0; i < n; i++)
        {
            ecdf[i] = (i + 1) / (double)n;
        }

        // 计算 D 统计量
        double dMax = 0.0;
        for (int i = 0; i < n; i++)
        {
            double theoreticalCdf = NormalCdf(standardizedSample[i]);
            dMax = Math.Max(dMax, Math.Abs(ecdf[i] - theoreticalCdf));
        }

        return dMax;
    }

    // 标准正态分布的累积分布函数
    static double NormalCdf(double x)
    {
        return 0.5 * (1 + SpecialFunctions.Erf(x / Math.Sqrt(2)));
    }


    double GoodNumber(double f, double defaultval)
    {
        if (double.IsNaN(f) || double.IsPositiveInfinity(f) || double.IsNegativeInfinity(f))
        {
            return defaultval;
        }
        return f;
    }

    public void Detect(double val)
    {
        try
        {
            string text = "";
            bool unnormalCofVar = false, b2 = false;
            double p1 = 0;
            //样本量10  α=0.05  d=0.483
            if (this.KsTest < 0.483)
            {
                p1 = Normal.CDF(this.Mean, this.StdDev, val);
            }
            if (double.IsNaN(this.CofVar))
            {
                this.CofVar = 0;
            }
            if (double.IsPositiveInfinity(this.CofVar))
            {
                this.CofVar = 1;
            }
            if (double.IsNegativeInfinity(this.CofVar))
            {
                this.CofVar = 0;
            }
            if (this.CofVar > 0.3)
            {
                text += $"数据波动成度较大：{this.CofVar},";
                this.UnnormalCofVar = true;
            }
            if (this.CofVar == 0)
            {
                text += $"数据没有变化";
                this.UnnormalCofVar = true;
            }
            this.Proper = Normal.CDF(this.Mean, this.StdDev, val);
            if (double.IsNaN(this.Proper))
            {
                this.Proper = 1;
            }
            if (double.IsPositiveInfinity(this.Proper))
            {
                this.Proper = 1;
            }
            if (double.IsNegativeInfinity(this.Proper))
            {
                this.Proper = 0;
            }
            if (this.KsTest < 0.483 && p1 <= 0.1)
            {
                text += $"正态分布检测数据出现概率较低：{p1}";
                this.UnnormalProper = true;
            }

            this.UnnormalTime = DateTime.UtcNow;
            this.UnnormalText = text;

            Console.WriteLine($"DetectData {this.Var} {this.Mean} {StdDev} UnnormalCofVar:{this.UnnormalCofVar} CofVar:{CofVar} UnnormalProper:{UnnormalProper} Proper:{Proper}  {text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }


    }

}
