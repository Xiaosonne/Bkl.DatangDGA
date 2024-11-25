namespace Bkl.Models
{
    public class RequestHttpRequest{
        public string Url{get;set;}
        public HttpHeader[] Headers{get;set;}
        public class HttpHeader{
            public string Name{get;set;}
            public string Value{get;set;}
        }
        public string Method{get;set;}
    }
}
