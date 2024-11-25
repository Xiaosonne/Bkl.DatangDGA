using Bkl.Dst.Interfaces;
using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Orleans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.ServiceProcess;
using DocumentFormat.OpenXml.Wordprocessing;
using Bkl.Infrastructure;

namespace Bkl.ESPS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ThermalController : Controller
    {
        private ILogger<ThermalController> _logger;

        //private ThermalSource context;
        private IClusterClient _cluster;
        public ThermalController(IClusterClient cache, ILogger<ThermalController> logger)
        {
            _logger = logger;
            this._cluster = cache;
            //this.context = context;
        }


        [AllowAnonymous]
        [HttpGet("/data")]
        [HttpOptions("/data")]
        [HttpPut("/data")]
        [HttpDelete("/data")]
        [HttpPost("/data")]
        public async Task<string> AlarmHost([FromServices] BklConfig config)
        {
            try
            {
                var serial = new XmlSerializer(typeof(CameraAlarmEntry.EventNotificationAlert));
                var form = await this.Request.ReadFormAsync();
                CameraAlarmEntry.EventNotificationAlert alert = null;
                var lastDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)).ToString("yyyyMMddHHmmssfff");
                string storeFormat = "yyyyMMddHHmm";
                foreach (var item in form)
                {
                    alert = (CameraAlarmEntry.EventNotificationAlert)serial.Deserialize(new StringReader(item.Value[0]));

                    var path = $"d:/uploaded/{alert.ipAddress}/{alert.dateTime.ToString(storeFormat)}";
                    Directory.CreateDirectory(path);


                    Console.WriteLine(alert.ipAddress + " " + alert.channelID.ToString() + " " + alert.detectionPicturesNumber + " " + alert.activePostCount);

                    var dirs = Directory.EnumerateDirectories($"d:/uploaded/{alert.ipAddress}/");
                    var remove = dirs.Count() - 12800;
                    if (remove > 0)
                    {
                        foreach (var dir in dirs)
                        {
                            if (remove <= 0)
                                break;
                            try
                            {
                                Directory.Delete($"d:/uploaded/{alert.ipAddress}/{dir}", true);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("DeleteFileError " + ex.ToString());
                            }
                        }
                    }

                    System.IO.File.AppendAllText($"{path}/{alert.eventType}-{DateTime.Now.ToString("yyyyMMddHHmmss")}.xml", item.Value[0]);
                }
                if (alert != null)
                {
                    var path = $"d:/uploaded/{alert.ipAddress}/{alert.dateTime.ToString(storeFormat)}";
                    Directory.CreateDirectory(path);
                    System.IO.File.AppendAllText($"{path}/data.json", JsonSerializer.Serialize(alert));

                    int index = 0;
                    foreach (var file in form.Files)
                    {
                        var fname = System.IO.Path.Combine(path, $"{index}.jpg");
                        if (!System.IO.File.Exists(fname))
                        {
                            try
                            {
                                using (FileStream fs = new FileStream(fname, FileMode.OpenOrCreate))
                                {
                                    await file.CopyToAsync(fs);
                                    await fs.FlushAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("SaveFileError " + ex.ToString());
                            }
                        }
                        index++;
                    }
                }
            }
            catch
            {

            }
            return "";
        }

        [HttpGet("thermalRules")]
        public async Task<IActionResult> GetThermalRule(long cameraId, [FromServices] BklDbContext context)
        {
            var dev = context.BklThermalCamera.FirstOrDefault(s => s.DeviceId == cameraId);
            var sdk = new ThermalCameraISAPI(dev.Ip, dev.Port, dev.Account, dev.Password);
            var rules = await sdk.GetThermalRules();
            return new JsonResult(rules);
        }
        public class ProxyRequest
        {
            public string Url { get; set; }
            public string Method { get; set; }

            public string ContentType { get; set; }

            public string Body { get; set; }

            public string Username { get; set; }

            public string Password { get; set; }
        }


        [HttpPost("thermalRules")]
        [HttpPut("thermalRules")]
        public async Task<ThermalXmlObject.ResponseStatus> UpdateRule(long cameraId, [FromServices] BklDbContext context, [FromBody] ThermalMeasureRule rule, int ruleId = 0)
        {
            var dev = context.BklThermalCamera.FirstOrDefault(s => s.DeviceId == cameraId);
            var sdk = new ThermalCameraISAPI(dev.Ip, dev.Port, dev.Account, dev.Password);
            if (rule.ruleId <= 0)
                rule.ruleId = ruleId;
            if (rule.ruleId <= 0)
                rule.ruleId = 1;
            var resp = await sdk.SetThermalRule(rule);
            return resp;
        }


        static string[] cameraChannels = new string[] { "101", "102", "201", "202" };


        [HttpGet("reset-camera")]
        public async Task<IActionResult> ResetCamera(
            [FromServices] BklDbContext context,
            [FromServices] CommonDeviceImport commonDeviceImport,
            [FromQuery] long deviceId,
            [FromQuery] string username = null,
            [FromQuery] string password = null,
            [FromQuery] string host = null,
            [FromQuery] string rtspserver = "127.0.0.1:9997")
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(host))
            {
                var cam = context.BklThermalCamera.Where(s => s.DeviceId == deviceId).FirstOrDefault();
                username = username ?? cam.Account;
                password = password ?? cam.Password;
                host = host ?? cam.Ip;
            }
            var req = new CreateDeviceRequest
            {
                UserName = username,
                Password = password,
                IPaddress = host
            };
            var dev = new BklDeviceMetadata { Id = deviceId };

            var resp1 = await commonDeviceImport.AddCameraToRtspSimpleServer(req, dev, "101", rtspserver);
            var resp2 = await commonDeviceImport.AddCameraToRtspSimpleServer(req, dev, "102", rtspserver);
            var resp3 = await commonDeviceImport.AddCameraToRtspSimpleServer(req, dev, "201", rtspserver);
            var resp4 = await commonDeviceImport.AddCameraToRtspSimpleServer(req, dev, "202", rtspserver);
            if (resp1.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await commonDeviceImport.SetCameraToRtspSimpleServer(req, dev, "101", rtspserver);
            }
            if (resp2.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await commonDeviceImport.SetCameraToRtspSimpleServer(req, dev, "102", rtspserver);
            }
            if (resp3.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await commonDeviceImport.SetCameraToRtspSimpleServer(req, dev, "201", rtspserver);
            }
            if (resp4.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await commonDeviceImport.SetCameraToRtspSimpleServer(req, dev, "202", rtspserver);
            }
            return Json("");
        }

        [AllowAnonymous]
        [HttpGet("camera-list")]
        public IActionResult GetCameraList([FromServices] BklConfig config, [FromServices] BklDbContext context)
        {
            var deserial = new DeserializerBuilder()
                 //.WithAttemptingUnquotedStringTypeDeserialization()
                 .WithNamingConvention(CamelCaseNamingConvention.Instance)
                 .Build();
            var serialize = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithQuotingNecessaryStrings(true)
                .Build();


            using var fs = new FileStream("rtsp-simple-server.yml", FileMode.Open);
            fs.Seek(0, SeekOrigin.Begin);

            var templateConfig = deserial.Deserialize<YmlCameraConfig>(new StreamReader(fs));


            templateConfig.paths = new Dictionary<string, YmlCameraConfig.YmlPath>();
            var thermal = context.BklDeviceMetadata.Where(s => s.DeviceType == "ThermalCamera").ToList();
            var thermalConnection = context.BklThermalCamera.ToList();
            foreach (var con in thermalConnection)
            {
                foreach (var ch in cameraChannels)
                {
                    var path = new YmlCameraConfig.YmlPath
                    {
                        source = $"rtsp://{con.Account}:{con.Password}@{con.Ip}:554/Streaming/Channels/{ch}?transportmode=unicast",
                        sourceProtocol = "automatic",
                        sourceOnDemand = true,
                    };
                    templateConfig.paths.Add($"did{con.DeviceId}{ch}", path);
                }
            }
            var data = serialize.Serialize(templateConfig);
            ServiceController service = null;
            try
            {
                service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "bklrtsp");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
            catch
            {

            }
            try
            {
                using var fswrite = new FileStream("rtsp-simple-server-new.yml", FileMode.OpenOrCreate);
                fswrite.Seek(0, SeekOrigin.Begin);
                fswrite.SetLength(0);
                fswrite.Flush();

                using var writer = new StreamWriter(fswrite);
                writer.Write(data);
                fswrite.Flush();
                return Content(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            try
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return Content(data);
        }


        [HttpGet("temperature")]
        public async Task<IActionResult> GetTemperature([FromServices] BklDbContext context, long cameraId, int x = 0, int y = 0)
        {
            var dev = context.BklThermalCamera.FirstOrDefault(s => s.DeviceId == cameraId);
            var sdk = new ThermalCameraISAPI(dev.Ip, dev.Port, dev.Account, dev.Password);
            var boundaries = await sdk.GetThermalJpeg();
            var tempdata = boundaries.Where(s => s.IsTempratureData).FirstOrDefault();
            var data = tempdata.ReadAsTemperature();
            return new JsonResult(data);
        }
        [HttpGet("get-init")]
        [HttpPut("set-init")]
        public async Task<IActionResult> CameraInit([FromServices] BklDbContext context, long deviceId)
        {
            var body = Encoding.UTF8.GetString((await this.Request.BodyReader.ReadAsync()).Buffer);

            var dev = context.BklThermalCamera.FirstOrDefault(s => s.DeviceId == deviceId);
            var sdk = new ThermalCameraISAPI(dev.Ip, dev.Port, dev.Account, dev.Password);
            if (this.HttpContext.Request.Method == "GET")
                return Json(await sdk.GetThermalBasic());

            var resp1 = await sdk.SetThermalBasic(JsonSerializer.Deserialize<ThermalXmlObject.ThermometryBasicParam>(body));

            var resp2 = await sdk.SetThermalRule(new ThermalMeasureRule
            {
                enabled = 1,
                ruleId = 1,
                ruleName = "测温",
                regionType = 1,
                regionPoints = new List<double[]>
                {
                    new double[]{0.0,0.0},
                    new double[]{1.0,0.0},
                    new double[]{1.0,1.0},
                    new double[]{0.0,1.0},
                }
            });
            return Json(new { init = resp1, thermal = resp2 });
        }
    }
}
