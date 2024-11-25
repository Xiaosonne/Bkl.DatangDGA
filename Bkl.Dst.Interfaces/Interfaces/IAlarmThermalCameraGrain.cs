using Bkl.Infrastructure;
using Bkl.Models;
using Orleans;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{

    public interface ILinkageGrain : IGrainWithStringKey
    { 
        Task SetMatchedItem(LinkageMatchedItem matchedItem);
    }
    public interface IAlarmThermalCameraGrain : IGrainWithStringKey
    {
        Task ProcessAlarm(ThermalMetryResult metryResult);
    }
}
