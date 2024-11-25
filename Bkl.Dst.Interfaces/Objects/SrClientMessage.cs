using System;

namespace Bkl.Dst.Interfaces
{
    public class StreamResponse
    {
        public string StreamNamespace { get; set; }

        public Guid StreamId { get; set; }
    }
    public class SrJoinGroupRequest
    {
        public string ConnectionId { get; set; }

        public long DeviceId { get; set; }

        public string ClientMethod { get; set; }
        public string GroupId { get; set; }


        public override string ToString()
        {
            return $"{this.ClientMethod}/{this.GroupId}/{this.DeviceId}/{this.ConnectionId}";
        }
        public static SrJoinGroupRequest Parse(string subinfo)
        {
            var arr = subinfo.Split('/');
            return new SrJoinGroupRequest
            {
                ClientMethod = arr[0],
                GroupId = arr[1],
                DeviceId = long.Parse(arr[2]),
                ConnectionId = arr[3]
            };
        }
    }
   
    public class SrUnicastMessage
    {
        public string ClientCallbackMethod { get; set; }
        public string GroupId { get; set; }
        public object Data { get; set; }
    }
    public class SrClientMessage
    {
        public long DeviceId { get; set; } 

        public string Data { get; set; }
        public string DataType { get; set; }
        public string MessageType { get; set; }

    }
    public class StreamMessage
    {
        public string From { get; set; }
        public SrClientMessage Message { get; set; }
    }
}
