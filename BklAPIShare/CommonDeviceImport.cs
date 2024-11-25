using System;
using Bkl.Infrastructure;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Bkl.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
public class ImportResult
{
    public object streams { get; set; }
    public List<BklDeviceMetadata> devices { get; set; }
}
public class CommonDeviceImport
{
    private ILogger<CommonDeviceImport> _logger;
    protected BklDbContext _context;
    protected LogonUser _user;
    private BklConfig _config;
    static HttpClient client = new HttpClient();
    public CommonDeviceImport(ILogger<CommonDeviceImport> logger, BklDbContext context, BklConfig config, LogonUser user)
    {
        this._logger = logger;
        this._context = context;
        this._user = user;
        _config = config;
    }
    static string[] ModbusTrans = new string[]
    {
            "modbusrtu","modbustcp","modbusrtuovertcp"
    };
    public async Task<BklDeviceMetadata> CreateNewDevice(CreateDeviceRequest post)
    {
        string[] ps = post.Position.Split('/');


        using (var tran = _context.Database.BeginTransaction())
        {
            try
            {
                var factory = _context.BklFactory.FirstOrDefault(q => q.Id == post.FactoryId);
                if (factory == null)
                {
                    throw new Exception("未提供对应的厂区ID");
                }
                var facility = _context.BklFactoryFacility.Where(q => q.FactoryId == factory.Id && q.FacilityType == post.FacilityType && q.Name == post.FacilityName).FirstOrDefault();
                if (facility == null)
                {
                    facility = new BklFactoryFacility
                    {
                        Name = post.FacilityName,
                        CreatorId = _user.userId,
                        FactoryId = factory.Id,
                        FactoryName = factory.FactoryName,
                        FacilityType = post.FacilityType,
                        CreatorName = _user.name,
                        GPSLocation = "[]"
                    };
                    _context.BklFactoryFacility.Add(facility);
                    _context.SaveChanges();
                    //faci = context.BklFactoryFacility.Where(q => q.FacilityType == facilityType && q.Name == fname).FirstOrDefault();
                }
                var dbDevice = _context.BklDeviceMetadata.FirstOrDefault(s => s.FactoryId == factory.Id && s.FacilityId == facility.Id && s.ProbeName == post.ProbeName && s.DeviceName == post.ProbeName && s.DeviceType == post.DeviceType);

                if (dbDevice != null)
                {
                    throw new Exception("设备已经存在");
                }
                var newDevice = new BklDeviceMetadata
                {
                    GroupName = ps[ps.Length - 2],
                    ProbeName = post.ProbeName,
                    DeviceType = post.DeviceType,
                    DeviceName = post.ProbeName,
                    PDeviceName = "#",
                    PDeviceType = "#",
                    PathType = "#",
                    FullPath = post.Position,
                    Path1 = ps[0],
                    Path2 = ps.Length >= 2 ? ps[1] : "#",
                    Path3 = ps.Length >= 3 ? ps[2] : "#",
                    Path4 = ps.Length >= 4 ? ps[3] : "#",
                    Path5 = ps.Length >= 5 ? ps[4] : "#",
                    Path6 = ps.Length >= 6 ? ps[5] : "#",
                    MacAddress = $"{post.Position}/{post.ProbeName}".BKDRHash().ToString("X8"),
                    AreaName = "#",
                    ConnectionType = "rtsp",
                    DeviceMetadata = "#",
                    FactoryName = factory.FactoryName,
                    FactoryId = _user.factoryId,
                    CreatorId = _user.userId,
                };
                newDevice.FactoryId = factory.Id;
                newDevice.FactoryName = factory.FactoryName;
                newDevice.FacilityId = facility.Id;
                newDevice.FacilityName = facility.Name;


                switch (newDevice.DeviceType)
                {
                    case "ThermalCamera":
                        //红外是一对一的关系 一个点位只有一个红外
                        if (dbDevice != null)
                        {
                            return null;
                        }
                        else
                        {
                            _context.BklDeviceMetadata.Add(newDevice);
                            newDevice.ConnectionString = JsonSerializer.Serialize(new
                            {
                                visible = $"rtsp://{post.UserName}:{post.Password}@{post.IPaddress}:554/Streaming/Channels/101?transportmode=unicast",
                                thermal = $"rtsp://{post.UserName}:{post.Password}@{post.IPaddress}:554/Streaming/Channels/201?transportmode=unicast",
                            });
                            await _context.SaveChangesAsync();
                        }

                        await AddCameraToRtspSimpleServer(post, newDevice, "101", _config.RtspServer);
                        await AddCameraToRtspSimpleServer(post, newDevice, "102", _config.RtspServer);
                        await AddCameraToRtspSimpleServer(post, newDevice, "201", _config.RtspServer);
                        await AddCameraToRtspSimpleServer(post, newDevice, "202", _config.RtspServer);

                        if (!_context.BklThermalCamera.Any(s => s.Ip == post.IPaddress && s.DeviceId == newDevice.Id))
                        {
                            _context.BklThermalCamera.Add(new BklThermalCamera
                            {
                                Ip = post.IPaddress,
                                Port = post.Port,
                                Account = post.UserName,
                                Password = post.Password,
                                DeviceId = newDevice.Id,
                                UserId = newDevice.CreatorId,
                            });
                            _context.SaveChanges();
                        }
                        break;
                    case "DGA":
                    case "BandageSensor":
                    case "PTDetector":
                    case string kkk when ModbusTrans.Contains(post.TransferType.ToLower()):
                        //每一个检测点位 都可以对应多个modbus设备 
                        if (dbDevice != null)
                        {
                            newDevice = dbDevice;
                        }
                        else
                        {
                            newDevice.ConnectionString = $"{post.TransferType}://{post.IPaddress}:{post.Port}";
                            newDevice.ConnectionType = post.TransferType.ToLower();
                            _context.BklDeviceMetadata.Add(newDevice);
                            await _context.SaveChangesAsync();
                        }

                        var uuid = SecurityHelper.Get32MD5(newDevice.ConnectionString);
                        var connInfo = new ModbusConnInfo
                        {
                            ModbusType = post.TransferType.ToLower(),
                            ConnType = post.TransferType == "ModbusRTU" ? "serialport" : "tcp",
                            ConnStr = $"{post.IPaddress}:{post.Port}",
                            Uuid = uuid
                        };
                        if (!_context.ModbusConnInfo.Any(s => s.Uuid == uuid))
                        {
                            _context.ModbusConnInfo.Add(connInfo);
                        }
                        else
                        {
                            connInfo = _context.ModbusConnInfo.FirstOrDefault(s => s.Uuid == connInfo.Uuid);
                        }
                        await _context.SaveChangesAsync();


                        string protocolName = post.ProtocolName.Empty() ? post.DeviceType : post.ProtocolName;
                        var nodes = _context.ModbusNodeInfo.Where(q => q.ProtocolName == protocolName)
                            .Select(s => new { s.Id, s.ProtocolName }).AsNoTracking().ToList();
                        var pairs = nodes.Select(s => new ModbusDevicePair
                        {
                            ConnUuid = connInfo.Uuid,
                            DeviceId = newDevice.Id,
                            BusId = post.BusId,
                            ConnectionId = connInfo.Id,
                            NodeIndex = Convert.ToInt16(post.NodeIndex),
                            NodeId = s.Id,
                            ProtocolName = s.ProtocolName,
                        }).ToList();
                        _context.ModbusDevicePair.AddRange(pairs);
                        await _context.SaveChangesAsync();
                        break;
                    default:
                        _logger.LogError("错误的设备类型" + newDevice.DeviceType);
                        break;
                }
                await tran.CommitAsync();
                return newDevice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                await tran.RollbackAsync();
                return null;
            }
        }

    }
    public async Task SetCameraToRtspSimpleServer(CreateDeviceRequest post, BklDeviceMetadata meta, string channel, string rtspserver = "127.0.0.1:8888")
    {
        var resp1 = client.PostAsync($"http://{_config.RtspServer ?? rtspserver}/v1/config/paths/edit/did{meta.Id}{channel}", new StringContent(JsonSerializer.Serialize(
              new
              {
                  source = $"rtsp://{post.UserName}:{post.Password}@{post.IPaddress}:554/Streaming/Channels/{channel}?transportmode=unicast",
                  sourceProtocol = "automatic",
                  sourceOnDemand = true
              }), System.Text.Encoding.UTF8, "application/json"));

        try
        {
            await resp1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }
    public async Task<HttpResponseMessage> AddCameraToRtspSimpleServer(CreateDeviceRequest post, BklDeviceMetadata meta, string channel, string rtspserver = "127.0.0.1:8888")
    {
        var resp1 = client.PostAsync($"{_config.RtspServer ?? rtspserver}/v1/config/paths/add/did{meta.Id}{channel}", new StringContent(JsonSerializer.Serialize(
              new
              {
                  source = $"rtsp://{post.UserName}:{post.Password}@{post.IPaddress}:554/Streaming/Channels/{channel}?transportmode=unicast",
                  sourceProtocol = "automatic",
                  sourceOnDemand = true
              }), System.Text.Encoding.UTF8, "application/json"));

        try
        {
            return await resp1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        return default(HttpResponseMessage);
    }

    // 0          1         2           3         4         5                           6       7        
    //ThermalCamera groupName probeName devicename fullpath  camUser                   camPswd     camIP
    //  0               1         2         3           4                       5                    6                        
    //DGA           groupName probeName devicename fullpath  udp://192.168.31.100:8888 modbus://03/21/1501/1
    //BandageSensor groupName probeName devicename fullpath  udp://192.168.31.100:8888 modbus://03/21/1501/1
    public async Task<ImportResult> ImportDevice(StreamReader sr, string splitter = "\t")
    {
        Dictionary<string, object> dic = new Dictionary<string, object>();
        List<BklDeviceMetadata> importDevs = new List<BklDeviceMetadata>();

        using (var tran = _context.Database.BeginTransaction())
        {
            try
            {
                var cols = (await sr.ReadLineAsync()).Split(splitter);
                var facname = _context.BklFactory.FirstOrDefault(q => q.Id == _user.factoryId);
                if (facname == null)
                {
                    facname = new BklFactory
                    {
                        Country = "中国",
                        Province = "河南",
                        ProvinceCode = "",
                        City = "",
                        CityCode = "",
                        Distribute = "",
                        DistributeCode = "",
                        CreatorId = _user.userId,
                        FactoryName = "默认",
                        Id = _user.factoryId,
                    };
                    _context.BklFactory.Add(facname);
                    _context.SaveChanges();
                }
                while (cols != null && cols.Length != 0)
                {
                    if (string.IsNullOrEmpty(cols[0]))
                    {
                        Console.WriteLine("error add row " + string.Join(" ", cols));
                        break;
                    }
                    Console.WriteLine("add row " + string.Join(" ", cols));
                    var facilityType = cols[0];

                    cols = cols.Skip(1).ToArray();
                    var fname = cols[4].Split('/')[0];
                    var faci = _context.BklFactoryFacility.Where(q => q.FacilityType == facilityType && q.Name == fname).FirstOrDefault();
                    if (faci == null)
                    {
                        faci = new BklFactoryFacility
                        {
                            Name = fname,
                            CreatorId = _user.userId,
                            FactoryId = facname.Id,
                            FactoryName = facname.FactoryName,
                            FacilityType = facilityType,
                            CreatorName = _user.name
                        };
                        _context.BklFactoryFacility.Add(faci);
                        _context.SaveChanges();
                        //faci = context.BklFactoryFacility.Where(q => q.FacilityType == facilityType && q.Name == fname).FirstOrDefault();
                    }
                    BklDeviceMetadata deviceMeta = null;
                    ModbusConnInfo connInfo = null;
                    switch (cols[0].ToLower())
                    {
                        case "thermalcamera":
                            deviceMeta = NewThermalCameraDevice(_user, cols);

                            break;
                        case "dga":
                            deviceMeta = NewModbusDevice(_user, cols, "DGA", out connInfo);

                            break;
                        case "bandagesensor":
                            deviceMeta = NewModbusDevice(_user, cols, "BandageSensor", out connInfo);
                            break;
                        case "ptdetector":
                            deviceMeta = this.NewModbusDevice(_user, cols.ToArray(), "PTDetector", out connInfo);
                            break;
                        default:
                            break;
                    }
                    var md5 = SecurityHelper.Get32MD5($"{facilityType}{deviceMeta.DeviceType}{deviceMeta.FullPath}");
                    deviceMeta.DeviceMetadata = JsonSerializer.Serialize(new { s = md5, k = md5.BKDRHash() });
                    deviceMeta.FactoryName = facname.FactoryName;
                    deviceMeta.FacilityId = faci.Id;
                    _context.BklDeviceMetadata.Add(deviceMeta);
                    switch (cols[0].ToLower())
                    {

                        case "thermalcamera":
                            var dev = new DeviceConnectionString
                            {
                                local = $"rtsp://{cols[5]}:{cols[6]}@{cols[7]}:554/Streaming/Channels/101?transportmode=unicast",
                                remote = $"rtsp://{cols[5]}:{cols[6]}@{cols[7]}:554/Streaming/Channels/201?transportmode=unicast",
                                proto = $"hkthermal://{cols[5]}/{cols[6]}/{cols[7]}/8000/2"
                            };
                            var thermalInfo = dev.GetThermalInfo();
                            dic.Add($"{deviceMeta.MacAddress}visible", new { url = thermalInfo.visibleUrl, on_demand = true });
                            dic.Add($"{deviceMeta.MacAddress}", new { url = thermalInfo.thermalUrl, on_demand = true });
                            if (!_context.BklThermalCamera.Any(s => s.Ip == thermalInfo.ip))
                            {
                                _context.BklThermalCamera.Add(new BklThermalCamera
                                {
                                    Ip = thermalInfo.ip,
                                    Port = thermalInfo.port,
                                    Account = thermalInfo.username,
                                    Password = thermalInfo.password,
                                    DeviceId = deviceMeta.Id,
                                    UserId = deviceMeta.CreatorId,
                                });
                                _context.SaveChanges();
                            }
                            break;
                        case "dga":
                        case "bandagesensor":
                        case "ptdetector":

                            var conn1 = BklDeviceMetadata.GetConnection(JsonSerializer.Serialize(new
                            {
                                local = cols[5],//tcp://ip:port
                                remote = $"tcp://127.0.0.1:8899",
                                proto = cols[6],
                            }));
                            var modbusinfo = conn1.GetModbusInfo();

                            if (!_context.ModbusConnInfo.Any(s => s.Uuid == connInfo.Uuid))
                            {
                                _context.ModbusConnInfo.Add(connInfo);
                            }
                            else
                            {
                                connInfo = _context.ModbusConnInfo.FirstOrDefault(s => s.Uuid == connInfo.Uuid);
                            }
                            _context.SaveChanges();
                            var nodeInfos = _context.ModbusNodeInfo.Where(q => q.ProtocolName == cols[0]);
                            var pairs = nodeInfos.Select(s => new ModbusDevicePair
                            {
                                DeviceId = deviceMeta.Id,
                                BusId = modbusinfo.busid,
                                ConnectionId = connInfo.Id,
                                NodeIndex = Convert.ToInt16(modbusinfo.dataOffset),
                                NodeId = s.Id,
                                ProtocolName = s.ProtocolName,
                            }).ToList();
                            _context.ModbusDevicePair.AddRange(pairs);
                            _context.SaveChanges();
                            break;
                        default:
                            break;
                    }

                    _context.SaveChanges();
                    importDevs.Add(deviceMeta);
                    cols = (await sr.ReadLineAsync())?.Split('\t');
                }
                tran.Commit();
                _context.SaveChanges();


            }
            catch (Exception ex)
            {

                tran.Rollback();
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        return new ImportResult
        {
            streams = dic.Count == 0 ? null : new
            {
                server = new { http_port = "0.0.0.0:8083" },
                streams = dic
            },
            devices = importDevs
        };
    }

    public BklDeviceMetadata NewThermalCameraDevice(LogonUser user, string[] cols)
    {
        var ps = cols[4].Split('/');
        BklDeviceMetadata meta = new BklDeviceMetadata
        {
            GroupName = cols[1],
            ProbeName = cols[2],
            DeviceType = "ThermalCamera",
            DeviceName = cols[3],
            PDeviceName = "#",
            PDeviceType = "#",
            PathType = "#",
            FullPath = cols[4],
            Path1 = ps[0],
            Path2 = ps.Length >= 2 ? ps[1] : "#",
            Path3 = ps.Length >= 3 ? ps[2] : "#",
            Path4 = ps.Length >= 4 ? ps[3] : "#",
            Path5 = ps.Length >= 5 ? ps[4] : "#",
            Path6 = ps.Length >= 6 ? ps[5] : "#",
            MacAddress = cols[8],
            ConnectionString = JsonSerializer.Serialize(new
            {
                visible = $"rtsp://{cols[5]}:{cols[6]}@{cols[7]}:554/Streaming/Channels/101?transportmode=unicast",
                thermal = $"rtsp://{cols[5]}:{cols[6]}@{cols[7]}:554/Streaming/Channels/201?transportmode=unicast",
            }),
            AreaName = "",
            ConnectionType = "rtsp",
            DeviceMetadata = "#",
            FactoryName = "",
            FactoryId = user.factoryId,
            CreatorId = user.userId,
        };

        return meta;
    }
    public BklDeviceMetadata NewModbusDevice(LogonUser user, string[] cols, string devType, out ModbusConnInfo conn)
    {
        conn = new ModbusConnInfo
        {
            ModbusType = cols[6].Split(':')[0],
            ConnType = cols[5].Split(':')[0],
            ConnStr = cols[5].Split('/')[2],
        };
        var ps = cols[4].Split('/');
        BklDeviceMetadata meta = new BklDeviceMetadata
        {
            GroupName = cols[1],
            ProbeName = cols[2],
            DeviceType = devType,
            DeviceName = cols[3],
            PDeviceName = "#",
            PDeviceType = "#",
            PathType = "#",
            FullPath = cols[4],
            Path1 = ps[0],
            Path2 = ps.Length >= 2 ? ps[1] : "#",
            Path3 = ps.Length >= 3 ? ps[2] : "#",
            Path4 = ps.Length >= 4 ? ps[3] : "#",
            Path5 = ps.Length >= 5 ? ps[4] : "#",
            Path6 = ps.Length >= 6 ? ps[5] : "#",
            MacAddress = cols[7],
            ConnectionString = $"{conn.ModbusType}://{conn.ConnStr}",

            AreaName = "",
            ConnectionType = cols[6].Split(':')[0],
            DeviceMetadata = "#",
            FactoryName = "",
            FactoryId = user.factoryId,
            CreatorId = user.userId,
        };

        conn = new ModbusConnInfo
        {
            ModbusType = cols[6].Split(':')[0],
            ConnType = cols[5].Split(':')[0],
            ConnStr = cols[5].Split('/')[2],
        };
        meta.ConnectionString = $"{conn.ModbusType}://{conn.ConnStr}";
        conn.Uuid = SecurityHelper.Get32MD5($"{conn.ModbusType}{conn.ConnType}{conn.ConnStr}");
        return meta;
    }
}
