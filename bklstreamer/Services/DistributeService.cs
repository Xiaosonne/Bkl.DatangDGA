using Bkl.Infrastructure;
using Bkl.Models;
using Bkl.StreamServer.Hubs;
using CsvHelper;
using InfluxDB.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.ServiceModel.Channels;
using System.Text.Json;
using System.Threading.Channels;
using static Bkl.Models.BklConfig;
using static Bkl.Models.DGAModel;
namespace Bkl.StreamServer.Services
{
    public class DistributeService : BackgroundService
    {
        private Channel<DgaPushData> _channel;
        private Channel<DgaAlarmResult> _channelAlarm;
        private IConfiguration _config;
        private ILogger<DistributeService> _logger;
        private IHubContext<DeviceStateHub> _hubContext;
        private IServiceScope _scope;
        public DistributeService(ILogger<DistributeService> logger,
             Channel<DgaPushData> channel,
             Channel<DgaAlarmResult> channelAlarm,
            IServiceProvider service,
            IHubContext<DeviceStateHub> hubContext,
            IConfiguration config)
        {
            _channel = channel;
            _channelAlarm = channelAlarm;
            _config = config;
            _logger = logger;
            _hubContext = hubContext;
            _scope = service.CreateScope();
        }
        JsonSerializerOptions webjson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SubscriberSocket sub = new SubscriberSocket();
            var url = _config.GetSection("AppSetting:DGAStateServer").Value;
            var urlAlarm = _config.GetSection("AppSetting:DGAAlarmServer").Value;
            var urlAlarmService = _config.GetSection("AppSetting:DGAAlarmService").Value;
            var connectionService = _config.GetSection("AppSetting:DeviceConnection").Value;
            while (true)
            {
                try
                {
                    sub.Connect(url);
                    sub.Connect(urlAlarm);
                    sub.Connect(urlAlarmService);
                    sub.Connect(connectionService);
                    sub.SubscribeToAnyTopic();
                    _logger.LogInformation("connected " + url);
                    _logger.LogInformation("connected " + urlAlarm);
                    _logger.LogInformation("connected " + urlAlarmService);
                    _logger.LogInformation("connected " + connectionService);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "connecterror");
                    await Task.Delay(500);
                    try
                    {
                        sub.Dispose();
                    }
                    catch (Exception ex1)
                    {
                        _logger.LogError(ex1, "Dispose Error ");
                    }
                    sub = new SubscriberSocket();
                }
            }

            while (stoppingToken.IsCancellationRequested == false)
            {
                await Task.Delay(1);
                try
                {
                    var msg = sub.ReceiveMultipartStrings(2);
                    switch (msg[0])
                    {
                        case "modbus":
                            {
                                var data = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<DeviceStatus>(str), msg[1], msg[1]);
                                if (data == null)
                                {
                                    _logger.LogError("ParsError " + msg[1]);
                                }
                            }
                            break;
                        case "dga":
                            {
                                _logger.LogInformation(msg[1]);
                                var data = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<DgaPushData>(str), msg[1], msg[1]);
                                if (data == null)
                                {
                                    _logger.LogError("ParsError " + msg[1]);
                                }
                                else
                                {
                                    await _hubContext.Clients.All.SendAsync("OnDgaStates", JsonSerializer.Serialize(new
                               DeviceWebStatus
                                    {
                                        meta = new DeviceWebMeta { FactoryId = data.FactoryId, FacilityId = data.FacilityId, DeviceId = data.DeviceId },
                                        status = data.GasData.Select(s => s.ToStatus()).SelectMany(s => s)
                                    }, webjson));
                                    await _channel.Writer.WriteAsync(data);
                                }

                            }
                            break;
                        case "dgaalarm":
                            {
                                _logger.LogInformation(msg[1]);
                                var data = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<DgaAlarmResult>(str), msg[1], msg[1]);
                                if (data == null)
                                {
                                    _logger.LogError("ParsError " + msg[1]);
                                }
                                else
                                {
                                    await _channelAlarm.Writer.WriteAsync(data);
                                }
                            }
                            break;
                        case "devconn":
                            _logger.LogInformation(msg[1]);
                            await _hubContext.Clients.All.SendAsync("OnDgaCon", msg[1]);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
            //var influxclient = new InfluxDBClient("http://127.0.0.1:8086", "eHk5ebRJ1FP0nI35rqOL91PBQquXtegfuWVNYBDG8QZ9vcteZWRyQU-Xlj7A7vCi5J9CL9DiaI8V6aWCZ8BhIg==");

            //var writeapi = influxclient.GetWriteApi();
            //while (stoppingToken.IsCancellationRequested == false)
            //{
            //    try
            //    {
            //        var msgs = sub.ReceiveMultipartStrings(2);
            //        Console.WriteLine($"topic:{msgs[0]} content:{msgs[1]}");
            //        var resps = JsonSerializer.Deserialize<ThermalTemperatureResponse[]>(msgs[1]);
            //        writeapi.WriteRecords(resps.Select(s => $"thermaltemperature,deviceId={s.deviceId},factoryId={s.factoryId},facilityId={s.facilityId},ruleId={s.ruleId} max={s.max},min={s.min},average={s.average}").ToArray(), bucket: "bucket1", org: "hn");
            //    }
            //    catch
            //    {

            //    }
            //}
        }

    }

    public class DeviceWebMeta
    {
        public long FactoryId { get; set; }
        public long FacilityId { get; set; }
        public long DeviceId { get; set; }
    }

    public class DeviceWebStatus
    {
        public DeviceWebMeta meta { get; set; }
        public IEnumerable<DeviceUpdateStatusBase> status { get; set; }
    }
}
