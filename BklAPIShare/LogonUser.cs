using Bkl.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

public class LogonUser
{
    private HttpContext httpcontext;
    public LogonUser(IHttpContextAccessor accessor)
    {
        httpcontext = accessor.HttpContext;
        var Request = accessor.HttpContext.Request;
        var users = Request.HttpContext.User;
        var claims = users.Identity as ClaimsIdentity;
        //var dic = claims.Claims.ToDictionary(p => p.Type, p => p.Value);
        Dictionary<string, string> dic = new Dictionary<string, string>();
        foreach (var item in claims.Claims.GroupBy(q => q.Type))
        {
            dic.Add(item.Key, string.Join(",", item.Select(s => s.Value)));
        }
        SetLoginUserInstance(Request, dic, this);
    }
    public LogonUser()
    {

    }
    public bool IsAdmin() => httpcontext.User.HasClaim(ClaimTypes.Role, "admin");
    public bool IsFAdmin(long facId) => httpcontext.User.HasClaim(ClaimTypes.Role, "admin") && this.factoryId == facId;
    public string name { get; set; }
    public string account { get; set; }
    public long userId { get; set; }
    public long factoryId { get; set; }
    public string role { get; set; }
    public string position { get; set; }
    public string contact { get; set; }

    public long[] GetPermittedId(BklDbContext context, string targetType, string access = "allow", string control = "read")
    {
        var roles = this.role.Split(",");
        var facIds = context.BklPermission.Where(s => s.Access == access && s.TargetType == targetType && s.Control == control && s.FactoryId == this.factoryId)
          .ToList()
          .Where(s => roles.Contains(s.Role))
          .Select(s => s.TargetId)
          .ToArray();
        return facIds;
    }
    public bool HasPermission(BklDbContext context, params (string targetType, long targetId, string access, string control)[] permissions)
    {
        var roles = this.role.Split(",");
        var p1 = permissions[0];
        var query1 = context.BklPermission.Where(s => s.Access == p1.access && s.TargetId == p1.targetId && s.TargetType == p1.targetType && s.Control == p1.control && s.FactoryId == this.factoryId);
        for (int i = 1; i < permissions.Length; i++)
        {
            p1 = permissions[i];
            query1 = query1.Union(context.BklPermission.Where(s => s.Access == p1.access && s.TargetId == p1.targetId && s.TargetType == p1.targetType && s.Control == p1.control && s.FactoryId == this.factoryId));
        }
        var facIds = query1.Select(s => s.Role).ToArray();
        return roles.SequenceEqual(facIds);
    }
    private static void SetLoginUserInstance(HttpRequest request, Dictionary<string, string> dic, LogonUser user)
    {
        var map = JwtSecurityTokenHandler.DefaultInboundClaimTypeMap;

        foreach (var item in user.GetType().GetProperties())
        {
            var claimType = map.ContainsKey(item.Name) ? map[item.Name] : item.Name;
            if (!dic.ContainsKey(claimType))
                continue;
            if (item.PropertyType == typeof(int))
            {
                item.GetSetMethod().Invoke(user, new object[] { int.TryParse(dic[claimType], out var ival) ? ival : 0 });
            }
            else if (item.PropertyType == typeof(long))
            {
                item.GetSetMethod().Invoke(user, new object[] { long.TryParse(dic[claimType], out var ival) ? ival : 0 });
            }
            else
            {
                item.GetSetMethod().Invoke(user, new object[] { dic[claimType] });
            }
        }
        //if (request.Query.TryGetValue("factoryId", out var val))
        //{
        //    if (long.TryParse(val, out var fid))
        //    {
        //        user.factoryId = fid;
        //    }
        //}
    }
}