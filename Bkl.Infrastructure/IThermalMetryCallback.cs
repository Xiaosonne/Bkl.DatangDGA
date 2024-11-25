using Bkl.Models;

namespace Bkl.Infrastructure
{

    public interface IThermalMetryCallback
    {
        void ProcessCallback( ThermalMetryResult temp);
    }

}
