using Orleans;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{
    public interface IStreamGrain :
        //INotificationPublisher,
        IGrainWithStringKey
    { 
        Task SendMessage(SrClientMessage msg);
        Task<StreamResponse> GetStreamInfo();
    }
}
