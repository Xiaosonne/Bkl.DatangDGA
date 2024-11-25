using Bkl.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Bkl.Infrastructure;
using Microsoft.Extensions.Configuration;
using NetMQ.Sockets;
using System.Text.Json;
using NetMQ;
using System.Security.Policy;
using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.IO;


public class StateSourceService : BackgroundService
{
    private IConfiguration _config;
    private IServiceScope _scope;
    private ILogger<StateSourceService> _logger;
    private IServiceProvider _serviceProvider;
    public class ExecCmd
    {
        public string Exec { get; set; }
        public string WorkingDir { get; set; }
        public string[] Verbs { get; set; }
        public long TaskId { get; set; }
    }
    public StateSourceService(
        ILogger<StateSourceService> logger,
        IServiceProvider serviceProvider,
        IConfiguration config)
    {
        _config = config;
        _scope = serviceProvider.CreateScope();
        _logger = logger;
        _serviceProvider = _scope.ServiceProvider;
    }

    DateTime _lastRefresh = DateTime.MinValue;
    DateTime _lastTask = DateTime.MinValue;
    DateTime _lastHeartBeat = DateTime.MinValue;
    Dictionary<string, IReadTask> modbusList = new Dictionary<string, IReadTask>();

    HttpClient http = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
    Dictionary<string, string> dic = new Dictionary<string, string>();
    DgaReadingContext[] contexts = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _config.GetSection("Iec61850:GasMap").Bind(dic);


        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000);

            var heartBeat = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:HeartBeatInterval").Value, out var a) ? a : 30, 30);

            var factoryId = TryCatchExtention.TryCatch(() => long.TryParse(_config.GetSection("AppSetting:FactoryId").Value, out var a) ? a : 0, 0);

            var contextServer = _config.GetSection("AppSetting:ContextServer").Value;

            ExecHeartBeat(contexts, heartBeat, factoryId);

            await ExecClientTask(http, factoryId, contextServer);

            await ExecLoadContext(factoryId, contextServer);

            await ExecRead();
        }
    }

    private async Task ExecLoadContext(long factoryId, string contextServer)
    {
        var refreshInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:RefreshInterval").Value, out var a) ? a : 30, 30);
        var overrideIP = _config.GetValue<string>("overrideIPEndPoint");
        var overrideModbus = _config.GetValue<string>("overrideModbusType");
        var overriderModbusOffset = _config.GetValue<string>("overrideModbusOffset");
        //Console.WriteLine($"reload:{factoryId} time:{DateTime.Now.Subtract(_lastRefresh).TotalSeconds} max:{refreshInterval}");

        if (DateTime.Now.Subtract(_lastRefresh).TotalSeconds > refreshInterval)
        {
            _lastRefresh = DateTime.Now;

            var t = DateTime.UtcNow.ToString("yyyyMMddHHssmm");
            var sec = SecurityHelper.AESEncrypt($"{factoryId}secbkl{t}");
            var url = contextServer + $"/iot/loadContext?factoryId={factoryId}&sign={sec}&t={t}";


            try
            {
                var resp = await http.GetAsync(url);
                var strstr = resp.Content.ReadAsStringAsync().Result;
                contexts = TryCatchExtention.TryCatch(() => JsonSerializer.Deserialize<DgaReadingContext[]>(strstr), null, null);
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
                        contexts = JsonSerializer.Deserialize<DgaReadingContext[]>(str);
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
                    _logger.LogInformation(context.Connection.ConnStr + " " + context.Device.FactoryName + " " + context.Device.FacilityName + " " + context.Device.DeviceName + " " + string.Join("\t", context.Nodes.Select(s => s.StatusName + " " + s.DataOrder.ToString())));

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
                    if (!modbusList.TryGetValue(context.Device.Id.ToString(), out var task))
                    {
                        switch (first.Connection.ModbusType)
                        {
                            case "iec61850":
                                task = _serviceProvider.GetService<Iec61850Task>();
                                break;
                            case "sqlserver":
                            case "sqlite":
                            case "access":
                                task = _serviceProvider.GetService<SqlServerTask>();
                                break;
                            default:
                                if (!string.IsNullOrEmpty(overriderModbusOffset))
                                {
                                    var remove = new List<ModbusNodeInfo>();
                                    foreach (var node in context.Nodes)
                                    {
                                        var data = _config.GetValue<string>("modbus:" + node.StatusName);
                                        if (!string.IsNullOrEmpty(data))
                                        {
                                            var arr = data.Split(",");
                                            node.ReadType = (ModbusReadType)int.Parse(arr[0]);
                                            node.StartAddress = short.Parse(arr[1]);
                                            node.DataSize = byte.Parse(arr[2]);
                                            node.DataOrder = (ModbusByteDataOrder)int.Parse(arr[3]);
                                        }
                                        else
                                        {
                                            remove.Add(node);
                                        }
                                    }
                                    foreach (var rm in remove)
                                    {
                                        context.Nodes.Remove(rm);
                                    }
                                }
                                task = _serviceProvider.GetService<ModbusTask>();
                                break;
                        }
                        modbusList.Add(context.Device.Id.ToString(), task);
                    }
                    if (overrideModbus != null)
                    {
                        context.Connection.ModbusType = overrideModbus;
                    }
                    if (overrideIP != null)
                    {
                        context.Connection.ConnStr = overrideIP;
                    }
                    task.Init(context);
                }
                var ids = contexts.Select(s => s.Device.Id.ToString());
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

    private async Task ExecRead()
    {
        var connTimeOutsecond = TryCatchExtention.TryCatch(() => double.TryParse(_config.GetSection("DGA:ConnectionTimeout").Value, out var a) ? a : 1, 1);
        var readInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:ReadInterval").Value, out var a) ? a : 3600, 3600);
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
                var status = await gp.QueryAsync(cts.Token);
                //Console.WriteLine($"WRITE {status.First().DeviceId}  {string.Join("\t", status.Select(stat => $"{stat.Name} {stat.NameCN} {stat.Value}"))}");
                //await _deviceStatusQueue.Writer.WriteAsync(status);
                // Console.WriteLine($"WRITE  END");
                if (status == null)
                    return;
                try
                {
                    var data = JsonSerializer.Serialize(status);
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in status)
                    {
                        sb.Append(item.Name + "\t" + item.Value + "\t");
                    }
                    _logger.LogInformation($"{gp.Device.DeviceName}\t{gp.Device.Id}\t{sb}");
                    await SendMessage("dga", data, gp.Device);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                // Console.WriteLine($"STATUS  {string.Join("\t", status.Select(stat => $"{stat.Name} {stat.NameCN} {stat.Value}"))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async void ExecHeartBeat(DgaReadingContext[] contexts, int heartBeat, double factoryId)
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
                        await SendMessage("heartbeat", data, context.Device);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private async Task ExecClientTask(HttpClient http, long factoryId, string contextServer)
    {
        var taskInterval = TryCatchExtention.TryCatch(() => int.TryParse(_config.GetSection("DGA:TaskInterval").Value, out var a) ? a : 30, 30);

        if (DateTime.Now.Subtract(_lastTask).TotalSeconds > taskInterval)
        {
            _lastTask = DateTime.Now;
            var t = DateTime.UtcNow.ToString("yyyyMMddHHssmm");
            var sec = SecurityHelper.AESEncrypt($"{factoryId}secbkl{t}");
            var url = contextServer + $"/iot/loadTask?factoryId={factoryId}&sign={sec}&t={t}";

            string taskdata = "";
            try
            {
                var resp = await http.GetAsync(url);
                taskdata = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(taskdata))
                    return;
                var decdata = SecurityHelper.AESDecrypt(taskdata, "wblmxxx##bjcjggl");
                var cmdstr = JsonSerializer.Deserialize<ExecCmd>(decdata);
                var cmd = Cli.Wrap(cmdstr.Exec);
                StringBuilder error = new StringBuilder();
                StringBuilder output = new StringBuilder();
                if (string.IsNullOrEmpty(cmdstr.WorkingDir) == false)
                {
                    cmd = cmd.WithWorkingDirectory(cmdstr.WorkingDir);
                }
                if (cmdstr.Verbs != null && cmdstr.Verbs.Length != 0)
                {
                    cmd = cmd.WithArguments(cmdstr.Verbs);
                }
                cmd = cmd.WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error));
                cmd = cmd.WithValidation(CommandResultValidation.None);
                var data = JsonSerializer.Serialize(new
                {
                    result = await cmd.ExecuteAsync(),
                    output = output.ToString(),
                    error = error.ToString()
                });
                Console.WriteLine("output");
                Console.WriteLine(output);
                Console.WriteLine("error");
                Console.WriteLine(error);
                var urlOver = contextServer + $"/iot/taskResult?taskId={cmdstr.TaskId}&factoryId={factoryId}&sign={sec}&t={t}";
                await http.PostAsync(urlOver, new StringContent(SecurityHelper.AESEncrypt(data, "wblmxxx##bjcjggl")));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    Channel<(string topic, string content)> channel = Channel.CreateBounded<(string topic, string content)>(new BoundedChannelOptions(10)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    });


    RequestSocket pubstate = null;


    private async Task<string> SendMessage(string topic, string content, BklDeviceMetadata context)
    {
        try
        {
            if (pubstate == null)
                pubstate = new RequestSocket(_config.GetSection("AppSetting:StateSinkServer").Value);

            var sended = pubstate.SendMoreFrame(topic).TrySendFrame(TimeSpan.FromSeconds(1), content);
            var recv = pubstate.TryReceiveFrameString(TimeSpan.FromSeconds(1), out var resp);
            _logger.LogInformation($"{context.Id} {context.DeviceName} Send:{topic} Send:{sended} Recv:{recv}  ServerResponse:{resp}");
            DateTime send = DateTime.Now;
            while (sended == true && recv == true && DateTime.Now.Subtract(send).TotalMilliseconds < 1000 && channel.Reader.Count > 0)
            {
                (var tp, var ct) = await channel.Reader.ReadAsync();
                sended = pubstate.SendMoreFrame(tp).TrySendFrame(TimeSpan.FromSeconds(1), ct);
                recv = pubstate.TryReceiveFrameString(TimeSpan.FromSeconds(1), out var resp1);
                _logger.LogInformation($"ReSend:{tp} Send:{sended} Recv:{recv}  ServerResponse:{resp1}");
            }
            return resp;
        }
        catch (Exception ex)
        {
            await channel.Writer.WriteAsync((topic, content));
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
