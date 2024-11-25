using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{

    public static class DGAGPRHelper
    {
        public static async Task<List<BklDGAGasProduction>> CaculateAGPR(BklDbContext context, BklDGAConfig config, DateTime startTime, DateTime stopTime, long deviceId, string taskId = "system")
        {
            var low = startTime.UnixEpoch();
            var high = stopTime.UnixEpoch();
            var cubic = double.Parse(config.CubicMeters);
            var days = (stopTime - startTime).TotalDays;

            var list = await context.BklDGAStatus.Where(s => s.DeviceRelId == deviceId && s.Time >= low && s.Time <= high).AsNoTracking().ToListAsync();
            List<BklDGAGasProduction> results = new List<BklDGAGasProduction>();

            if (list.Count == 0)
            {
                var device = context.BklDeviceMetadata.Where(s => s.Id == deviceId && s.DeviceType == "DGA").FirstOrDefault();
                if (device == null)
                {
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "H2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "O2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "N2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CH4", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H4", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H6", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "TotHyd", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CmbuGas", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    context.BklDGAGasProduction.AddRange(results);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                var first = list.FirstOrDefault();
                if (first != null)
                {
                    //绝对产气率
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO", Rate = (list.Sum(s => s.CO_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO2", Rate = (list.Sum(s => s.CO2_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "H2", Rate = (list.Sum(s => s.H2_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "O2", Rate = (list.Sum(s => s.O2_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "N2", Rate = (list.Sum(s => s.N2_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CH4", Rate = (list.Sum(s => s.CH4_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H2", Rate = (list.Sum(s => s.C2H2_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H4", Rate = (list.Sum(s => s.C2H4_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H6", Rate = (list.Sum(s => s.C2H6_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "TotHyd", Rate = (list.Sum(s => s.TotHyd_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CmbuGas", Rate = (list.Sum(s => s.CmbuGas_Inc) / days) * cubic, DeviceRelId = first.DeviceRelId, FacilityRelId = first.FacilityRelId, FactoryRelId = first.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "absoluteRate" });
                    results.Where(s => double.IsNaN(s.Rate) || double.IsInfinity(s.Rate)).ToList().ForEach(q => q.Rate = 0);
                    context.BklDGAGasProduction.AddRange(results);
                    await context.SaveChangesAsync();
                }
            }
            return results;
        }

        public static async Task<List<BklDGAGasProduction>> CalculateRGPR(BklDbContext context, BklDGAConfig config, DateTime startTime, DateTime stopTime, long deviceId, string taskId = "system")
        {
            var cubic = double.Parse(config.CubicMeters);
            var low = startTime.UnixEpoch();
            var high = stopTime.UnixEpoch();
            var list = await context.BklDGAStatus.Where(s => s.DeviceRelId == deviceId && s.Time >= low && s.Time <= high).AsNoTracking().ToListAsync();
            List<BklDGAGasProduction> results = new List<BklDGAGasProduction>();
            if (list.Count == 0)
            {
                var device = context.BklDeviceMetadata.Where(s => s.Id == deviceId && s.DeviceType == "DGA").FirstOrDefault();
                if (device != null)
                {
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "H2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "O2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "N2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CH4", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H2", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H4", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H6", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "TotHyd", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CmbuGas", Rate = 0, DeviceRelId = device.Id, FacilityRelId = device.FacilityId, FactoryRelId = device.FactoryId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                    context.BklDGAGasProduction.AddRange(results);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                var beginStatus = list.OrderBy(s => s.Time).First();
                var endStatus = list.OrderBy(s => s.Time).Last();
                var days = (endStatus.Time - beginStatus.Time) * 1.0 / 86400;
                if (days == 0)
                    days = double.MaxValue;
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO", Rate = list.Sum(s => s.CO_Inc) / (days * beginStatus.CO), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CO2", Rate = list.Sum(s => s.CO2_Inc) / (days * beginStatus.CO2), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "H2", Rate = list.Sum(s => s.H2_Inc) / (days * beginStatus.H2), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "O2", Rate = list.Sum(s => s.O2_Inc) / (days * beginStatus.O2), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "N2", Rate = list.Sum(s => s.N2_Inc) / (days * beginStatus.N2), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CH4", Rate = list.Sum(s => s.CH4_Inc) / (days * beginStatus.CH4), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H2", Rate = list.Sum(s => s.C2H2_Inc) / (days * beginStatus.C2H2), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H4", Rate = list.Sum(s => s.C2H4_Inc) / (days * beginStatus.C2H4), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "C2H6", Rate = list.Sum(s => s.C2H6_Inc) / (days * beginStatus.C2H6), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "TotHyd", Rate = list.Sum(s => s.TotHyd_Inc) / (days * beginStatus.TotHyd), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Add(new BklDGAGasProduction { TaskId = taskId, GasName = "CmbuGas", Rate = list.Sum(s => s.CmbuGas_Inc) / (days * beginStatus.CmbuGas), DeviceRelId = endStatus.DeviceRelId, FacilityRelId = endStatus.FacilityRelId, FactoryRelId = endStatus.FactoryRelId, Createtime = stopTime, Time = stopTime.UnixEpoch(), RateType = "relativeRate" });
                results.Where(s => double.IsNaN(s.Rate) || double.IsInfinity(s.Rate)).ToList().ForEach(q => q.Rate = 0);
                results.ForEach(item =>
                {
                    item.Rate = Convert.ToDouble(item.Rate.ToString("0.0000"));
                });
                context.BklDGAGasProduction.AddRange(results);
                await context.SaveChangesAsync();
            }

            return results;
        }
    }
}
