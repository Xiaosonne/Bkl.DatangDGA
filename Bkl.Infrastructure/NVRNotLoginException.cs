using System;


namespace Bkl.Infrastructure
{
    public class ModbusReadWriteException:Exception
    {
        public ModbusReadWriteException(string message,Exception ex):base(message,ex)
        {
            
        }
        public string ModbusConnection { get; set; }
        public DateTime Createtime { get; set; }
        public string Method { get; set; }
    }
    public class NVRNotLoginException : Exception
    {
        private string _deviceInfo;

        public NVRNotLoginException(string deviceinfo)
        {
            _deviceInfo = deviceinfo;
        }
        public override string Message => $"{_deviceInfo} not login ";
    }
}
