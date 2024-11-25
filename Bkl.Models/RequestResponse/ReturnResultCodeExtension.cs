using System.ComponentModel;

namespace Bkl.Models
{
    public static class ReturnResultCodeExtension
    {
        public enum ReturnResultCode
        {

            [Description("Success")]
            Success = 1000,
            [Description("未找到该用户")]
            UserNotFound = 1001,
        }
        public static GeneralResponse ToReturnObject(this ReturnResultCode retcode)
        {
            return new  GeneralResponse { error = (int)retcode, msg = retcode.ToString() };
        }
    }
}
