using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bkl.Models.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bkl_analysis_log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    Level = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Content = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    FacilityId = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint(20)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    RecordedVideo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RecordedPicture = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RecordedData = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    RuleId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Year = table.Column<int>(type: "int(11)", nullable: false),
                    AlarmTimes = table.Column<int>(type: "int(11)", nullable: false),
                    HandleTimes = table.Column<int>(type: "int(11)", nullable: false),
                    OffsetStart = table.Column<long>(type: "bigint(20)", nullable: false),
                    OffsetEnd = table.Column<long>(type: "bigint(20)", nullable: false),
                    Day = table.Column<int>(type: "int(11)", nullable: false),
                    Week = table.Column<int>(type: "int(11)", nullable: false),
                    Month = table.Column<int>(type: "int(11)", nullable: false),
                    HourOfDay = table.Column<int>(type: "int(11)", nullable: false),
                    DayOfMonth = table.Column<int>(type: "int(11)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_analysis_log", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_analysis_rule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    ProbeName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    RuleName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    StatusName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    DeviceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EndTime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TimeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Min = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Max = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Method = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Level = table.Column<int>(type: "int(11)", maxLength: 10, nullable: false),
                    ExtraInfo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    AttributeId = table.Column<long>(type: "bigint(20)", nullable: false),
                    PairId = table.Column<long>(type: "bigint(20)", nullable: false),
                    LinkageActionId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_analysis_rule", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_device_metadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    GroupName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ProbeName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DeviceType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DeviceName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    PDeviceType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    PDeviceName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    PathType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FullPath = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Path1 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Path2 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Path3 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Path4 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Path5 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Path6 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    MacAddress = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ConnectionString = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ConnectionType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DeviceMetadata = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    FacilityName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    AreaName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FacilityId = table.Column<long>(type: "bigint(20)", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_device_metadata", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_device_status",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    Time = table.Column<long>(type: "bigint(20)", nullable: false),
                    TimeType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    StatusName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    GroupName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    StatusValue = table.Column<double>(type: "double", nullable: false),
                    FacilityRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_device_status", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_dga_gas_production",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    TaskId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Time = table.Column<long>(type: "bigint(20)", nullable: false),
                    FacilityRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    GasName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Rate = table.Column<double>(type: "double", nullable: false),
                    RateType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_dga_gas_production", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_dga_status",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    Time = table.Column<long>(type: "bigint(20)", nullable: false),
                    FacilityRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceRelId = table.Column<long>(type: "bigint(20)", nullable: false),
                    CO = table.Column<double>(type: "double", nullable: false),
                    CO2 = table.Column<double>(type: "double", nullable: false),
                    H2 = table.Column<double>(type: "double", nullable: false),
                    O2 = table.Column<double>(type: "double", nullable: false),
                    N2 = table.Column<double>(type: "double", nullable: false),
                    CH4 = table.Column<double>(type: "double", nullable: false),
                    C2H2 = table.Column<double>(type: "double", nullable: false),
                    C2H4 = table.Column<double>(type: "double", nullable: false),
                    C2H6 = table.Column<double>(type: "double", nullable: false),
                    TotHyd = table.Column<double>(type: "double", nullable: false),
                    CmbuGas = table.Column<double>(type: "double", nullable: false),
                    Mst = table.Column<double>(type: "double", nullable: false),
                    OilTmp = table.Column<double>(type: "double", nullable: false),
                    LeakCur = table.Column<double>(type: "double", nullable: false),
                    GasPres = table.Column<double>(type: "double", nullable: false),
                    C2H2_C2H4_Code = table.Column<int>(type: "int(11)", nullable: false),
                    CH4_H2_Code = table.Column<int>(type: "int(11)", nullable: false),
                    C2H4_C2H6_Code = table.Column<int>(type: "int(11)", nullable: false),
                    C2H2_C2H4_Tatio = table.Column<double>(type: "double", nullable: false),
                    CH4_H2_Tatio = table.Column<double>(type: "double", nullable: false),
                    C2H4_C2H6_Tatio = table.Column<double>(type: "double", nullable: false),
                    C2H6_CH4_Tatio = table.Column<double>(type: "double", nullable: false),
                    CO_Inc = table.Column<double>(type: "double", nullable: false),
                    CO2_Inc = table.Column<double>(type: "double", nullable: false),
                    H2_Inc = table.Column<double>(type: "double", nullable: false),
                    O2_Inc = table.Column<double>(type: "double", nullable: false),
                    N2_Inc = table.Column<double>(type: "double", nullable: false),
                    CH4_Inc = table.Column<double>(type: "double", nullable: false),
                    C2H2_Inc = table.Column<double>(type: "double", nullable: false),
                    C2H4_Inc = table.Column<double>(type: "double", nullable: false),
                    C2H6_Inc = table.Column<double>(type: "double", nullable: false),
                    TotHyd_Inc = table.Column<double>(type: "double", nullable: false),
                    CmbuGas_Inc = table.Column<double>(type: "double", nullable: false),
                    C2H2_H2_Tatio = table.Column<double>(type: "double", nullable: false),
                    O2_N2_Tatio = table.Column<double>(type: "double", nullable: false),
                    CO2_CO_Tatio = table.Column<double>(type: "double", nullable: false),
                    O2_N2_Inc_Tatio = table.Column<double>(type: "double", nullable: false),
                    CO2_CO_Inc_Tatio = table.Column<double>(type: "double", nullable: false),
                    ThreeTatio_Code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Calculated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    DataId = table.Column<long>(type: "bigint(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_dga_status", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_factory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Province = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ProvinceCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    City = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CityCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Distribute = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DistributeCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_factory", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_factory_facility",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FacilityType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    CreatorName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    GPSLocation = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_factory_facility", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_factory_user",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Account = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Password = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Roles = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Positions = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_factory_user", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_inspection_task",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    TaskName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    TaskType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TaskStatus = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TaskDescription = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TotalNumber = table.Column<int>(type: "int(11)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Updatetime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_inspection_task", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_inspection_task_detail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    TaskId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FacilityId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FacilityName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    LocalImagePath = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Error = table.Column<int>(type: "int(11)", nullable: false),
                    RemoteImagePath = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    ImageType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ImageWidth = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ImageHeight = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_inspection_task_detail", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_inspection_task_result",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    TaskDetailId = table.Column<long>(type: "bigint(20)", nullable: false),
                    TaskId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FacilityId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FacilityName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DamageSize = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DamageX = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: false),
                    DamageY = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: false),
                    DamageWidth = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: false),
                    DamageHeight = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: false),
                    DamageType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DamageLevel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DamagePosition = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DamageDescription = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TreatmentSuggestion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_inspection_task_result", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_linkage_action",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    LinkageActionId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Order = table.Column<int>(type: "int(11)", nullable: false),
                    Sleep = table.Column<int>(type: "int(11)", nullable: false),
                    PairId = table.Column<long>(type: "bigint(20)", nullable: false),
                    ConnectionUuid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    AttributeId = table.Column<long>(type: "bigint(20)", nullable: false),
                    WriteType = table.Column<int>(type: "int(11)", nullable: false),
                    WriteStatusName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    WriteStatusNameCN = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ValueHexString = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ValueCN = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_linkage_action", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_notification_contact",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    ContactName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ContactType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ContactInfo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    UserId = table.Column<long>(type: "bigint(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_notification_contact", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_permission",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Control = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Access = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TargetType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<long>(type: "bigint(20)", nullable: false),
                    TargetName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_permission", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_thermal_camera",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Port = table.Column<int>(type: "int(11)", nullable: false),
                    Account = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Password = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_thermal_camera", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "bkl_user_granted",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    UserId = table.Column<long>(type: "bigint(20)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FactoryId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FactoryName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FacilityId = table.Column<long>(type: "bigint(20)", nullable: false),
                    FacilityName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Roles = table.Column<string>(type: "VARCHAR(200)", maxLength: 200, nullable: false),
                    Createtime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bkl_user_granted", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "modbus_conn_info",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    ModbusType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    ConnType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    ConnStr = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Uuid = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modbus_conn_info", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "modbus_device_pair",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint(20)", nullable: false),
                    BusId = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    ConnectionId = table.Column<long>(type: "bigint(20)", nullable: false),
                    NodeId = table.Column<long>(type: "bigint(20)", nullable: false),
                    NodeIndex = table.Column<short>(type: "smallint(6)", nullable: false),
                    ProtocolName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ConnUuid = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modbus_device_pair", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "modbus_node_info",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint(20)", nullable: false),
                    ProtocolName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ReadType = table.Column<int>(type: "int(11)", nullable: false),
                    StartAddress = table.Column<short>(type: "smallint(6)", nullable: false),
                    DataSize = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    DataType = table.Column<int>(type: "int(11)", nullable: false),
                    DataOrder = table.Column<int>(type: "int(11)", nullable: false),
                    NodeCount = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    Scale = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    StatusName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    StatusNameCN = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    UnitCN = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    ValueMap = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modbus_node_info", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8_general_ci");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bkl_analysis_log");

            migrationBuilder.DropTable(
                name: "bkl_analysis_rule");

            migrationBuilder.DropTable(
                name: "bkl_device_metadata");

            migrationBuilder.DropTable(
                name: "bkl_device_status");

            migrationBuilder.DropTable(
                name: "bkl_dga_gas_production");

            migrationBuilder.DropTable(
                name: "bkl_dga_status");

            migrationBuilder.DropTable(
                name: "bkl_factory");

            migrationBuilder.DropTable(
                name: "bkl_factory_facility");

            migrationBuilder.DropTable(
                name: "bkl_factory_user");

            migrationBuilder.DropTable(
                name: "bkl_inspection_task");

            migrationBuilder.DropTable(
                name: "bkl_inspection_task_detail");

            migrationBuilder.DropTable(
                name: "bkl_inspection_task_result");

            migrationBuilder.DropTable(
                name: "bkl_linkage_action");

            migrationBuilder.DropTable(
                name: "bkl_notification_contact");

            migrationBuilder.DropTable(
                name: "bkl_permission");

            migrationBuilder.DropTable(
                name: "bkl_thermal_camera");

            migrationBuilder.DropTable(
                name: "bkl_user_granted");

            migrationBuilder.DropTable(
                name: "modbus_conn_info");

            migrationBuilder.DropTable(
                name: "modbus_device_pair");

            migrationBuilder.DropTable(
                name: "modbus_node_info");
        }
    }
}
