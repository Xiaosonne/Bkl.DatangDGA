using Bkl.Infrastructure;
using Bkl.Models;
using Orleans;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{

    public interface IThermalCameraGrain : IGrainWithStringKey
    {
        Task WeakUp();
        Task<DataResponse<Dictionary<int, ThermalMetryResult>>> GetStatus();
        Task<DataResponse<List<ThermalMeasureRule>>> GetMeasureRules();
        Task<ThermalSetRuleResponse> UpdateRule(ThermalMeasureRule rule);
        Task<DataResponse<List<ThermalXYTemperature>>> GetThermalJPEG(int x, int y, int padding);

    }
}
