using Microsoft.EntityFrameworkCore.Migrations;

namespace Bkl.Models.LocalContext
{
    public partial class YoloSqlInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BklLocalYoloDataSet",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RectId = table.Column<int>(nullable: false),
                    Path = table.Column<string>(nullable: true),
                    DirName = table.Column<string>(nullable: true),
                    FacilityId = table.Column<int>(nullable: false),
                    FactoryId = table.Column<int>(nullable: false),
                    TaskId = table.Column<int>(nullable: false),
                    TaskDetailId = table.Column<int>(nullable: false),
                    RawPoints = table.Column<string>(nullable: true),
                    YoloPoints = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BklLocalYoloDataSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BklLocalYoloPath",
                columns: table => new
                {
                    DirName = table.Column<string>(nullable: false),
                    YoloSetting = table.Column<string>(nullable: true),
                    ClassStatistic = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BklLocalYoloPath", x => x.DirName);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BklLocalYoloDataSet");

            migrationBuilder.DropTable(
                name: "BklLocalYoloPath");
        }
    }
}
