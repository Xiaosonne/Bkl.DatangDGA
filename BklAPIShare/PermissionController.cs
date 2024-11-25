using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;



[ApiController]
[Authorize]
[Route("[controller]")]
public class PermissionController : Controller
{
    BklDbContext context;
    private ILogger<ManagementController> logger;

    public PermissionController(BklDbContext context, ILogger<ManagementController> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    [HttpPost, Authorize(Roles = "admin")]
    public async Task<ActionResult> Post([FromServices] LogonUser user)
    {
        var results = await (new StreamReader(this.Request.Body)).ReadToEndAsync();
        var pers = JsonSerializer.Deserialize<BklPermission[]>(results, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var erroor = pers.Where(s => s.Role != "admin" && (
            s.FactoryId == 0 ||
            (s.TargetType == "factory" && s.TargetId == 0)
        )).ToList();
        if (erroor.Count > 0)
            return Json(erroor.CreateResponse(1));
        foreach (var per in pers)
        {
            per.CreatorId = user.userId;
        }

        context.BklPermission.AddRange(pers);
        await context.SaveChangesAsync();
        return Json(pers.CreateResponse());
    }

    [HttpPut("user/{userId}/roles"), Authorize(Roles = "admin,fadmin")]
    public async Task<ActionResult> SetUserRoles([FromServices] LogonUser curuser, [FromRoute] long userId,
        [FromBody] string[] roles)
    {
        var user = await context.BklFactoryUser.Where(s => s.Id == userId).FirstOrDefaultAsync();
        if (curuser.IsAdmin() || curuser.IsFAdmin(user.FactoryId))
        {
            var dbRolos = context.BklPermission.Where(s => s.FactoryId == 0 || s.FactoryId == user.FactoryId)
                .Select(s => s.Role).Distinct().ToArray();
            var setRoles = dbRolos.Intersect(roles).ToList();
            if (!curuser.IsAdmin())
                setRoles.Remove("admin");
            user.Roles = String.Join(",", setRoles);
            await context.SaveChangesAsync();
        }

        return Json(user.CreateResponse());
    }

    //获取用户的权限
    [HttpGet("user/{userId}/roles"), Authorize(Roles = "admin,fadmin")]
    public async Task<IActionResult> GetUserPermissions([FromServices] LogonUser loginUser,
        [FromRoute] long userId = 0)
    {
        if (userId > 0)
        {
            var user = await context.BklFactoryUser.Where(s => s.Id == userId).FirstOrDefaultAsync();
            var userfactory = user.FactoryId;

            var pers = context.BklPermission.Where(s => s.FactoryId == 0 || s.FactoryId == userfactory).ToList();
            var rolos = user.Roles.Split(",");
            if (rolos.Length > 0)
                pers = pers.Where(s => rolos.Contains(s.Role)).ToList();
            var resp = pers.GroupBy(s => s.Role).Select(s => new { role = s.Key, permissions = s.ToArray() })
                .CreateResponse();
            return Json(resp);
        }

        return BadRequest("user id > 0");
    }


    [HttpGet("factory/{factoryId}/roles"), Authorize(Roles = "admin,fadmin")]
    public IActionResult ListRoles([FromServices] LogonUser user, [FromRoute] long factoryId)
    {
        bool isadmin = user.IsAdmin();
        var lis = context.BklPermission.Where(s => isadmin && s.FactoryId == 0 || s.FactoryId == factoryId)
            .ToList();
        var resp = lis.GroupBy(s => s.Role).Select(s => new { role = s.Key, permissions = s.ToArray() })
            .CreateResponse();
        return Json(resp);
    }
}