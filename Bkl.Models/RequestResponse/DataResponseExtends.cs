namespace Bkl.Models
{
    public static class DataResponseExtends{
    public static DataResponse<T> CreateResponse<T>(this T obj,int error=0,string msg = "")
        {
            return new DataResponse<T> { data = obj, error = error, msg = msg };
        }
    }
}
