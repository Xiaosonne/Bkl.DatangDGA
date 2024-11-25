using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Makaretu.Dns;
using Microsoft.Extensions.DependencyInjection;

namespace Bkl.Infrastructure
{
    public static class MDNSHelper
    {
        public class ApplicationProfile
        {
         public   string applicationType;
            public string applicationName;
            public string transportType;
            public ushort applicationPort;
            public ApplicationProfile(string appName, string appType,ushort appPort, string transType="_tcp")
            {
                applicationName = appName;
                applicationType = appType;
                transportType = transType;
                applicationPort=appPort;
            }
            public void Process(MessageEventArgs e, Question que)
            {
                //Console.WriteLine(que.Type + " " + e.RemoteEndPoint + " " + e.Message.ToString());
                switch (que.Type)
                {
                    case DnsType.PTR:
                        OnPTR(e, que, applicationName, applicationType, transportType, HostName);
                        break;
                    case DnsType.SRV:
                        OnSRV(e, que, applicationName, applicationType, transportType,applicationPort, HostName);
                        break;
                    case DnsType.TXT:
                        OnTXT(e, que, applicationName, applicationType, transportType, HostName);
                        break;
                    case DnsType.A:
                        OnA(e, que, applicationName, applicationType, transportType, HostName);
                        break;
                    case DnsType.AAAA:
                        OnAAAA(e, que, applicationName, applicationType, transportType, HostName);
                        break;
                    case DnsType.ANY:
                        break;
                }
            }

        }

        static void OnPTR(MessageEventArgs e, Question que, string applicationName, string applicationType, string transportType, string host)
        {
            if ((que.Name == "_services._dns-sd._udp.local") && que.Type == DnsType.PTR)
            {
                var resp = e.Message.CreateResponse();
                resp.Answers.Add(new PTRRecord
                {
                    Name = que.Name,
                    DomainName = $"{applicationType}.{transportType}"
                });
                _mdns.SendAnswer(resp);
            }
            if ((que.Name == $"{applicationType}.{transportType}.local") && que.Type == DnsType.PTR)
            {
                var resp = e.Message.CreateResponse();
                resp.Answers.Add(new PTRRecord()
                {
                    Name = $"{applicationType}.{transportType}.local",
                    DomainName = $"{applicationName}.{applicationType}.{transportType}.local"
                });
                _mdns.SendAnswer(resp);
            }
        }
        static void OnSRV(MessageEventArgs args, Question que, string applicationName, string applicationType, string transportType,ushort port, string HostName)
        {
            if ((que.Name == $"{applicationName}.{applicationType}.{transportType}.local") && que.Type == DnsType.SRV)
            {
                Console.WriteLine(que.Type + " " + que.Name);
                var resp = args.Message.CreateResponse();
                resp.Answers.Add(new SRVRecord()
                {
                    Name = que.Name,
                    Port = port,
                    Target = HostName
                });

                _mdns.SendAnswer(resp);
            }
        }
        static void OnTXT(MessageEventArgs args, Question que, string applicationName, string applicationType, string transportType, string HostName)
        {
            if ((que.Name == $"{applicationName}.{applicationType}.{transportType}.local") && que.Type == DnsType.TXT)
            {
                var resp = args.Message.CreateResponse();
                resp.Answers.Add(new TXTRecord()
                {
                    Name = que.Name,
                    Strings = new string[] { "TxtResponse" }.ToList()
                });
                _mdns.SendAnswer(resp);
                //sd.Advertise(new ServiceProfile("_kafka", "_bcr-srv._tcp", 9092){HostName=hostDomain}); 
            }
        }
        static void OnA(MessageEventArgs args, Question que, string applicationName, string applicationType, string transportType, string HostName)
        {
            if ((que.Name == HostName) && que.Type == DnsType.A)
            {
                var resp = args.Message.CreateResponse();
                foreach (var addr in MulticastService.GetIPAddresses())
                {
                    Console.WriteLine($"response IP address {addr}");
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        resp.Answers.Add(new ARecord()
                        {
                            Name = HostName,
                            Address = IPAddress.Parse(addr.ToString()),
                        });
                    }
                }
                _mdns.SendAnswer(resp);
            }
        }
        static void OnAAAA(MessageEventArgs args, Question que, string applicationName, string applicationType, string transportType, string HostName)
        {

        }
        static MulticastService _mdns;
        static ServiceDiscovery _serviceDiscovery;
        static List<ApplicationProfile> _profiles;
        static MDNSHelper()
        {
            _mdns = new MulticastService();
            _serviceDiscovery = new ServiceDiscovery(_mdns);
            _profiles = new List<ApplicationProfile>(); 
            _mdns.QueryReceived += (s, e) =>
            {
                e.Message.Questions.ForEach(que => _profiles.ForEach(profile => profile.Process(e, que)));
            };
            _runAction = async () =>
            {
                await Task.Delay(5000);
                _mdns.SendQuery("_tcp.local");
                _queryTask = Task.Run(_runAction);
            };
        }
        static Action _runAction;
        private static Task _queryTask;

        public static string HostName { get; set; } = $"bkl-server-{Dns.GetHostName()}.local";

        public static void AddApplicationProfile(ApplicationProfile profile)
        {
            _profiles.Add(profile);
        }

        public static void Run()
        { 
            _mdns.Start();
            _queryTask= Task.Run(_runAction);
           
        }

        public static void AddMDNS(this IServiceCollection services,ApplicationProfile profile){
            MDNSHelper.Run();
            MDNSHelper.AddApplicationProfile(profile);
        }
    }
}
