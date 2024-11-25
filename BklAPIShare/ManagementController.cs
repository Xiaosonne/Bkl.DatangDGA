using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bkl.Infrastructure;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Bkl.Models.Std;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using StackExchange.Redis;
using System.Dynamic;
using System.IO;
using DocumentFormat.OpenXml.Bibliography;
using Bkl.ESPS.Bussiness;


[ApiController]
[Authorize]
[Route("[controller]")]
public class ManagementController : Controller
{
    BklDbContext context;
    private ILogger<ManagementController> logger;

    public ManagementController(BklDbContext context, ILogger<ManagementController> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("getToken")]
    public IActionResult GetToken([FromServices] BklDbContext context, [FromServices] BklConfig config, [FromBody] LoginRequest req)
    {
        var passenc = req.password.Sha256();
        logger.LogInformation($"UserLogin {req.account} {req.password} {passenc}");
        var user = context.BklFactoryUser.Where(q => q.Account == req.account && q.Password == passenc).FirstOrDefault();
        if (user == null)
        {
            return new JsonResult(ReturnResultCodeExtension.ReturnResultCode.UserNotFound.ToReturnObject());
        }
        List<Claim> lisClaims = new List<Claim>();
        lisClaims.Add(new Claim("name", user.UserName));
        lisClaims.Add(new Claim("account", user.Account));
        lisClaims.Add(new Claim("userId", user.Id.ToString()));
        lisClaims.Add(new Claim("factoryId", user.FactoryId.ToString()));

        var factIds = new HashSet<long>();
        factIds.Add(user.FactoryId);

        var createdFactIds = context.BklFactory.Where(s => s.CreatorId == user.Id).Select(q => q.Id).ToList();
        createdFactIds.ForEach(item => factIds.Add(item));

        lisClaims.Add(new Claim("factoryIds", String.Join(",", factIds)));
        foreach (string role in user.Roles.Split(","))
        {
            lisClaims.Add(new Claim("role", role));
        }
        foreach (string role in user.Positions.Split(","))
        {
            lisClaims.Add(new Claim("position", role));
        }
        var contacts = context.BklNotificationContact.Where(q => q.UserId == user.Id).ToList();
        foreach (var contact in contacts)
        {
            lisClaims.Add(new Claim("contact", contact.ContactType + ":" + contact.ContactInfo));
        }

        var sing = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.AuthConfig.Secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            config.AuthConfig.Issuer,
            config.AuthConfig.Audience,
            lisClaims.ToArray(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(100), sing);
        var tokenHandler = new JwtSecurityTokenHandler()
        {
            InboundClaimTypeMap = new Dictionary<string, string>()
        };
        var tokenJson = tokenHandler.WriteToken(token);
        return new JsonResult(new
        {
            accessToken = tokenJson
        });
    }



    [HttpGet("profile")]
    public IActionResult Profile([FromServices] LogonUser user)
    {
        if (context.BklFactoryUser.Any(s => s.Id == user.userId && s.FactoryId == user.factoryId))
            return new JsonResult(user);
        return new ForbidResult();
    }

    [HttpPut("factory")]
    public IActionResult UpdateFactory([FromServices] IRedisClient redis, [FromServices] LogonUser user, [FromBody] CreateFactoryRequest request)
    {
        BklFactory fac = context.BklFactory.Where(s => s.Id == request.FactoryId).FirstOrDefault();

        if (context.BklFactory.Any(s => s.Id != request.FactoryId && s.FactoryName == request.FactoryName))
        {
            return new JsonResult(new { error = 1, errorDetail = $"名为\"{request.FactoryName}\"的厂区已存在，请输入其他名称" });
        }
        fac.FactoryName = request.FactoryName;
        //fac.Province = request.Pcas.FirstOrDefault(s => s.Type == "province").Value;
        //fac.ProvinceCode = request.Pcas.FirstOrDefault(s => s.Type == "province").NodeId.ToString();
        //fac.City = request.Pcas.FirstOrDefault(s => s.Type == "city").Value;
        //fac.CityCode = request.Pcas.FirstOrDefault(s => s.Type == "city").NodeId.ToString();
        //fac.Distribute = request.Pcas.FirstOrDefault(s => s.Type == "area").Value;
        //fac.DistributeCode = request.Pcas.FirstOrDefault(s => s.Type == "area").NodeId.ToString();
        //var street = request.Pcas.FirstOrDefault(s => s.Type == "street");
        //if (street != null)
        //{
        //    fac.Distribute = fac.Distribute + "," + street.Value;
        //    fac.DistributeCode = fac.DistributeCode + "," + street.NodeId;
        //}
        //fac.Distribute = fac.Distribute + "," + request.DetailAddress;

        try
        {
            if (request.Metadata != null)
            {
                redis.SetRangeInHash($"FactoryMeta",
                 request.Metadata.ToDictionary(s => $"{fac.Id}-{s.Name}",
                 s => (RedisValue)s.Value).ToList());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }

        context.SaveChanges();
        return new JsonResult(new { error = 0 });
        return Ok();
    }

    /// <summary>
    /// 全量更新 包括删除 修改 创建
    /// </summary>
    /// <param name="user"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("create-factory")]
    public async Task<IActionResult> CreateFactories([FromServices] IRedisClient redis, [FromServices] LogonUser user, [FromBody] CreateFactoryRequest request)
    {
        using (var tran = context.Database.BeginTransaction())
        {
            try
            {
                BklFactory fac = null;
                fac = context.BklFactory.Where(s => s.FactoryName == request.FactoryName).FirstOrDefault();
                if (fac != null)
                {
                    fac.Province = request.Pcas.FirstOrDefault(s => s.Type == "province").Value;
                    fac.ProvinceCode = request.Pcas.FirstOrDefault(s => s.Type == "province").NodeId.ToString();
                    fac.City = request.Pcas.FirstOrDefault(s => s.Type == "city").Value;
                    fac.CityCode = request.Pcas.FirstOrDefault(s => s.Type == "city").NodeId.ToString();
                    fac.Distribute = request.Pcas.FirstOrDefault(s => s.Type == "area").Value;
                    fac.DistributeCode = request.Pcas.FirstOrDefault(s => s.Type == "area").NodeId.ToString();
                    var street = request.Pcas.FirstOrDefault(s => s.Type == "street");
                    if (street != null)
                    {
                        fac.Distribute = fac.Distribute + "," + street.Value;
                        fac.DistributeCode = fac.DistributeCode + "," + street.NodeId;
                    }
                    fac.Distribute = fac.Distribute + "," + request.DetailAddress;
                    context.SaveChanges();
                    tran.Commit();
                    return new JsonResult(new { error = 1, errorDetail = $"名为\"{request.FactoryName}\"的厂区已存在，请输入其他名称" });
                }
                fac = new BklFactory
                {
                    FactoryName = request.FactoryName,
                    Country = "中国",
                    Province = request.Pcas.FirstOrDefault(s => s.Type == "province").Value,
                    ProvinceCode = request.Pcas.FirstOrDefault(s => s.Type == "province").NodeId.ToString(),
                    City = request.Pcas.FirstOrDefault(s => s.Type == "city").Value,
                    CityCode = request.Pcas.FirstOrDefault(s => s.Type == "city").NodeId.ToString(),
                    Distribute = request.Pcas.FirstOrDefault(s => s.Type == "area").Value,
                    DistributeCode = request.Pcas.FirstOrDefault(s => s.Type == "area").NodeId.ToString(),
                };
                {
                    var street = request.Pcas.FirstOrDefault(s => s.Type == "street");
                    if (street != null)
                    {
                        fac.Distribute = fac.Distribute + "," + street.Value;
                        fac.DistributeCode = fac.DistributeCode + "," + street.NodeId;
                    }
                }
                fac.Distribute = fac.Distribute + "," + request.DetailAddress;
                fac.CreatorId = user.userId;
                context.BklFactory.Add(fac);
                await context.SaveChangesAsync();
                try
                {
                    if (request.Metadata != null)
                    {
                        redis.SetRangeInHash($"FactoryMeta",
                         request.Metadata.ToDictionary(s => $"{fac.Id}-{s.Name}",
                         s => (RedisValue)s.Value).ToList());
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }
                var grant = context.BklUserGranted.Where(s => s.FactoryId == fac.Id && s.UserId == user.userId).FirstOrDefault();
                if (grant == null)
                {
                    context.BklUserGranted.Add(new BklUserGranted
                    {
                        FactoryId = fac.Id,
                        FactoryName = fac.FactoryName,
                        FacilityName = "#",
                        FacilityId = 0,
                        Roles = user.role,
                        UserId = user.userId,
                        UserName = user.name,
                        CreatorId = user.userId,
                    });
                    await context.SaveChangesAsync();
                }
                tran.Commit();

                return new JsonResult(fac);
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine(ex);
                return new JsonResult(new { error = 1, errorDetail = ex.ToString() });
            }
        }

    }

    [HttpGet("factories")]
    public IActionResult ListFactories([FromServices] IRedisClient redis, [FromServices] LogonUser user)
    {

        var filter = context.BklFactory.Where(s => 1 == 1);

        if (!user.IsAdmin())
        {
            var facIds = user.GetPermittedId(context, PermissionConstants.TargetFactory);
            filter = filter.Where(s => facIds.Contains(s.Id));
        }
        var arr = filter.ToList();
        List<BklFactory> facs = new List<BklFactory>();
        foreach (var name in FactoryOrders.orders)
        {
            var fac = arr.FirstOrDefault(s => s.FactoryName.Contains(name));
            if (fac != null)
            {
                facs.Add(fac);
                arr.Remove(fac);
            }
        }
        //不要其它场站的数据

        //facs.AddRange(arr);
        return Json(facs);
    }

    [HttpPost("batch-create-facility")]
    public async Task<IActionResult> BatchCreateFacility([FromServices] LogonUser user, [FromServices] IRedisClient redis, string splitter = "\t")
    {
        StreamReader sr = new StreamReader(this.Request.Body);
        var headers = (await sr.ReadLineAsync()).Split(splitter);
        List<Dictionary<string, string>> lis = new List<Dictionary<string, string>>();
        while (true)
        {
            var strLine = (await sr.ReadLineAsync());
            if (string.IsNullOrEmpty(strLine))
                break;
            var line1 = strLine.Split(splitter);
            var datasource = new Dictionary<string, string>();
            int i = 0;
            foreach (var col in line1)
            {
                datasource.Add(headers[i++], col);
            }
            lis.Add(datasource);
        }
        var facs = new List<BklFactory>();
        using (var tran = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
        {
            foreach (var item in lis)
            {
                try
                {
                    var fac = facs.Where(s => s.FactoryName == item["所属电厂"]).FirstOrDefault();
                    if (fac == null)
                    {
                        fac = context.BklFactory.Where(s => s.FactoryName == item["所属电厂"]).FirstOrDefault();
                        facs.Add(fac);
                    }
                    if (fac == null)
                    {
                        fac = new BklFactory
                        {
                            FactoryName = item["所属电厂"],
                            Country = "#",
                            Province = "#",
                            ProvinceCode = "#",
                            City = "#",
                            CityCode = "#",
                            Distribute = "#",
                            DistributeCode = "#",
                            CreatorId = user.userId,
                        };
                        context.BklFactory.Add(fac);
                        await context.SaveChangesAsync();
                    }
                    var facility = context.BklFactoryFacility.FirstOrDefault(s => s.FactoryId == fac.Id && s.Name == item["名称"]);
                    if (facility == null)
                    {
                        facility = new BklFactoryFacility
                        {
                            CreatorId = user.userId,
                            FacilityType = item["类型"],
                            FactoryId = fac.Id,
                            FactoryName = item["所属电厂"],
                            Name = item["名称"],
                            CreatorName = user.name,
                            GPSLocation = item.TryGetValue("GPS", out var gps) ? gps : ""
                        };
                        context.BklFactoryFacility.Add(facility);
                        await context.SaveChangesAsync();
                    }
                    var keyvalue = item.Where(s => s.Key.StartsWith("KEY-")).ToList();
                    var rediskv = item.Where(q => q.Key.StartsWith("KEY-"))
                      .Select(p =>
                      {
                          var realkey = p.Key.Substring(4);
                          return new KeyValuePair<string, RedisValue>(realkey, p.Value);
                      }).ToList();
                    redis.Remove($"FacilityMeta:{facility.Id}");
                    redis.SetRangeInHash($"FacilityMeta:{facility.Id}", rediskv);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    tran.Rollback();
                    return Json(new { error = 1, errorMsg = ex.ToString() });
                }
            }
            tran.Commit();
        }
        return Json(new { error = 0 });
    }

    [HttpPost("create-facility")]
    public async Task<IActionResult> CreateFacility([FromServices] LogonUser user, [FromServices] IRedisClient redis, CreateFacilityRequest req)
    {
        var facility = new BklFactoryFacility
        {
            CreatorId = user.userId,
            FacilityType = req.FacilityType,
            FactoryId = req.FactoryId,
            FactoryName = req.FactoryName,
            Name = req.FacilityName,
            CreatorName = user.name,
            GPSLocation = req.GPS == null ? "[]" : req.GPS
        };

        var oldFacility = context.BklFactoryFacility.FirstOrDefault(s => s.FactoryId == facility.FactoryId && s.Name == facility.Name);
        if (oldFacility != null)
        {
            oldFacility.Name = facility.Name;
            oldFacility.GPSLocation = facility.GPSLocation;
            await context.SaveChangesAsync();
        }
        else
        {
            context.BklFactoryFacility.Add(facility);
            await context.SaveChangesAsync();
        }
        if (req.Metadata != null)
        {
            redis.SetRangeInHash($"FacilityMeta:{facility.Id}", req.Metadata.Select(s => new KeyValuePair<string, RedisValue>(s.Name, (RedisValue)s.Value)));
        }
        return new JsonResult(facility);
    }

    [HttpGet("facilities")]
    public IActionResult ListFacilities([FromServices] LogonUser user, long factoryId)
    {

        var arr = context.BklFactoryFacility.Where(p => factoryId == 0 || p.FactoryId == factoryId).ToList();
        if (user.IsAdmin() || user.IsFAdmin(factoryId))
        {
            return Json(arr);
        }
        var faciIds = user.GetPermittedId(context, PermissionConstants.TargetFacility);
        return Json(arr.Where(s => faciIds.Contains(s.Id)).ToArray());
    }

    [HttpGet("facility/{facilityId}/devices")]
    public IActionResult ListFacilityDevices(long facilityId)
    {
        var arr = context.BklDeviceMetadata.Where(p => p.FacilityId == facilityId).ToList().OrderBy(s => s.DeviceMetadata).ToList();
        return new JsonResult(arr);
    }
    [HttpGet("devices")]
    public IActionResult ListDevices([FromServices] LogonUser user, [FromQuery] long factoryId = 0, [FromQuery] long facilityId = 0)
    {
        var arr = context.BklDeviceMetadata.Where(q => (factoryId == 0 || q.FactoryId == factoryId) && (facilityId == 0 || q.FacilityId == facilityId)).ToList();
        List<BklDeviceMetadata> devices = new List<BklDeviceMetadata>();
        foreach (var order in FactoryOrders.orders)
        {
            var devs = arr.Where(s => s.FactoryName.Contains(order)).OrderBy(s => s.DeviceName).ToList();
            devices.AddRange(devs);
        }
        return new JsonResult(devices);
    }
}