using Bkl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Bkl.Infrastructure;
using Microsoft.Extensions.Logging;

[ApiController]
[Authorize]
[Route("[controller]")]
public class UserController : Controller
{
    BklDbContext _context;
    private ILogger<ManagementController> _logger;

    public UserController(BklDbContext context, ILogger<ManagementController> logger)
    {
        this._context = context;
        this._logger = logger;
    }

    [Authorize(Roles = "admin,fadmin")]
    [HttpPost]
    public IActionResult CreateUser([FromServices] BklDbContext context, [FromServices] LogonUser logon,
        [FromBody] RegistryRequest req)
    {
        var user = new BklFactoryUser
        {
            UserName = req.username,
            Account = req.account,
            Password = req.password.Sha256(),
            FactoryId = req.factoryId,
            CreatorId = logon.userId, 
            Roles = "default",
            Positions = req.positions
        };
        if (!context.BklFactoryUser.Any(s => s.Account == req.account))
        {
            context.BklFactoryUser.Add(user);
            context.SaveChanges();
            return Json(user.CreateResponse());
        }
        else
        {
            return Json(user.CreateResponse(1, "账号已存在"));
        }
    }

    [HttpPut("{userId}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateUser([FromServices] LogonUser user,
        [FromRoute] long userId,
        [FromBody] UpdateUserRequest request)
    {
        var user1 = _context.BklFactoryUser.FirstOrDefault(s => s.Id == request.UserId);
        if (user1 == null)
            return Json(new GeneralResponse() { error = 1, msg = "用户不存在" });
        if (request.UserName.NotEmpty() && !request.UserName.Equals(user1.UserName))
            user1.UserName = request.UserName;
        if (request.Positions.NotEmpty() && !request.Positions.Equals(user1.Positions))
            user1.Positions = request.Positions;
        if (request.Password.NotEmpty())
            user1.Password = request.Password.Sha256();
        await _context.SaveChangesAsync();
        return Json(user1.CreateResponse());
    }

    [HttpGet("factory/{factoryId}/users"), Authorize(Roles = "admin,fadmin")]
    public IActionResult ListUsers([FromServices] LogonUser user, [FromRoute] long factoryId = 0)
    {
        if (user.IsAdmin())
        {
            if (factoryId != 0)
                return Json(_context.BklFactoryUser.Where(s => s.FactoryId == factoryId).ToList());
            return Json(new int[] { });
        }

        if (user.HasPermission(_context, ("factory", factoryId, "allow", "read")))
        {
            var userIds = user.GetPermittedId(_context, "user");
            if (factoryId != 0)
                return Json(_context.BklFactoryUser.Where(s => userIds.Contains(s.Id) && s.FactoryId == factoryId)
                    .ToList());
            return Json(new int[] { });
        }

        return Forbid();
    }

    [HttpDelete(), Authorize(Roles = "admin,fadmin")]
    public IActionResult DeleteUser([FromServices] LogonUser user, long delUserId)
    {
        var delUser = _context.BklFactoryUser.FirstOrDefault(s => s.Id == delUserId);
        if (delUser.CreatorId == user.userId)
        {
            if (user.role == "admin" || user.role == "fadmin" && delUser.Roles == "user")
            {
                var grants = _context.BklUserGranted.Where(s => s.UserId == delUserId).ToList();
                _context.BklUserGranted.RemoveRange(grants);
                _context.BklFactoryUser.Remove(delUser);
                _context.SaveChanges();
                return Json(new GeneralResponse { error = 0, success = true });
            }
        }

        return Json(new GeneralResponse { error = 1, success = false, msg = "该账户由其他人创建，无法删除。" });
    }
}