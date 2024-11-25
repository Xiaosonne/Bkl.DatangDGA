namespace Bkl.Infrastructure
{


    public enum NVRFileType
    {
        全部 = 0xff,
        定时录像 = 0,
        移动侦测 = 1,
        报警触发 = 2,
        报警或动测 = 3,
        报警和动测 = 4,
        命令触发 = 5,
        手动录像 = 6,
        智能录像 = 7,
    }
    public enum EnumErrorCodeNVR
    {
        NET_DVR_NOERROR = 0,// 没有错误。 
        NET_DVR_NOENOUGHPRI = 2,// 权限不足。该注册用户没有权限执行当前对设备的操作，可以与远程用户参数配置做对比。 
        NET_DVR_NOINIT = 3,// SDK未初始化。 
        NET_DVR_CHANNEL_ERROR = 4,// 通道号错误。设备没有对应的通道号。 
        NET_DVR_NETWORK_FAIL_CONNECT = 7,// 连接设备失败。设备不在线或网络原因引起的连接超时等。 
        NET_DVR_NETWORK_SEND_ERROR = 8,// 向设备发送失败。 
        NET_DVR_NETWORK_RECV_ERROR = 9,// 从设备接收数据失败。 
        NET_DVR_NETWORK_RECV_TIMEOUT = 10,// 从设备接收数据超时。 
        NET_DVR_ORDER_ERROR = 12,// 调用次序错误。 
        NET_DVR_PARAMETER_ERROR = 17,// 参数错误。SDK接口中给入的输入或输出参数为空。 
        NET_DVR_NODISK = 19,// 设备无硬盘。当设备无硬盘时，对设备的录像文件、硬盘配置等操作失败。 
        NET_DVR_NOSUPPORT = 23,// 设备不支持。 
        NET_DVR_ALLOC_RESOURCE_ERROR = 41,// SDK资源分配错误。 
        NET_DVR_NOENOUGH_BUF = 43,// 缓冲区太小。接收设备数据的缓冲区或存放图片缓冲区不足。 
        NET_DVR_CREATESOCKET_ERROR = 44,// 创建SOCKET出错。 
        NET_DVR_USERNOTEXIST = 47,// 用户不存在。注册的用户ID已注销或不可用。 
        NET_DVR_BINDSOCKET_ERROR = 72,// 绑定套接字失败。 
        NET_DVR_SOCKETCLOSE_ERROR = 73,// socket连接中断，此错误通常是由于连接中断或目的地不可达。 
    } 


   
}
