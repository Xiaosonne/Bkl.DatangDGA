using Bkl.Models;

public interface IThreeCodeChecker
{
    DGAAlarmConfig AlarmConfig { get; }

    double ReadGasValue(string namestr);
    double ReadGasAbsValue(string namestr);
    double ReadTotHyd();
}
