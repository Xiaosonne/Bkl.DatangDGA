using Bkl.Models;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NetMQ.Sockets;
using NetMQ;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.Net.Http;
using Bkl.Infrastructure;
using Newtonsoft.Json;
using System.IO;
using System.Globalization;
using CliWrap;

public static class TimeExtension
{
    public static int GetDayOfWeek(this DateTime now)
    {
        return now.DayOfWeek == System.DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
    }
    public static int WeekOfYear(this DateTime now)
    {
        CultureInfo ci = new CultureInfo("zh-CN");
        System.Globalization.Calendar cal = ci.Calendar;
        CalendarWeekRule cwr = ci.DateTimeFormat.CalendarWeekRule;
        DayOfWeek dow = DayOfWeek.Monday;
        int week = cal.GetWeekOfYear(now, cwr, dow);
        return week;
    }

    static DateTime unixExpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static int UnixEpoch(this DateTime time)
    {
        return (int)time.ToUniversalTime().Subtract(unixExpoch).TotalSeconds;
    }
    public static DateTime UnixEpochBack(this long time)
    {
        return unixExpoch.AddSeconds(time).ToLocalTime();
    }
    public static DateTime UnixEpochBack(this int time)
    {
        return unixExpoch.AddSeconds(time).ToLocalTime();
    }
}


public class Log
{
    public void LogInformation(string str) { }
    public void LogError(string str) { }
    public void LogDebug(string str) { }

}

public class DgaReadingContext
{
    public BklDeviceMetadata Device { get; set; }
    public ModbusConnInfo Connection { get; set; }
    public List<ModbusDevicePair> Pairs { get; set; }
    public List<ModbusNodeInfo> Nodes { get; set; }
}


public class StateSourceService
{
    private NameValueCollection _config;
    private IServiceProvider _serviceProvider;
    public class ExecCmd
    {
        public string Exec { get; set; }
        public string WorkingDir { get; set; }
        public string[] Verbs { get; set; }
        public long TaskId { get; set; }
    }
    private Log _logger = new Log();
    public StateSourceService(
//IServiceProvider serviceProvider
)
    {
        _config = ConfigurationManager.AppSettings; ;
        //_serviceProvider = serviceProvider;
    }

    DateTime _lastRefresh = DateTime.MinValue;
    DateTime _lastTask = DateTime.MinValue;
    DateTime _lastHeartBeat = DateTime.MinValue;
    Dictionary<string, IReadTask> modbusList = new Dictionary<string, IReadTask>();

    HttpClient http = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
    DgaReadingContext[] contexts = null;

    public void ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep (1000);

            var heartBeat = TryCatchExtention.TryCatch(() => int.TryParse(_config.Get("DGA:HeartBeatInterval"), out var a) ? a : 30, 30);

            var factoryId = TryCatchExtention.TryCatch(() => double.TryParse(_config.Get("AppSetting:FactoryId"), out var a) ? a : 0, 0);

            var contextServer = _config.Get("AppSetting:ContextServer");

            ExecHeartBeat(contexts, heartBeat, factoryId);

            ExecClientTask(http, factoryId, contextServer);

            ExecLoadContext(factoryId, contextServer);

            ExecRead();
        }
    }

    private void ExecLoadContext(double factoryId, string contextServer)
    {
        var refreshInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.Get("DGA:RefreshInterval"), out var a) ? a : 30, 30);
        var overrideIP = _config.Get("overrideIPEndPoint");
        var overrideModbus = _config.Get("overrideModbusType");
        //Console.WriteLine($"reload:{factoryId} time:{DateTime.Now.Subtract(_lastRefresh).TotalSeconds} max:{refreshInterval}");

        if (DateTime.Now.Subtract(_lastRefresh).TotalSeconds > refreshInterval)
        {
            _lastRefresh = DateTime.Now;

            var t = DateTime.UtcNow.ToString("yyyyMMddHHssmm");
            var sec = SecurityHelper.AESEncrypt($"{factoryId}secbkl{t}");
            var url = contextServer + $"/iot/loadContext?factoryId={factoryId}&sign={sec}&t={t}";


            try
            {
                var resp = http.GetAsync(url).Result;
                var strstr = resp.Content.ReadAsStringAsync().Result;
                contexts = TryCatchExtention.TryCatch(() => JsonConvert.DeserializeObject<DgaReadingContext[]>(strstr), null, null);
                if (contexts != null && contexts.Length > 0)
                {
                    File.WriteAllText("protocol.json", SecurityHelper.AESEncrypt(strstr));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            if (contexts == null)
            {
                Console.WriteLine("ErrorLoadContext " + url);

                if (File.Exists("protocol.json"))
                {
                    var str = TryCatchExtention.TryCatch(() => SecurityHelper.AESDecrypt(File.ReadAllText("protocol.json")), null, null);
                    if (str != null)
                    {
                        contexts = JsonConvert.DeserializeObject<DgaReadingContext[]>(str);
                    }
                }

            }
            if (contexts == null || contexts.Length == 0)
            {
                Console.WriteLine("No Context  " + url);
                return;
            }

            try
            {



                foreach (var context in contexts)
                {
                    _logger.LogInformation(context.Connection.ConnStr + " " + context.Device.FactoryName + " " + context.Device.FacilityName + " " + context.Device.DeviceName + " " + string.Join("\t", context.Nodes.Select(s => s.StatusName)));

                    if (!string.IsNullOrEmpty(overrideModbus))
                    {
                        context.Connection.ModbusType = overrideModbus;
                    }
                    if (!string.IsNullOrEmpty(overrideIP))
                    {
                        context.Connection.ConnStr = overrideIP;
                    }
                }

                var first = contexts.First();
                foreach (var context in contexts)
                {
                    if (!modbusList.TryGetValue(context.Connection.Uuid, out var task))
                    {
                        switch (first.Connection.ModbusType)
                        {
                            case "iec61850":
                                //task = _serviceProvider.GetService<Iec61850Task>();
                                break;
                            case "sqlserver":
                            case "sqlite":
                            case "access":
                                task = new SqlServerTask(ConfigurationManager.AppSettings);
                                break;
                            default:
                                //task = _serviceProvider.GetService<ModbusTask>();
                                break;
                        }
                        modbusList.Add(context.Connection.Uuid, task);
                    }
                    task.Init(context);
                }
                var ids = contexts.Select(s => s.Connection.Uuid);
                var removes = modbusList.Keys.Where(s => ids.Contains(s) == false).ToList();

                foreach (var rm in removes)
                {
                    var item = modbusList[rm];
                    try { item.Unload(); }
                    catch { }
                    modbusList.Remove(rm);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadContextError:" + ex.ToString());
            }
        }
    }


    private void ExecRead()
    {
        var connTimeOutsecond = TryCatchExtention.TryCatch(() => double.TryParse(_config.Get("DGA:ConnectionTimeout"), out var a) ? a : 1, 1);
        var readInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.Get("DGA:ReadInterval"), out var a) ? a : 3600, 3600);
        try
        {
            foreach (var gp in modbusList.Values)
            {
                List<DeviceState> lis = new List<DeviceState>();
                // Console.WriteLine($"ENTER ");
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(connTimeOutsecond));

                if (DateTime.Now.Subtract(gp.LastQuery).TotalSeconds < readInterval)
                {
                    continue;
                }
                var status = gp.QueryAsync(cts.Token);
                //Console.WriteLine($"WRITE {status.First().DeviceId}  {string.Join("\t", status.Select(stat => $"{stat.Name} {stat.NameCN} {stat}"))}");
                //await _deviceStatusQueue.Writer.WriteAsync(status);
                // Console.WriteLine($"WRITE  END");
                if (status == null)
                    return;
                try
                {
                    var data = JsonConvert.SerializeObject(status);
                    foreach (var item in status)
                    {
                        _logger.LogInformation(item.DeviceId + "\t" + item.Name + "\t" + item);
                    }
                    SendMessage("dga", data);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                // Console.WriteLine($"STATUS  {string.Join("\t", status.Select(stat => $"{stat.Name} {stat.NameCN} {stat}"))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void ExecHeartBeat(DgaReadingContext[] contexts, int heartBeat, double factoryId)
    {
        if (DateTime.Now.Subtract(_lastHeartBeat).TotalSeconds > heartBeat)
        {
            _lastHeartBeat = DateTime.Now;
            try
            {
                if (contexts != null)
                {
                    foreach (var context in contexts)
                    {
                        var data = SecurityHelper.AESEncrypt(factoryId + "." + context.Device.Id + "." + DateTime.Now.UnixEpoch());
                        SendMessage("heartbeat", data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private void ExecClientTask(HttpClient http, double factoryId, string contextServer)
    {
        var taskInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.Get("DGA:TaskInterval"), out var a) ? a : 30, 30);

        if (DateTime.Now.Subtract(_lastTask).TotalSeconds > taskInterval)
        {
            _lastTask = DateTime.Now;
            var t = DateTime.UtcNow.ToString("yyyyMMddHHssmm");
            var sec = SecurityHelper.AESEncrypt($"{factoryId}secbkl{t}");
            var url = contextServer + $"/iot/loadTask?factoryId={factoryId}&sign={sec}&t={t}";

            string taskdata = "";
            try
            {
                var resp = http.GetAsync(url).Result;
                taskdata = resp.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(taskdata))
                    return;
                var decdata = SecurityHelper.AESDecrypt(taskdata, "wblmxxx##bjcjggl");
                var cmdstr = JsonConvert.DeserializeObject<ExecCmd>(decdata);
                var cmd = Cli.Wrap(cmdstr.Exec);
                StringBuilder error = new StringBuilder();
                StringBuilder output = new StringBuilder();
                if (string.IsNullOrEmpty(cmdstr.WorkingDir) == false)
                {
                    cmd = cmd.SetWorkingDirectory(cmdstr.WorkingDir);
                }
                if (cmdstr.Verbs != null && cmdstr.Verbs.Length != 0)
                {
                    cmd = cmd.SetArguments(cmdstr.Verbs);
                }
                cmd = cmd.SetStandardOutputCallback(str =>
                {
                    output.Append(str);
                })
                    .SetStandardErrorCallback(err =>
                    {
                        error.Append(err);
                    });
                var result = cmd.ExecuteAsync().Result;
                var data = JsonConvert.SerializeObject(new
                {
                    result = result,
                    output = output.ToString(),
                    error = error.ToString()
                });
                Console.WriteLine("output");
                Console.WriteLine(output);
                Console.WriteLine("error");
                Console.WriteLine(error);
                var urlOver = contextServer + $"/iot/taskResult?taskId={cmdstr.TaskId}&factoryId={factoryId}&sign={sec}&t={t}";
                http.PostAsync(urlOver, new StringContent(SecurityHelper.AESEncrypt(data, "wblmxxx##bjcjggl"))).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }


    RequestSocket pubstate = null;
    private string SendMessage(string topic, string content)
    {
        try
        {
            if (pubstate == null)
                pubstate = new RequestSocket(_config.Get("AppSetting:StateSinkServer"));

            var sended = pubstate.SendMoreFrame(topic).TrySendFrame(TimeSpan.FromSeconds(1), content);
            var recv = pubstate.TryReceiveFrameString(TimeSpan.FromSeconds(1), out var resp);
            _logger.LogInformation($"Send:{topic} Send:{sended} Recv:{recv}  ServerResponse:{resp}");
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            try
            {
                pubstate.Dispose();
            }
            catch (Exception ex12)
            {
                _logger.LogError(ex12.ToString());
            }
            pubstate = null;
        }
        return null;
    }

}
