using Bkl.Models;

public interface IThreeCodeChecker
{
    DGAAlarmConfig AlarmConfig { get; }
    string DeviceName { get; }

    double ReadGasValue(string namestr);
    double ReadGasAbsValue(string namestr);
    double ReadTotHyd();
}
