//using NetMQ;
//using NetMQ.Sockets;
//using System.Text;
//namespace Bkl.StreamServer.Services
//{
//    public class ZqmDemo : BackgroundService
//    {
//        private async Task sample1()
//        {
//            RouterSocket router = new RouterSocket("@tcp://127.0.0.1:5560");
//            DealerSocket dealer = new DealerSocket("@tcp://127.0.0.1:5561");

//            RequestSocket req = new RequestSocket(">tcp://127.0.0.1:5560");

//            ResponseSocket response1 = new ResponseSocket(">tcp://127.0.0.1:5561");
//            ResponseSocket response2 = new ResponseSocket(">tcp://127.0.0.1:5561");
//            ResponseSocket response3 = new ResponseSocket(">tcp://127.0.0.1:5561");
//            response1.Options.Identity = Encoding.UTF8.GetBytes("ServerA");
//            response2.Options.Identity = Encoding.UTF8.GetBytes("ServerB");
//            response3.Options.Identity = Encoding.UTF8.GetBytes("ServerC");
//            req.Options.Identity = Encoding.UTF8.GetBytes("ClientA");
//            dealer.Options.Identity = Encoding.UTF8.GetBytes("Dealer1");
//            var routerdealerpoller = new NetMQPoller
//            {
//                router,
//                dealer
//            };

//            router.ReceiveReady += delegate (object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    //var msg = e.Socket.ReceiveMultipartMessage();
//                    //dealer.SendMultipartMessage(msg);
//                    //Console.WriteLine("routerclient recv " + string.Join("#", msg.Select(s => s.ConvertToString())));

//                    //上述两种代码都是ok的
//                    var msg = e.Socket.ReceiveMultipartStrings();
//                    dealer.SendMoreFrame("ServerA")
//                    .SendFrame("dealer send:" + msg.Last());

//                    Console.WriteLine("routerclient recv::: " + string.Join("，", msg));

//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            };
//            dealer.ReceiveReady += delegate (object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    var msg = e.Socket.ReceiveMultipartMessage();
//                    router.SendMultipartMessage(msg);
//                    Console.WriteLine("dealer recv::: " + string.Join("#", msg.Select(s => s.ConvertToString())));
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            };


//            var proc = (EventHandler<NetMQSocketEventArgs>)delegate (object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    var msg = e.Socket.ReceiveMultipartStrings();
//                    var id = Encoding.UTF8.GetString(e.Socket.Options.Identity);
//                    e.Socket.SendFrame($"resp:{id} data:{DateTime.Now.ToString()} ");
//                    Console.WriteLine($"response {id} recv::: " + msg[0]);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            };
//            response1.ReceiveReady += proc;
//            response2.ReceiveReady += proc;
//            response3.ReceiveReady += proc;

//            var responsepoller = new NetMQPoller
//            {
//                response1,
//                response2,
//                response3
//            };
//            var t2 = Task.Run(responsepoller.Run);

//            var t1 = Task.Run(routerdealerpoller.Run);


//            //var proxy = new Proxy(routerclient, dealer);
//            //Task.Run(proxy.Start);


//            //responsepoller.RunAsync();
//            //routerdealerpoller.RunAsync();
//            var servers = new string[] { "A", "B", "C" };
//            while (true)
//            {
//                try
//                {
//                    var id = Guid.NewGuid().ToString();
//                    Console.WriteLine("client send:::" + id);

//                    req.SendFrame(id);
//                    var resp = req.ReceiveFrameString();
//                    Console.WriteLine("client recv::: " + string.Join("#", resp));
//                    await Task.Delay(1000);

//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }
//            }
//            await Task.Delay(1000);
//        }

//        private async Task sample2()
//        {
//            RouterSocket router = new RouterSocket("@tcp://127.0.0.1:5561");
//            RequestSocket request = new RequestSocket(">tcp://127.0.0.1:5561");

//            DealerSocket dealer = new DealerSocket("@tcp://127.0.0.1:5560");
//            ResponseSocket response1 = new ResponseSocket(">tcp://127.0.0.1:5560");
//            ResponseSocket response2 = new ResponseSocket(">tcp://127.0.0.1:5560");
//            ResponseSocket response3 = new ResponseSocket(">tcp://127.0.0.1:5560");

//            response1.Options.Identity = Encoding.UTF8.GetBytes("A");
//            response2.Options.Identity = Encoding.UTF8.GetBytes("B");
//            response3.Options.Identity = Encoding.UTF8.GetBytes("C");
//            var NetProxy = new Proxy(router, dealer);
//            var t1 = Task.Run(() =>
//            {
//                NetProxy.Start();
//            });
//            var t2 = Task.Run(async () =>
//            {
//                while (true)
//                {
//                    request.SendFrame(DateTime.Now.ToString());
//                    var msg = request.ReceiveFrameString();
//                    Console.WriteLine("request recv " + msg);
//                    await Task.Delay(1000);
//                }
//            });

//            void proc(object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    var msg = e.Socket.ReceiveFrameString();
//                    var id = e.Socket.Options.Identity == null ? "" : Encoding.UTF8.GetString(e.Socket.Options.Identity);
//                    e.Socket.SendFrame($"server-{id}-says:" + DateTime.Now.ToString());
//                    Console.WriteLine("response recv " + id + " " + msg);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            }

//            response1.ReceiveReady += proc;
//            response2.ReceiveReady += proc;
//            response3.ReceiveReady += proc;

//            var poller = new NetMQPoller
//            {
//                response1,response2,response3
//            };
//            var t3 = Task.Run(() =>
//            {
//                poller.Run();
//            });

//            await t3;
//            await t2;
//            await t1;
//        }
//        /// <summary>
//        /// 两个router 转发请求
//        /// </summary>
//        /// <returns></returns>
//        private async Task sample3()
//        {
//            RouterSocket routerclient = new RouterSocket("@tcp://127.0.0.1:5560");
//            RouterSocket routerdealer = new RouterSocket("@tcp://127.0.0.1:5561");
//            DealerSocket dealerclient1 = new DealerSocket(">tcp://127.0.0.1:5561");
//            DealerSocket dealerclient2 = new DealerSocket(">tcp://127.0.0.1:5561");
//            DealerSocket dealerclient3 = new DealerSocket(">tcp://127.0.0.1:5561");
//            dealerclient1.Options.Identity = Encoding.UTF8.GetBytes("A");
//            dealerclient2.Options.Identity = Encoding.UTF8.GetBytes("B");
//            dealerclient3.Options.Identity = Encoding.UTF8.GetBytes("C");
//            void proc(object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    var msg = e.Socket.ReceiveMultipartStrings();
//                    var id = e.Socket.Options.Identity == null ? "" : Encoding.UTF8.GetString(e.Socket.Options.Identity);
//                    e.Socket.SendFrame($"server-{id}-says:" + DateTime.Now.ToString());
//                    Console.WriteLine("server recv " + id + " " + msg.Last());
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            }
//            var reqid = "";
//            void routerclientproc(object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    var msg = e.Socket.ReceiveMultipartStrings();
//                    var id = e.Socket.Options.Identity == null ? "" : Encoding.UTF8.GetString(e.Socket.Options.Identity);
//                    if (msg.Count() == 4)
//                    {
//                        routerdealer.SendMoreFrame(msg[2]).SendFrame("server2 ");
//                        var resp1 = routerdealer.ReceiveMultipartStrings();

//                        //e.Socket.SendFrame($"server-{id}-says:" + resp1.Last());
//                        NetMQMessage msgsend = new NetMQMessage();
//                        msgsend.Append(msg[0]);
//                        msgsend.Append("");
//                        msgsend.Append(resp1.Last());
//                        e.Socket
//                            .SendMultipartMessage(msgsend);


//                        //e.Socket
//                        //.SendMoreFrame(msg[0])
//                        //.SendMoreFrameEmpty()
//                        //.SendFrame(msg.Last());
//                        //e.Socket.SendMoreFrame
//                    }

//                    Console.WriteLine("routerclient recv " + id + " " + msg);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            }

//            routerclient.ReceiveReady += routerclientproc;

//            dealerclient1.ReceiveReady += proc;
//            dealerclient2.ReceiveReady += proc;
//            dealerclient3.ReceiveReady += proc;

//            var poller = new NetMQPoller
//            {
//                dealerclient1,dealerclient2,dealerclient3
//            };
//            poller.RunAsync();
//            var poller2 = new NetMQPoller
//            {
//               routerclient
//            };
//            poller2.RunAsync();
//            RequestSocket client = new RequestSocket(">tcp://127.0.0.1:5560");
//            //client.Options.Identity = Encoding.UTF8.GetBytes("c1");
//            string[] arr = new string[] { "A", "B", "C" };
//            while (true)
//            {
//                var id = arr[DateTime.Now.Ticks % 3];
//                Console.WriteLine("it is " + id);

//                client.SendMoreFrame(id);
//                client.SendFrame(id + "'s msg " + DateTime.Now.ToString());


//                var msg = client.ReceiveMultipartStrings(2);
//                Console.WriteLine("recv " + string.Join(" ", msg));

//                //var msg = client.ReceiveMultipartMessage();
//                //Console.WriteLine("recv " + string.Join(" ", msg.Select(s => s.ConvertToString())));
//                await Task.Delay(1000);
//            }
//        }

//        /// <summary>
//        /// 两个router 转发请求
//        /// </summary>
//        /// <returns></returns>
//        private async Task sample4()
//        {
//            RouterSocket routerdealer = new RouterSocket("@tcp://127.0.0.1:5561");
//            DealerSocket dealerclient1 = new DealerSocket(">tcp://127.0.0.1:5561");
//            DealerSocket dealerclient2 = new DealerSocket(">tcp://127.0.0.1:5561");
//            DealerSocket dealerclient3 = new DealerSocket(">tcp://127.0.0.1:5561");
//            dealerclient1.Options.Identity = Encoding.UTF8.GetBytes("A");
//            dealerclient2.Options.Identity = Encoding.UTF8.GetBytes("B");
//            dealerclient3.Options.Identity = Encoding.UTF8.GetBytes("C");
//            void proc(object sender, NetMQSocketEventArgs e)
//            {
//                try
//                {
//                    var msg = e.Socket.ReceiveMultipartStrings();
//                    var id = e.Socket.Options.Identity == null ? "" : Encoding.UTF8.GetString(e.Socket.Options.Identity);
//                    e.Socket.SendFrame($"server-{id}-says:" + DateTime.Now.ToString());
//                    Console.WriteLine("server recv " + id + " " + msg.Last());
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            }
//            var reqid = "";
//            void routerclientproc(object sender, NetMQSocketEventArgs e)
//            {
//                var msg111 = e.Socket.ReceiveMultipartMessage();
//                if (msg111.Count()==4)
//                    routerdealer.SendMoreFrame(msg111[2]).SendFrame("server2 ");
//                e.Socket.SendMultipartMessage(msg111);
//                return;
//                try
//                {
//                    var msg = e.Socket.ReceiveMultipartStrings();
//                    var id = e.Socket.Options.Identity == null ? "" : Encoding.UTF8.GetString(e.Socket.Options.Identity);
//                    if (msg.Count() == 4)
//                    {
//                        routerdealer.SendMoreFrame(msg[2]).SendFrame("server2 ");
//                        var resp1 = routerdealer.ReceiveMultipartStrings();

//                        //e.Socket.SendFrame($"server-{id}-says:" + resp1.Last());
//                        //NetMQMessage msgsend = new NetMQMessage();
//                        //msgsend.Append(msg[0]); 
//                        //msgsend.Append("");
//                        //msgsend.Append(resp1.Last());
//                        //e.Socket
//                        //     .SendMultipartMessage(msgsend);

//                        routerdealer.SendMoreFrame(msg[0]).SendFrame(resp1.Last());
//                        resp1 = routerdealer.ReceiveMultipartStrings();
//                        //e.Socket
//                        //.SendMoreFrame(msg[0])
//                        //.SendMoreFrameEmpty()
//                        //.SendFrame(msg.Last());
//                        //e.Socket.SendMoreFrame
//                    }

//                    Console.WriteLine("routerclient recv " + id + " " + msg);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            }

//            routerdealer.ReceiveReady += routerclientproc;

//            dealerclient1.ReceiveReady += proc;
//            dealerclient2.ReceiveReady += proc;
//            dealerclient3.ReceiveReady += proc;

//            var poller = new NetMQPoller
//            {
//                dealerclient1,dealerclient2,dealerclient3
//            };
//            poller.RunAsync();
//            var poller2 = new NetMQPoller
//            {
//               routerdealer
//            };
//            poller2.RunAsync();
//            RequestSocket client = new RequestSocket(">tcp://127.0.0.1:5561");
//            //client.Options.Identity = Encoding.UTF8.GetBytes("c1");
//            string[] arr = new string[] { "A", "B", "C" };
//            while (true)
//            {
//                var id = arr[DateTime.Now.Ticks % 3];
//                Console.WriteLine("it is " + id);

//                client.SendMoreFrame(id);
//                client.SendFrame(id + "'s msg " + DateTime.Now.ToString());


//                var msg = client.ReceiveMultipartStrings();
//                Console.WriteLine("recv " + string.Join(" ", msg));

//                //var msg = client.ReceiveMultipartMessage();
//                //Console.WriteLine("recv " + string.Join(" ", msg.Select(s => s.ConvertToString())));
//                await Task.Delay(1000);
//            }
//        }
//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            await sample4();
//        }
//    }
//}
