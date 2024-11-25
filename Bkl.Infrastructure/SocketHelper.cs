using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bkl.Infrastructure
{
    public class SocketHelper
    {
        public static async Task<Socket> TcpConnectAsync(IPAddress ip, int port, CancellationToken token)
        {

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            SocketAsyncEventArgs saea = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = new IPEndPoint(ip, port),
                DisconnectReuseSocket = false,
            };

            bool conn = false;
            bool onConnecting = false;
            bool onConnectedOver = false;
            EventHandler<SocketAsyncEventArgs> completed = new EventHandler<SocketAsyncEventArgs>(new Action<object, SocketAsyncEventArgs>((sender, e) =>
            {
                Console.WriteLine($"{e.LastOperation} {DateTime.Now} {e.SocketError} {e.RemoteEndPoint}");
                onConnectedOver = true;
                if (e.LastOperation == SocketAsyncOperation.Connect && e.SocketError == SocketError.Success)
                {
                    conn = true;
                }
            }));
            saea.Completed += completed;

            TaskCompletionSource<Socket> source = new TaskCompletionSource<Socket>();

            if (false == socket.ConnectAsync(saea))
            {
                onConnecting = false;
                conn = socket.Connected;
                if (socket.Connected)
                {
                    return socket;
                }
            }
            while (token.IsCancellationRequested == false)
            {
                await Task.Delay(10);
                if (conn)
                {
                    return socket;
                }
            }
            if (!conn)
            {
                Console.WriteLine($"Socket Connection Failed {ip} {port}");
                try { socket.Close(); }
                catch { }
                try { socket.Dispose(); }
                catch { }
                socket = null;
            }

            return socket;

            //while (!conn)
            //{
            //    if (token.IsCancellationRequested)
            //    {
            //        if (socket != null)
            //        {
            //            socket.Close();
            //            socket.Dispose();
            //        }
            //        break;
            //    }
            //    if (onConnectedOver)
            //    {
            //        try
            //        {
            //            socket.Send(new byte[0], 0, SocketFlags.None);
            //            conn = true;
            //        }
            //        catch
            //        {
            //            conn = false;
            //            onConnecting = false;
            //            onConnectedOver = false;
            //            socket.Close();
            //            socket.Dispose();
            //            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //        }
            //    }
            //    if (!onConnecting)
            //    {
            //        onConnecting = true;
            //        if (false == socket.ConnectAsync(saea))
            //        {
            //            onConnecting = false;
            //            conn = socket.Connected;
            //            if (!conn)
            //            {
            //                onConnectedOver = false;

            //            }
            //        }
            //    }
            //    if (onConnecting)
            //        await Task.Delay(50);
            //}
            //if (!conn)
            //{
            //    socket.Close();
            //    socket.Dispose();
            //    saea.Dispose();
            //    return null;
            //}

            //saea.Dispose();
            //return socket;
        }

    }
}
