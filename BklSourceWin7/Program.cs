using FreeSql;
using FreeSql.DataAnnotations;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace BklSourceWin7
{

    public class GasData
    {
        public double H2 { get; set; }
        public double CO { get; set; }
        public double CO2 { get; set; }
        public double CH4 { get; set; }
        public double C2H2 { get; set; }
        public double C2H4 { get; set; }
        public double C2H6 { get; set; }
        public double TotHyd { get; set; }
        public double Mst { get; set; }
        public double O2 { get; set; }
        public double N2 { get; set; }
        public double CmbuGas { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {

            StateSourceService service = new StateSourceService();
            CancellationTokenSource cts = new CancellationTokenSource();

            service.ExecuteAsync(cts.Token);
        }
    }
}
