using Bkl.Dst.Interfaces;
using Bkl.Models;
using Bkl.Models.Std;
using CommandLine;
using DocumentFormat.OpenXml.Vml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.ESPS.Controllers
{
    public class ContactView
    {
        public int Id { get; set; }
        public string ContactType { get; set; }
        public string ContactInfo { get; set; }
        public string ContactName { get; set; }
    }



    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public partial class RulesController : Controller
    {
        [HttpGet]
        public object Get([FromServices] BklDbContext context1, [FromServices] LogonUser user, long deviceId = 0, string deviceType = null)
        {
            return context1.BklAnalysisRule.Where(p => (deviceType == null || p.DeviceType == deviceType) && (deviceId == 0 || p.DeviceId == deviceId)).ToList();
        }

        [HttpPost("set-thermal-linkage")]
        public IActionResult SetThermalRule([FromServices] BklDbContext context, [FromServices] LogonUser user, [FromBody] CreateThermalLinkageAction rule)
        {

            var linkid = SnowId.IdGenInstance.NewLong();
            var post = new BklAnalysisRule
            {
                StatusName = rule.AreaName,
                AttributeId = rule.AreaId,
                PairId = 0, 
                Id = SnowId.NextId(),
                CreatorId = user.userId,
                DeviceId = rule.DeviceId,
                DeviceType = rule.DeviceType,
                ExtraInfo = rule.ExtraInfo,
                StartTime = "",
                EndTime = "",
                FactoryId = user.factoryId,
                Level = rule.Level,
                LinkageActionId = linkid,
                Max = rule.Max.ToString(),
                Min = rule.Min.ToString(),
                Method = rule.Method,
                ProbeName = rule.TargetDevice,
                RuleName = rule.Name,
                TimeType = ""
            };
            if (rule.Actions == null)
                rule.Actions = new CreateThermalLinkageAction.ThermalActions[0];
            var actions = rule.Actions.Select(s =>
             {
                 return new BklLinkageAction
                 {
                     AttributeId = s.Id,
                     PairId = s.PairId,
                     ConnectionUuid = s.ConnUuid,
                     LinkageActionId = linkid,
                     Order = 0,
                     Sleep = 0,
                     ValueHexString = new HexString(new ushort[] { ushort.Parse(s.SelectValue) }).ToString(),
                     ValueCN = s.SelectKey,
                     Id = SnowId.NextId(),
                     WriteType = (int)s.ReadType,
                     WriteStatusName = s.StatusName,
                     WriteStatusNameCN = s.StatusNameCN,
                     Name = "", 
                     CreatorId = user.userId
                 };
             }).ToArray();
            using (var tran = context.Database.BeginTransaction())
            {
                try
                {
                    context.BklAnalysisRule.Add(post);
                    context.BklLinkageAction.AddRange(actions);
                    context.SaveChanges();
                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                }
            }

            return Json(new { post, actions = actions.ToArray() });
        }



        [HttpPost]
        public async Task<BklAnalysisRule> Post([FromServices] BklDbContext context, [FromServices] LogonUser user, [FromBody] CreateLinkageActions.AnalysisRule rule)
        {
            var post = new BklAnalysisRule
            {
                StatusName = rule.statusName,
                AttributeId = rule.attributeId,
                PairId = rule.pairId, 
                Id = SnowId.NextId(),
                CreatorId = user.userId,
                DeviceId = rule.deviceId,
                DeviceType = rule.deviceType,
                ExtraInfo = rule.extraInfo,
                StartTime = "",
                EndTime = "",
                FactoryId = user.factoryId,
                Level = rule.level,
                LinkageActionId = rule.linkageActionId,
                Max = rule.key,
                Min = rule.key,
                Method = rule.method,
                ProbeName = "",
                RuleName = rule.name,
                TimeType = ""
            };
            post.Id = SnowId.NextId();
            post.Method = string.IsNullOrEmpty(post.Method) ? "average" : post.Method;
            context.BklAnalysisRule.Add(post);
            context.SaveChanges();
            return post;
        }


        [HttpGet("linkage-actions")]
        public async Task<IActionResult> GetLinkageActions([FromServices] BklDbContext context, [FromServices] LogonUser user,
            long linkageActionId)
        {
            var lis = await context.BklLinkageAction.Where(s => s.LinkageActionId == linkageActionId).ToListAsync();
            return Json(lis);
        }

        [HttpPost("linkage-actions")]
        public async Task<IActionResult> PostLinkageActions(
            [FromServices] BklDbContext context,
            [FromServices] LogonUser user,
            [FromBody] CreateLinkageActions actions)
        {
            var lid = actions.Actions.First().LinkageActionId;
            var dbRules = actions.Rules.Select(s => new BklAnalysisRule
            {
                StatusName = s.statusName,
                AttributeId = s.attributeId,
                PairId = s.pairId, 
                Id = SnowId.NextId(),
                CreatorId = user.userId,
                DeviceId = s.deviceId,
                DeviceType = s.deviceType,
                ExtraInfo = s.extraInfo,
                StartTime = "",
                EndTime = "",
                FactoryId = user.factoryId,
                Level = s.level,
                LinkageActionId = lid,
                Max = s.key,
                Min = s.key,
                Method = s.method,
                ProbeName = "",
                RuleName = s.name,
                TimeType = ""
            }).ToList();
            if (actions.Actions != null)
            {
                var attrIds = actions.Actions.Select(s => s.AttributeId).ToArray();
                var lisAttr = context.ModbusNodeInfo.Where(s => attrIds.Contains(s.Id)).ToList();
                var dbactions = actions.Actions.Select(s =>
                {
                    var val = lisAttr.FirstOrDefault(q => q.Id == s.AttributeId);
                    return new BklLinkageAction
                    {
                        AttributeId = s.AttributeId,

                        PairId = s.PairId,
                        ConnectionUuid = s.ConnectionUuid,
                        LinkageActionId = s.LinkageActionId,
                        Order = s.Order,
                        Sleep = s.Sleep,
                        ValueHexString = new HexString(new ushort[] { ushort.Parse(s.Value) }).ToString(),
                        ValueCN = s.ValueCN,
                        Id = SnowId.NextId(),
                        WriteType = (int)val.ReadType,
                        WriteStatusName = val.StatusName,
                        WriteStatusNameCN = val.StatusNameCN,
                        Name = "", 
                        CreatorId = user.userId

                    };
                }).ToList();
                context.BklLinkageAction.AddRange(dbactions);
            }
            context.BklAnalysisRule.AddRange(dbRules);
            await context.SaveChangesAsync();
            return Json(new { error = 0, });
        }


        [HttpPut]
        public async Task<BklAnalysisRule> Put([FromServices] BklDbContext context)
        {
            var body = await this.Request.BodyReader.ReadAsync();
            var strBody = Encoding.UTF8.GetString(body.Buffer);
            var post = JsonConvert.DeserializeObject<BklAnalysisRule>(strBody);
            var edit = context.BklAnalysisRule.FirstOrDefault(p => p.Id == post.Id);
            edit.UpdateFrom(post);
            context.SaveChanges();
            return post;
        }
        [HttpDelete]
        public object Delete([FromServices] BklDbContext context, long id)
        {
            var edit = context.BklAnalysisRule.FirstOrDefault(p => p.Id == id);
            context.BklAnalysisRule.Remove(edit);
            var links = context.BklLinkageAction.Where(s => s.LinkageActionId == edit.LinkageActionId).ToArray();
            context.BklLinkageAction.RemoveRange(links);
            context.SaveChanges();
            return edit;
        }


        [HttpGet("contact")]
        public object GetContact([FromServices] BklDbContext context, [FromServices] LogonUser user)
        {
            var contacts = context.BklNotificationContact.Where(p => p.FactoryId == user.factoryId).ToList();
            return contacts;
        }
        [HttpPut("contact")]
        public object PutContact([FromServices] BklDbContext context, [FromServices] LogonUser user, [FromBody] ContactView view)
        {
            var contact = context.BklNotificationContact.FirstOrDefault(s => s.Id == view.Id);
            contact.ContactName = view.ContactName;
            contact.ContactInfo = view.ContactInfo;
            context.SaveChanges();
            return contact;
        }
        [HttpPost("contact")]
        public object PostContact([FromServices] BklDbContext context, [FromServices] LogonUser user, [FromBody] ContactView view)
        {
            var item = new BklNotificationContact
            {
                ContactName = view.ContactName,
                FactoryId = user.factoryId,
                ContactType = view.ContactType,
                ContactInfo = view.ContactInfo, 
            };
            context.BklNotificationContact.Add(item);
            context.SaveChanges();
            return item;
        }
        [HttpDelete("contact")]
        public object DeleteContact([FromServices] BklDbContext context, int contactId)
        {
            var contact = context.BklNotificationContact.Where(p => p.Id == contactId).FirstOrDefault();
            context.BklNotificationContact.Remove(contact);
            return contact;
        }
    }
}
