using Bkl.Infrastructure;
using Bkl.Models;
using System.Threading.Channels;

public interface IThermalTask
{
    Channel<ThermalMetryResult> DataChannel { get; }
    BklDeviceMetadata Device { get; }
    IThermalTask BindDevice(BklDeviceMetadata device);
    bool Login();
    IThermalTask SetConnection(string DVRIPAddress, int DVRPortNumber, string DVRUserName, string DVRPassword);
    IThermalTask Start();
    IThermalTask Stop();
}