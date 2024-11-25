using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Bkl.Models.Migrations
{
    public partial class seeddata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            long userid = SnowId.NextId();
            long factoryId = SnowId.NextId();
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            migrationBuilder.InsertData(
                "bkl_factory_user",
         new string[] { "Id", "UserName", "Account", "Password", "CreatorId", "FactoryId", "Createtime", "Roles", "Positions" },
         new object[,] {
                {userid, "赵", "admin", "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", 0, factoryId, date, "admin", "系统管理员"},
         });
            migrationBuilder.InsertData("bkl_factory",
                new string[] { "Id", "FactoryName", "Country", "Province", "ProvinceCode", "City", "CityCode", "Distribute", "DistributeCode", "CreatorId", "Createtime" },
                new object[,] {
                  {factoryId, "电厂","中国", "河南", "100","郑州","100100","金水区","100100100",userid,date}
          });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
