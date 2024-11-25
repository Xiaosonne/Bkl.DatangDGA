using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bkl.Infrastructure;
using System.Text.Json;
using System.IO;
using Orleans;
using Bkl.Dst.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using StackExchange.Redis;
using Orleans.Internal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;
using RSAExtensions;
using Bkl.ESPS.Bussiness;

namespace Bkl.ESPS.Controllers
{
    //[ApiController]
    //[Authorize]
    //[Route("[controller]")]
    //public class SecurityController : Controller
    //{
    //    private ILogger<ManagementController> logger;
    //    public SecurityController(ILogger<ManagementController> logger)
    //    {
    //        this.logger = logger;
    //    }
    //    [HttpGet("cert")]
    //    public string GetPrivateKey([FromServices] BklDbContext context, [FromServices] IRedisClient redis, [FromServices] LogonUser user)
    //    {
    //        var userdb = context.BklFactoryUser.Where(s => s.Id == user.userId).FirstOrDefault();
    //        if (redis.SetEntryInHashIfNotExists($"UserCert", $"uid.{user.userId}.gen", DateTime.Now.ToString()))
    //        {
    //            var rsa = RSA.Create(2048);
    //            var pub = rsa.ExportPkcs8PublicKey();
    //            var pri = rsa.ExportPkcs8PrivateKey();
    //            var base64pri = Convert.ToBase64String(pri);
    //            var base64pub = Convert.ToBase64String(pub);
    //            var token = SecurityHelper.AESEncrypt($"{userdb.Id}.{userdb.Password}.{base64pri.Substring(10, 20)}");
    //            redis.SetEntryInHashIfNotExists($"UserCert", $"uid.{user.userId}.pub", base64pub);
    //            redis.SetEntryInHashIfNotExists($"UserCert", $"uid.{user.userId}.pri", base64pri);
    //            redis.SetEntryInHashIfNotExists($"UserCert", $"uid.{user.userId}.token", token);
    //            return token;
    //        }
    //        return null;
    //    }
    //}

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    [Route("management")]
    public class IotController : Controller
    {
        BklDbContext context;
        private ILogger<ManagementController> logger;

        public IotController(BklDbContext context, ILogger<ManagementController> logger)
        {
            this.context = context;
            this.logger = logger;
        }
        public class ExecCmd
        {
            public string Exec { get; set; }
            public string WorkingDir { get; set; }
            public string[] Verbs { get; set; }
            public long TaskId { get; set; }
        }
        [HttpGet("header-statistic")]
        public IActionResult HeaderStatistic([FromServices] LogonUser user, [FromServices] BklDbContext context, string prefix)
        {
            var faclist = context.BklFactory.Where(s => s.FactoryName != "主站").Count();
            var facilist = context.BklFactoryFacility.Where(s => s.FactoryName != "主站").Count();
            var devlist = context.BklDeviceMetadata.Where(s => s.FactoryName != "主站").Count();


            return Json(new { device = devlist, factory = faclist, facility = facilist });
        }



        [HttpGet("unnormal-device-list")]
        public IActionResult UnnormalDeviceList([FromServices] LogonUser user, [FromServices] IRedisClient redis, string prefix)
        {
            var arr = redis.GetValuesFromHash("DeviceUnnormal")
                .Where(s => string.IsNullOrEmpty(prefix) || s.Key.StartsWith(prefix))
                .GroupBy(q => q.Key.Split(".")[0])
                .Select(s =>
                {
                    return s.Select(r => new { deviceId = r.Key.Split('.')[0], type = r.Key.Split(".")[1], unnormal = r.Value.ToString().JsonToObj<Models.DGAModel.UnnormalContext>() });
                })
                .SelectMany(s => s).ToArray();
            return Json(arr);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("reset-device-alarm")]
        public IActionResult ResetDeviceAlarm([FromServices] LogonUser user, [FromServices] IRedisClient redis, [FromQuery] long deviceId, [FromQuery] long alarmId)
        {
            redis.RemoveEntryFromHash($"DeviceAlarms:{deviceId}", alarmId.ToString());
            return Ok();
        }


        [HttpDelete("del-device")]
        public async Task<IActionResult> DeleteDevice([FromServices] LogonUser user, [FromQuery] long deviceId)
        {
            if (user.IsAdmin() == false)
            {
                return BadRequest("非管理员用户无法操作");
            }
            var pairs = context.ModbusDevicePair.Where(s => s.DeviceId == deviceId).ToList();
            var devices = context.BklDeviceMetadata.Where(s => s.Id == deviceId).FirstOrDefault();
            if (devices != null)
            {
                context.BklDeviceMetadata.Remove(devices);
            }
            if (pairs.Count > 0)
            {
                context.ModbusDevicePair.RemoveRange(pairs);
            }
            await context.SaveChangesAsync();
            try
            {
                System.IO.File.AppendAllText($"del-file-{DateTime.Now.UnixEpoch()}.log", JsonSerializer.Serialize(new
                {
                    device = devices,
                    pairConnection = pairs
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            return Ok();

        }


        [HttpGet("dga-filter-devices")]
        public async Task<IActionResult> DgaFilterDevices([FromServices] LogonUser user, [FromServices] IRedisClient redisClient,
            long deviceId = 0,
            long factoryId = 0,
            long facilityId = 0,
            string deviceType = null,
            int needFull = 0,
            [FromQuery(Name = "ids")] List<long> deviceIds = null)
        {

            deviceType = deviceType ?? "";
            var condition = context.BklDeviceMetadata.Where(s => 1 == 1);

            if (factoryId != 0)
                condition = context.BklDeviceMetadata.Where(s => s.FactoryId == factoryId);

            if (deviceIds != null && deviceIds.Count > 0)
                condition = condition.Where(s => deviceIds.Contains(s.Id));
            if (!deviceType.Empty())
                condition = condition.Where(s => s.DeviceType == deviceType);
            if (facilityId != 0)
                condition = condition.Where(s => s.FacilityId == facilityId);
            if (deviceId != 0)
                condition = condition.Where(s => s.Id == deviceId);

            if (user.IsAdmin() == false)
            {
                var devIds = user.GetPermittedId(context, PermissionConstants.TargetDevice);
                condition = condition.Where(s => devIds.Contains(s.Id));
            }

            var devices = condition.AsNoTracking().ToList().OrderBy(s => s.FactoryId).ToList();

            var devTypes = devices.Select(s => s.DeviceType).Distinct();
            var nodes = context.ModbusNodeInfo.Where(s => devTypes.Contains(s.ProtocolName)).ToList();
            var conns = context.ModbusConnInfo.ToList();
            var rules = context.BklAnalysisRule.ToList();
            var pairs = context.ModbusDevicePair.ToList();

            var status = new List<DeviceDgaUpdateStatus>();
            Dictionary<long, Dictionary<string, DGAModel.DgaAlarmResult>> errorStatus = new();

            //List<Task> loadTasks = new List<Task>();
            foreach (var device in devices)
            {
                var statusNodes = (from p in pairs
                                   join n in nodes on p.NodeId equals n.Id
                                   join c in conns on p.ConnectionId equals c.Id
                                   where p.DeviceId == device.Id
                                   select new { node = n, pairId = p.Id, connUuid = c.Uuid }).ToList();
                var deviceRules = rules.Where(s => s.DeviceId == device.Id).ToList();

                List<DeviceDgaUpdateStatus> devStatus = new();

                try
                {
                    var dic = redisClient.GetValuesFromHash($"DeviceStatus:{device.Id}");
                    if (dic.Count == 0)
                    {
                        logger.LogWarning($"LoadDeviceStatusZero {device.FactoryName} {device.FacilityName} {device.DeviceName} {dic.Count}");
                    }
                    var redisDb = dic.Select(s => JsonSerializer.Deserialize<DeviceDgaUpdateStatus>(s.Value.ToString())).ToList();
                    //devStatus.AddRange();


                    foreach (var ss in statusNodes)
                    {
                        var runss = redisDb.FirstOrDefault(s => s.Name == ss.node.StatusName);
                        if (runss == null)
                        {
                            devStatus.Add(new DeviceDgaUpdateStatus
                            {
                                ConnUuid = ss.connUuid,
                                Index = ss.pairId,
                                AttributeId = ss.node.Id,
                                DeviceId = device.Id,
                                Name = ss.node.StatusName,
                                NameCN = ss.node.StatusNameCN,
                                Value = "-1",
                                Unit = ss.node.Unit,
                                UnitCN = ss.node.UnitCN,
                            });
                        }
                        else
                        {
                            runss.Unit = ss.node.Unit;
                            runss.UnitCN = ss.node.UnitCN;
                            runss.NameCN = ss.node.StatusNameCN;
                            devStatus.Add(runss);
                        }
                    }
                    if (needFull == 1)
                    {
                        foreach (var item in redisDb)
                        {
                            if (devStatus.Any(s => s.Name == item.Name))
                                continue;
                            devStatus.Add(item);
                        }
                    }
                    status.AddRange(devStatus);
                }
                catch (Exception ex)
                {
                    logger.LogError("ErrorLoadDeviceStatus:" + ex.ToString());
                }


                try
                {

                    var dicAlarm = redisClient.GetValuesFromHash($"DeviceAlarms:{device.Id}");

                    errorStatus.Add(device.Id, dicAlarm.ToDictionary(t => t.Key, t => JsonSerializer.Deserialize<DGAModel.DgaAlarmResult>(t.Value.ToString())));
                }
                catch (Exception ex)
                {
                    logger.LogError("ErrorLoadDeviceAlarms:" + ex.ToString());
                }
            }


            List<BklDeviceMetadata> orderDevices = new List<BklDeviceMetadata>();
            foreach (var order in FactoryOrders.orders)
            {
                var devs = devices.Where(s => s.FactoryName.Contains(order)).OrderBy(s => s.DeviceName).ToList();
                orderDevices.AddRange(devs);
                foreach (var d in devs)
                {
                    devices.Remove(d);
                }
            }
            orderDevices.AddRange(devices);

            return new JsonResult(new { devices = orderDevices, status = status, errorStatus });
        }


        [AllowAnonymous]
        [HttpGet("loadContext")]
        public async Task<IActionResult> GetContext([FromServices] BklDbContext context, [FromServices] IRedisClient redis, [FromQuery] long factoryId, [FromQuery] string sign, [FromQuery] string t)
        {

            List<DgaReadingContext> lis = new List<DgaReadingContext>();
            try
            {
                sign = sign.Replace(" ", "+");
                var data = SecurityHelper.AESDecrypt(sign);
                if (data != $"{factoryId}secbkl{t}")
                {
                    logger.LogError($"LoadContextSecurity Error sign:{sign} t:{t} fac:{factoryId} dec:{data}");
                    return BadRequest("error");
                }

                var devs = context.BklDeviceMetadata.Where(s => s.FactoryId == factoryId).ToList();
                foreach (var dev in devs)
                {
                    try
                    {
                        redis.SetEntryInHash("DeviceLoadContext", dev.Id.ToString(), DateTime.Now.UnixEpoch().ToString());
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.ToString());
                    }

                    var connIds = await context.ModbusDevicePair.Where(s => s.DeviceId == dev.Id).Select(s => s.ConnectionId).Distinct().ToArrayAsync();
                    foreach (var conId in connIds)
                    {
                        var _connection = await context.ModbusConnInfo.FirstOrDefaultAsync(s => s.Id == conId);
                        //connectionId busId protocol  device
                        var _devicePairs = await context.ModbusDevicePair.Where(s => s.DeviceId == dev.Id && s.ConnectionId == _connection.Id).AsNoTracking().ToListAsync();
                        var protocols = _devicePairs.Select(s => s.NodeId).Distinct();
                        var _nodes = await context.ModbusNodeInfo.Where(s => protocols.Contains(s.Id)).AsNoTracking().ToListAsync();
                        lis.Add(new DgaReadingContext
                        {
                            Device = dev,
                            Connection = _connection,
                            Pairs = _devicePairs,
                            Nodes = _nodes
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{factoryId} {sign} {t}");
                logger.LogError(ex.ToString());
            }
            logger.LogInformation($"LoadContext {DateTime.Now} {factoryId} {sign} {t} {lis.Count}");
            return Json(lis, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }


        [AllowAnonymous]
        [HttpGet("loadTask")]
        public async Task<string> GetClientTask([FromServices] BklDbContext context, [FromServices] IRedisClient redis, [FromQuery] long factoryId, [FromQuery] string sign, [FromQuery] string t)
        {
            await Task.Delay(1);

            try
            {
                var data = SecurityHelper.AESDecrypt(sign);
                if (data != $"{factoryId}secbkl{t}")
                    return "";
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                return "";
            }


            var lis = redis.GetKeysFromHash($"UserTask:{factoryId}");
            if (lis == null || lis.Count == 0)
                return "";
            string val = redis.GetValueFromHash($"UserTask:{factoryId}", lis[0]);
            if (val == null)
                return "";
            var cmd = JsonSerializer.Deserialize<ExecCmd>(val);
            cmd.TaskId = long.Parse(lis[0]);
            return SecurityHelper.AESEncrypt(JsonSerializer.Serialize(cmd), TASK_SECKEY);
        }
        const string TASK_SECKEY = "wblmxxx##bjcjggl";
        [AllowAnonymous]
        [HttpPost("taskResult")]
        public async Task<string> PostClientTaskResult([FromServices] BklDbContext context, [FromServices] IRedisClient redis, [FromQuery] long factoryId, [FromQuery] string sign, [FromQuery] long taskId, [FromQuery] string t)
        {


            var body = Encoding.UTF8.GetString((await this.Request.BodyReader.ReadAsync()).Buffer);
            try
            {
                var jsonst = SecurityHelper.AESDecrypt(body, TASK_SECKEY);
                logger.LogInformation(jsonst);
                redis.SetEntryInHash($"UserTaskResult:{factoryId}", taskId.ToString(), jsonst);
                redis.RemoveEntryFromHash($"UserTask:{factoryId}", taskId.ToString());
            }
            catch (Exception ex)
            {

            }
            await Task.Delay(1);
            return "";
        }
        [AllowAnonymous]
        [HttpPost("writeTask")]
        public async Task<string> WriteTask([FromServices] IRedisClient redis, [FromQuery] long factoryId, [FromQuery] string password)
        {
            if (password != "wblmxxxbjcjggl")
            {
                return "";
            }
            var buffer = await this.Request.BodyReader.ReadAsync();
            var taskid = SnowId.NextId().ToString();
            redis.SetEntryInHash($"UserTask:{factoryId}", taskid, Encoding.UTF8.GetString(buffer.Buffer));
            CancellationTokenSource cts = new CancellationTokenSource();
            var result = "";
            cts.CancelAfter(60000);
            while (cts.Token.IsCancellationRequested == false)
            {
                await Task.Delay(1000);
                result = redis.GetValueFromHash($"UserTaskResult:{factoryId}", taskid);
                if (result != null)
                {
                    break;
                }
            }
            return result;
        }

        [HttpPut("update-connection")]
        public async Task<IActionResult> UpdateProtocol([FromBody] ModbusConnInfo connPut, [FromQuery] long connId)
        {
            var conn = context.ModbusConnInfo.Where(s => s.Id == connId).FirstOrDefault();
            conn.ConnStr = connPut.ConnStr;
            conn.ConnType = connPut.ConnType;
            conn.ModbusType = connPut.ModbusType;
            context.SaveChanges();
            return Json(conn);
        }



        [HttpGet("filter-devices")]
        public async Task<IActionResult> FilterDevices([FromServices] LogonUser user, [FromServices] IRedisClient redisClient,
             long deviceId = 0,
            long factoryId = 0,
            long facilityId = 0,
            string deviceType = null,
            string subsys = null,
            [FromQuery(Name = "ids")] List<long> deviceIds = null)
        {

            deviceType = deviceType ?? "";
            var condition = context.BklDeviceMetadata.Where(s => 1 == 1);

            if (factoryId != 0)
                condition = context.BklDeviceMetadata.Where(s => s.FactoryId == factoryId);

            if (deviceIds != null && deviceIds.Count > 0)
                condition = condition.Where(s => deviceIds.Contains(s.Id));
            if (!deviceType.Empty())
                condition = condition.Where(s => s.DeviceType == deviceType);
            if (facilityId != 0)
                condition = condition.Where(s => s.FacilityId == facilityId);
            if (deviceId != 0)
                condition = condition.Where(s => s.Id == deviceId);
            //if (subsys != null)
            //{
            //    string facType = "";
            //    switch (subsys)
            //    {
            //        case "byq":
            //            facType = "Transformer";
            //            break;
            //        case "fjzn":
            //            facType = "WindPowerGenerator";
            //            break;
            //        case "fdj":
            //            facType = "HeatPowerGenerator";
            //            break;
            //        case "wpg":
            //            facType = "WindPowerGenerator";
            //            break;
            //        default:
            //            break;
            //    }
            //    condition = condition.Join(context.BklFactoryFacility.Where(q => q.FacilityType == facType), dev => dev.FacilityId, fa => fa.Id, (dev, fa) => dev);
            //}

            var devices = condition.AsNoTracking().ToList();
            var devTypes = devices.Select(s => s.DeviceType).Distinct();
            var nodes = context.ModbusNodeInfo.Where(s => devTypes.Contains(s.ProtocolName)).ToList();
            var conns = context.ModbusConnInfo.ToList();
            var status = new List<DeviceUpdateStatus>();
            Dictionary<long, Dictionary<string, DeviceAlarmEntry>> errorStatus = new Dictionary<long, Dictionary<string, DeviceAlarmEntry>>();
            var pairs = context.ModbusDevicePair.ToList();
            List<Task> loadTasks = new List<Task>();
            foreach (var device in devices)
            {



                var statusNodes = (from p in pairs
                                   join n in nodes on p.NodeId equals n.Id
                                   join c in conns on p.ConnectionId equals c.Id
                                   where p.DeviceId == device.Id
                                   select new { node = n, pairId = p.Id, connUuid = c.Uuid }).ToList();
                List<DeviceAlarmEntry> devAlarm = new();
                List<DeviceUpdateStatus> devStatus = new();
                //try
                //{
                //    IDeviceGrain grain = clusterClient.GetGrain<IDeviceGrain>(new DeviceGrainId(device));
                //    await grain.GetDevice().WithTimeout<BklDeviceMetadata>(TimeSpan.FromMilliseconds(3000));
                //}
                //catch (Exception ex)
                //{
                //    logger.LogError(ex.ToString());
                //}
                //try
                //{
                //    IDeviceGrain grain = clusterClient.GetGrain<IDeviceGrain>(new DeviceGrainId(device));
                //    //devAlarm = (await grain.GetAlarms()).ToList();
                //    devStatus = (await grain.GetStatus()).ToList();
                //    if (device.DeviceType == "DGA")
                //    {
                //        IAnalysisDGAGrain dgagrain = clusterClient.GetGrain<IAnalysisDGAGrain>("DGAGPR" + device.Id);
                //        List<DeviceUpdateStatus> lis = new List<DeviceUpdateStatus>(devStatus);
                //        lis.AddRange((await dgagrain.GetStatus()).Select(q => new DeviceUpdateStatus { DeviceId = device.Id, Name = q.name, Value = q.value }));
                //        devStatus = lis.ToList();
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.ToString());
                //}
                var dic = redisClient.GetValuesFromHash($"DeviceStatus:{device.Id}");
                devStatus.AddRange(dic.Select(s => JsonSerializer.Deserialize<DeviceUpdateStatus>(s.Value.ToString())));
                foreach (var ss in statusNodes)
                {
                    var runss = devStatus.FirstOrDefault(s => s.Index == ss.pairId && s.AttributeId == ss.node.Id && s.Name == ss.node.StatusName);
                    if (runss == null)
                    {
                        devStatus.Add(new DeviceUpdateStatus
                        {
                            ConnUuid = ss.connUuid,
                            Index = ss.pairId,
                            AttributeId = ss.node.Id,
                            DeviceId = device.Id,
                            Name = ss.node.StatusName,
                            NameCN = ss.node.StatusNameCN,
                            Value = "-1",
                            Unit = ss.node.Unit,
                            UnitCN = ss.node.UnitCN,
                        });
                    }
                }
                status.AddRange(devStatus);
                errorStatus.Add(device.Id, devAlarm.ToDictionary(q => q.Key, q => q));
            }

            Task.WaitAll(loadTasks.ToArray(), 2000);

            List<BklDeviceMetadata> devicesret = new List<BklDeviceMetadata>();
            foreach (var order in FactoryOrders.orders)
            {
                var devs = devices.Where(s => s.FactoryName.Contains(order)).OrderBy(s => s.DeviceName).ToList();
                devicesret.AddRange(devs);
            }
            return new JsonResult(new { devices = devicesret, status = status, errorStatus });
        }

        [HttpGet("status-devices")]
        public IActionResult ListDevicesWithStatus([FromServices] LogonUser user, [FromServices] IRedisClient redisClient, long deviceId = 0, string deviceType = null)
        {

            var arr = context.BklDeviceMetadata.Where(q => (deviceType == null || q.DeviceType == deviceType) && (deviceId == 0 || q.Id == deviceId) && q.FactoryId == user.factoryId).ToList();
            var dids = arr.Select(q => new { q.Id, q.DeviceType }).ToList();
            Dictionary<long, Dictionary<string, string>> status = new Dictionary<long, Dictionary<string, string>>();
            Dictionary<long, Dictionary<string, string>> errorStatus = new Dictionary<long, Dictionary<string, string>>();
            foreach (var groupDid in dids.GroupBy(q => q.DeviceType))
            {
                foreach (var did in groupDid)
                {
                    try
                    {
                        var dic = redisClient.GetValuesFromHash($"{did.DeviceType}Status:{did.Id}");
                        if (dic != null)
                            status.Add(did.Id, dic.ToDictionary(q => q.Key, q => q.Value.ToString()));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    try
                    {
                        var dic = redisClient.GetValuesFromHash($"{did.DeviceType}ErrorStatus:{did.Id}");
                        if (dic != null)
                            errorStatus.Add(did.Id, dic.ToDictionary(q => q.Key, q => q.Value.ToString()));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

            }

            return new JsonResult(new { devices = arr, status = status, errorStatus });
        }
        [HttpGet("refresh-status")]
        public async Task<IActionResult> RefreshDeviceStatus([FromServices] LogonUser user, [FromServices] IClusterClient clusterClient, [FromServices] BklDbContext context, [FromQuery] long deviceId)
        {
            var uuids = context.ModbusDevicePair.Where(s => s.DeviceId == deviceId).Select(s => s.ConnUuid).Distinct();
            List<DeviceUpdateStatus> lis = new List<DeviceUpdateStatus>();
            foreach (var uuid in uuids)
            {
                var imodbus = clusterClient.GetGrain<IModbusGrain>(new ModbusGrainId(uuid));
                var status = await imodbus.ReadStatus().WithTimeout(TimeSpan.FromMilliseconds(5000));
                lis.AddRange(status);
            }
            return Json(lis);
        }


        [HttpPost("control")]
        public async Task<IActionResult> Control([FromServices] LogonUser user, [FromServices] IClusterClient clusterClient,
             string connUuid, long attributeId, long deviceId, long pairId, string value)
        {
            IDeviceGrain grain = clusterClient.GetGrain<IDeviceGrain>(new DeviceGrainId(deviceId));
            var con = context.ModbusDevicePair.FirstOrDefault(s => s.Id == pairId);
            await grain.SetStatus(new WriteDeviceStatusRequest
            {
                PairId = pairId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                ConnUuid = connUuid,
                ProtocolName = con.ProtocolName,
                BusId = con.BusId,
                Data = new byte[] { byte.Parse(value) }
            });
            return Json(new { error = 0 });
        }



        [HttpPost("create-device")]
        public async Task<BklDeviceMetadata> CreateDevice([FromServices] CommonDeviceImport commonDeviceImport, [FromServices] LogonUser user,
            CreateDeviceRequest post)
        {
            BklDeviceMetadata meta = await commonDeviceImport.CreateNewDevice(post);
            return meta;
        }
        //修改一个在库的
        [HttpPut("edit-protocol")]
        public IActionResult EditProtocols([FromServices] BklDbContext context, [FromBody] EditModbusProtoRequest request)
        {
            var addedPairs = request.Data.Where(s => s.PairId != 0 && s.Attribute != null).ToArray();
            var ids = addedPairs.Select(s => s.PairId).ToArray();
            var dbPairs = context.ModbusDevicePair.Where(s => ids.Contains(s.Id)).ToList();
            List<ModbusNodeInfo> newAttrs = new List<ModbusNodeInfo>();
            foreach (var dbPair in dbPairs)
            {
                var newPair = addedPairs.FirstOrDefault(a => a.PairId == dbPair.Id);

                var newAttr = new ModbusNodeInfo
                {
                    ProtocolName = request.ProtocolName,
                    StartAddress = Convert.ToInt16(newPair.Attribute.StartAddress),
                    ReadType = Enum.Parse<ModbusReadType>(newPair.Attribute.ReadType),
                    DataType = Enum.Parse<ModbusDataType>("dt_" + newPair.Attribute.DataType),
                    Unit = newPair.Attribute.Unit,
                    UnitCN = newPair.Attribute.UnitCN ?? newPair.Attribute.Unit,
                    StatusName = newPair.Attribute.StatusName,
                    StatusNameCN = newPair.Attribute.StatusNameCN ?? newPair.Attribute.StatusName,
                    Scale = newPair.Attribute.Scale,
                    DataOrder = Enum.Parse<ModbusByteDataOrder>(newPair.Attribute.DataOrder),
                    DataSize = newPair.Attribute.DataSize,
                    ValueMap = JsonSerializer.Serialize(newPair.Attribute.ValueMap)
                };
                dbPair.BusId = newPair.BusId;
                dbPair.ProtocolName = request.ProtocolName;
                dbPair.NodeId = newAttr.Id;
                dbPair.NodeIndex = 0;
                newAttrs.Add(newAttr);
            }

            var removed = request.Data.Where(s => s.PairId != 0 && s.Attribute == null).Select(s => s.PairId).ToArray();
            var removedPairs = context.ModbusDevicePair.Where(s => removed.Contains(s.Id)).ToList();

            using (var tran = context.Database.BeginTransaction())
            {
                try
                {
                    context.ModbusDevicePair.RemoveRange(removedPairs);
                    context.ModbusNodeInfo.AddRange(newAttrs);
                    context.SaveChanges();
                    tran.Commit();
                    return Json(new { error = 0, data = newAttrs });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    tran.Rollback();
                    return Json(new { error = 1, msg = ex.ToString() });
                }
            }

        }

        [HttpGet("get-protocol")]
        public IActionResult GetProtocols([FromServices] BklDbContext context, [FromQuery] long deviceId = 0, [FromQuery] string protocolName = "")
        {
            if (deviceId == 0)
            {
                var lis = context.ModbusNodeInfo.Where(s => protocolName == null || protocolName == "" || s.ProtocolName.Contains(protocolName))
                     .ToList()
                     .GroupBy(s => s.ProtocolName)
                     .Select(s => new { protocolName = s.Key, details = s.ToList() })
                     .ToList();
                return Json(lis);
            }
            else
            {

                var connInfo = context.ModbusDevicePair.Where(s => s.DeviceId == deviceId).Select(s => s).ToList();
                var conid = connInfo.Select(s => s.ConnectionId).Distinct();
                var conlist = context.ModbusConnInfo.Where(s => conid.Contains(s.Id)).ToList();
                var name = connInfo.Select(s => s.ProtocolName).Distinct().ToList();
                var protocols = context.ModbusNodeInfo.Where(s => name.Contains(s.ProtocolName)).ToList();
                return Json(new
                {
                    protocols = protocols.GroupBy(s => s.ProtocolName)
                     .Select(s => new { protocolName = s.Key, details = s.ToList() })
                     .ToList(),
                    connections = connInfo,
                    connectionList = conlist
                });
            }

        }
        [HttpDelete("devicepair")]
        public async Task<IActionResult> DeletePair([FromServices] BklDbContext context, long deviceId, long pairId)
        {
            var first = context.ModbusDevicePair.Where(s => s.DeviceId == deviceId && s.Id == pairId).FirstOrDefault();
            if (first != null)
            {
                context.ModbusDevicePair.Remove(first);
                await context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        [HttpPut("update-slave-id")]
        public async Task<Object> UpdatePairId([FromServices] BklDbContext context, [FromBody] Dictionary<long, byte> updates)
        {
            var pairIds = updates.Keys.ToArray();
            var pair = context.ModbusDevicePair.Where(s => pairIds.Contains(s.Id)).ToList();
            using (var tran = context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var p in pair)
                    {
                        p.BusId = updates[p.Id];
                    }
                    await context.SaveChangesAsync();
                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                }
            }
            return Json(new { error = 0, });

        }
        /// <summary>
        /// 更新或者新建modbus protocol attribute 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("create-modbus-proto")]
        public async Task<object> CreateProtocol([FromServices] BklDbContext context, CreateModbusProtoRequest req)
        {
            ModbusNodeInfo[] nodes = null;
            using (var tran = context.Database.BeginTransaction())
            {
                try
                {
                    nodes = req.Attributes.Select(s => new ModbusNodeInfo
                    {
                        ProtocolName = req.ProtoName,
                        StartAddress = Convert.ToInt16(s.StartAddress),
                        ReadType = Enum.Parse<ModbusReadType>(s.ReadType),
                        DataType = Enum.Parse<ModbusDataType>("dt_" + s.DataType),
                        Unit = s.Unit,
                        UnitCN = s.UnitCN ?? s.Unit,
                        StatusName = s.StatusName,
                        StatusNameCN = s.StatusNameCN ?? s.StatusName,
                        Scale = s.Scale,
                        DataOrder = Enum.Parse<ModbusByteDataOrder>(s.DataOrder),
                        DataSize = s.DataSize,
                        ValueMap = JsonSerializer.Serialize(s.ValueMap)
                    }).ToArray();

                    foreach (var node in nodes)
                    {
                        //同一个protocolName  同一个 statusName 视为同一个属性 
                        var dbnode = context.ModbusNodeInfo.FirstOrDefault(s => s.ProtocolName == node.ProtocolName && s.StatusName == node.StatusName);
                        //若已在库的设备 自动关联新增的属性
                        if (dbnode == null)
                        {
                            context.ModbusNodeInfo.Add(node);
                            var gps = context.ModbusDevicePair.Where(s => s.ProtocolName == node.ProtocolName).ToList();
                            List<ModbusDevicePair> lis = new List<ModbusDevicePair>();
                            foreach (var sameDev in gps.GroupBy(s => $"{s.DeviceId}.{s.ConnUuid}.{s.BusId}"))
                            {
                                var first = sameDev.First();
                                lis.Add(new ModbusDevicePair
                                {
                                    BusId = first.BusId,
                                    ConnectionId = first.ConnectionId,
                                    ConnUuid = first.ConnUuid,
                                    DeviceId = first.DeviceId,
                                    ProtocolName = node.ProtocolName,
                                    NodeId = node.Id,
                                    NodeIndex = 0
                                });
                            }
                            context.ModbusDevicePair.AddRange(lis);
                        }
                        else
                        {
                            dbnode.StartAddress = node.StartAddress;
                            dbnode.ReadType = node.ReadType;
                            dbnode.Scale = node.Scale;
                            dbnode.DataOrder = node.DataOrder;
                            dbnode.DataSize = node.DataSize;
                            dbnode.ValueMap = node.ValueMap;
                            dbnode.StatusNameCN = node.StatusNameCN;
                        }
                    }
                    await context.SaveChangesAsync();
                    await tran.CommitAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                    tran.Rollback();
                    return Json(new { error = 1 });
                }
            }
            return Json(new { error = 0, data = nodes });

        }

        [HttpPost("create-http-proto")]
        public Task<object> CreateHttpProtocol([FromServices] BklDbContext context, CreateHttpProtoRequest req)
        {
            return Task.FromResult(default(object));
        }

        [HttpPost("http-request")]
        public async Task<RequestHttpResponse> RequestHttp([FromServices] CommonDeviceImport commonDeviceImport, [FromServices] LogonUser user,
            RequestHttpRequest post)
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler());
            HttpRequestMessage req = null;
            req = new HttpRequestMessage(new HttpMethod(post.Method), post.Url);
            foreach (var item in post.Headers)
            {
                req.Headers.Add(item.Name, item.Value);
            }
            var resp = await httpClient.SendAsync(req);
            return new RequestHttpResponse { Content = await resp.Content.ReadAsStringAsync() };
        }


        //WindPowerGenerator devicetype p1 p2 p3 p4 deviceName camUser camPswd camIP
        //WindPowerGenerator devicetype p1 p2 p3 p4 deviceName udp://192.168.31.100:8888 modbus://03/21/1501/1
        //HeatEnginPlant DGA 荣堂风电场	110KV主变 tcp://192.168.1.45:502	modbusip://04/01/0/32
        //HeatEnginPlant ThermalCamera 断路器1	测点1 192.168.1.100 username password
        [HttpPost("import-device")]
        public async Task<IActionResult> ImportThermalCamera(
            [FromServices] CommonDeviceImport commonDeviceImport,
            [FromServices] BklDbContext context,
            [FromServices] LogonUser user, string splitter = "")
        {
            StreamReader sr = new StreamReader(this.Request.Body);
            ImportResult obj = await commonDeviceImport.ImportDevice(sr, splitter == "tab" ? "\t" : (splitter == "comma" ? "," : " "));
            if (obj.streams == null)
            {
                return new ContentResult()
                {
                    Content = JsonSerializer.Serialize(obj.devices),
                    ContentType = "application/json"
                };
            }
            else
            {
                return new ContentResult()
                {
                    Content = JsonSerializer.Serialize(obj.streams),
                    ContentType = "application/json"
                };
            }
        }


        [AllowAnonymous]
        [HttpGet("export-modbus-table")]
        public IActionResult ExportModbusTable([FromServices] BklDbContext context, [FromServices] IRedisClient redis)
        {
            var modbusNodes = redis.Keys("Modbus:*");
            List<string[]> lis = new List<string[]>();
            var dbNodes = context.ModbusNodeInfo.Select(s => new { s.Id, s.ValueMap }).ToArray();
            StringBuilder sb = new StringBuilder();

            foreach (var key in modbusNodes.OrderBy(s => s))
            {
                var val = redis.GetValuesFromHash(key);
                string map = "";
                try
                {
                    var aid = long.Parse(val["nodeId"]);
                    var node = dbNodes.FirstOrDefault(s => s.Id == aid);
                    map = node?.ValueMap;
                    var aaa = JsonSerializer.Deserialize<KeyNamePair[]>(map);
                    foreach (var s in aaa)
                    {
                        s.name = Regex.Unescape(s.name);
                    }
                    map = JsonSerializer.Serialize(aaa);
                }
                catch
                {

                }
                sb.AppendLine(string.Join("\t", new string[] { key.Split(":")[1], val["info"], val["name"], val["nameCN"], val["value"], map }));
            }
            return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/plain");
        }

        [AllowAnonymous]
        [HttpGet("modbus-userscript")]
        public async Task<string> GetModbusUserScripts([FromServices] BklDbContext context, long deviceId = 0)
        {

            var deviceIds = await context.BklDeviceMetadata.Where(s => deviceId == 0 || s.Id == deviceId).Select(s => s.Id).ToListAsync();
            var pairs = await context.ModbusDevicePair.Where(s => deviceIds.Contains(s.DeviceId)).ToListAsync();
            var nodes = await context.ModbusNodeInfo.ToListAsync();
            var conns = await context.ModbusConnInfo.ToListAsync();
            StringBuilder sb = new StringBuilder();
            foreach (var sameConn in pairs.GroupBy(s => s.ConnUuid))
            {
                foreach (var sameBusId in sameConn.GroupBy(s => s.BusId))
                {
                    var nodeIds = sameBusId.Select(s => s.NodeId).ToList();
                    var nodes1 = nodes.Where(s => nodeIds.Contains(s.Id) && s.ReadType == ModbusReadType.ReadHoldingRegister).ToList();
                    try
                    {
                        var statusMap = nodes1.Select(s => new
                        {
                            di = s.StatusName.Split('-')[1],
                            @do = JsonSerializer.Deserialize<KeyNamePair[]>(s.ValueMap)
                        }).ToList();
                        sb.AppendLine($"BUSID:{sameBusId.Key} IP:{conns.FirstOrDefault(s => s.Uuid == sameConn.Key)?.ConnStr}");
                        foreach (var item in statusMap.OrderBy(s => s.di))
                        {
                            if (item.@do.First(s => s.name == "正常").key == "0")
                                sb.Append($"@{item.di.Replace("DI", "DO")}=@{item.di}");
                            else
                                sb.Append($"@{item.di.Replace("DI", "DO")}=!@{item.di}");
                        }
                        sb.AppendLine();
                    }
                    catch
                    {
                        continue;
                    }

                }
            }
            return sb.ToString();
        }


    }
}
