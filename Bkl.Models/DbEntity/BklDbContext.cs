using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bkl.Models
{
    public partial class BklDbContext : DbContext
    {
        public BklDbContext()
        {
        }

        public BklDbContext(DbContextOptions<BklDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BklAnalysisLog> BklAnalysisLog { get; set; }
        public virtual DbSet<BklAnalysisRule> BklAnalysisRule { get; set; }
        public virtual DbSet<BklDeviceMetadata> BklDeviceMetadata { get; set; }
        public virtual DbSet<BklDeviceStatus> BklDeviceStatus { get; set; }
        public virtual DbSet<BklDGAGasProduction> BklDGAGasProduction { get; set; }
        public virtual DbSet<BklDGAStatus> BklDGAStatus { get; set; }
        public virtual DbSet<BklFactory> BklFactory { get; set; }
        public virtual DbSet<BklFactoryFacility> BklFactoryFacility { get; set; }
        public virtual DbSet<BklFactoryUser> BklFactoryUser { get; set; }
        public virtual DbSet<BklInspectionTask> BklInspectionTask { get; set; }
        public virtual DbSet<BklInspectionTaskDetail> BklInspectionTaskDetail { get; set; }
        public virtual DbSet<BklInspectionTaskResult> BklInspectionTaskResult { get; set; }
        public virtual DbSet<BklLinkageAction> BklLinkageAction { get; set; }
        public virtual DbSet<BklNotificationContact> BklNotificationContact { get; set; }
        public virtual DbSet<BklPermission> BklPermission { get; set; }
        public virtual DbSet<BklThermalCamera> BklThermalCamera { get; set; }
        public virtual DbSet<BklUserGranted> BklUserGranted { get; set; } 
        public virtual DbSet<ModbusConnInfo> ModbusConnInfo { get; set; }
        public virtual DbSet<ModbusDevicePair> ModbusDevicePair { get; set; }
        public virtual DbSet<ModbusNodeInfo> ModbusNodeInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
//            if (!optionsBuilder.IsConfigured)
//            {
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//                optionsBuilder.UseMySql("server=192.168.31.173;uid=root;pwd=bkl123...;database=espsdb;convert zero datetime=True", Microsoft.EntityFrameworkCore.ServerVersion.Parse("5.7.40-mysql"));
//            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_general_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<BklAnalysisLog>(entity =>
            {
                entity.ToTable("bkl_analysis_log");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.AlarmTimes).HasColumnType("int(11)");

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.Day).HasColumnType("int(11)");

                entity.Property(e => e.DayOfMonth).HasColumnType("int(11)");

                entity.Property(e => e.DayOfWeek).HasColumnType("int(11)");

                entity.Property(e => e.DeviceId).HasColumnType("bigint(20)");

                entity.Property(e => e.EndTime).HasColumnType("datetime");

                entity.Property(e => e.FacilityId).HasColumnType("bigint(20)");

                entity.Property(e => e.HandleTimes).HasColumnType("int(11)");

                entity.Property(e => e.HourOfDay).HasColumnType("int(11)");

                entity.Property(e => e.Level)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Month).HasColumnType("int(11)");

                entity.Property(e => e.OffsetEnd).HasColumnType("bigint(20)");

                entity.Property(e => e.OffsetStart).HasColumnType("bigint(20)");

                entity.Property(e => e.RecordedData)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.RecordedPicture)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.RecordedVideo)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.RuleId).HasColumnType("bigint(20)");

                entity.Property(e => e.StartTime).HasColumnType("datetime");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Week).HasColumnType("int(11)");

                entity.Property(e => e.Year).HasColumnType("int(11)");
            });

            modelBuilder.Entity<BklAnalysisRule>(entity =>
            {
                entity.ToTable("bkl_analysis_rule");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.AttributeId).HasColumnType("bigint(20)");

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.DeviceId).HasColumnType("bigint(20)");

                entity.Property(e => e.DeviceType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.EndTime)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ExtraInfo)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.Level).HasColumnType("int(11)");

                entity.Property(e => e.LinkageActionId).HasColumnType("bigint(20)");

                entity.Property(e => e.Max)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Method)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Min)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PairId).HasColumnType("bigint(20)");

                entity.Property(e => e.ProbeName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.RuleName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.StartTime)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.StatusName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.TimeType)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<BklDeviceMetadata>(entity =>
            {
                entity.ToTable("bkl_device_metadata");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.AreaName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.ConnectionString)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ConnectionType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.DeviceMetadata)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.DeviceName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.DeviceType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.FacilityId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FullPath)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.GroupName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.MacAddress)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Path1)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Path2)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Path3)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Path4)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Path5)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Path6)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PathType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PDeviceName)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("PDeviceName");

                entity.Property(e => e.PDeviceType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("PDeviceType");

                entity.Property(e => e.ProbeName)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<BklDeviceStatus>(entity =>
            {
                entity.ToTable("bkl_device_status");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.DeviceRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.GroupName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.StatusName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Time).HasColumnType("bigint(20)");

                entity.Property(e => e.TimeType)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<BklDGAGasProduction>(entity =>
            {
                entity.ToTable("bkl_dga_gas_production");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.DeviceRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.GasName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.RateType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TaskId)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Time).HasColumnType("bigint(20)");
            });

            modelBuilder.Entity<BklDGAStatus>(entity =>
            {
                entity.ToTable("bkl_dga_status");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.C2H2).HasColumnName("C2H2");

                entity.Property(e => e.C2H2_C2H4_Code)
                    .HasColumnType("int(11)")
                    .HasColumnName("C2H2_C2H4_Code");

                entity.Property(e => e.C2H2_C2H4_Tatio).HasColumnName("C2H2_C2H4_Tatio");

                entity.Property(e => e.C2H2_H2_Tatio).HasColumnName("C2H2_H2_Tatio");

                entity.Property(e => e.C2H2_Inc).HasColumnName("C2H2_Inc");

                entity.Property(e => e.C2H4).HasColumnName("C2H4");

                entity.Property(e => e.C2H4_C2H6_Code).HasColumnType("int(11)").HasColumnName("C2H4_C2H6_Code");

                entity.Property(e => e.C2H4_C2H6_Tatio).HasColumnName("C2H4_C2H6_Tatio");

                entity.Property(e => e.C2H4_Inc).HasColumnName("C2H4_Inc");

                entity.Property(e => e.C2H6).HasColumnName("C2H6");

                entity.Property(e => e.C2H6_CH4_Tatio).HasColumnName("C2H6_CH4_Tatio");

                entity.Property(e => e.C2H6_Inc).HasColumnName("C2H6_Inc");

                entity.Property(e => e.CH4).HasColumnName("CH4");

                entity.Property(e => e.CH4_H2_Code).HasColumnType("int(11)").HasColumnName("CH4_H2_Code");

                entity.Property(e => e.CH4_H2_Tatio).HasColumnName("CH4_H2_Tatio");

                entity.Property(e => e.CH4_Inc).HasColumnName("CH4_Inc");

                entity.Property(e => e.CmbuGas_Inc).HasColumnName("CmbuGas_Inc");

                entity.Property(e => e.CO).HasColumnName("CO");

                entity.Property(e => e.CO2).HasColumnName("CO2");

                entity.Property(e => e.CO2_CO_Inc_Tatio).HasColumnName("CO2_CO_Inc_Tatio");

                entity.Property(e => e.CO2_CO_Tatio).HasColumnName("CO2_CO_Tatio");

                entity.Property(e => e.CO2_Inc).HasColumnName("CO2_Inc");

                entity.Property(e => e.CO_Inc).HasColumnName("CO_Inc");

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.DataId).HasColumnType("bigint(20)");

                entity.Property(e => e.DeviceRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryRelId).HasColumnType("bigint(20)");

                entity.Property(e => e.H2_Inc).HasColumnName("H2_Inc");

                entity.Property(e => e.N2_Inc).HasColumnName("N2_Inc");

                entity.Property(e => e.O2_Inc).HasColumnName("O2_Inc");

                entity.Property(e => e.O2_N2_Inc_Tatio).HasColumnName("O2_N2_Inc_Tatio");

                entity.Property(e => e.O2_N2_Tatio).HasColumnName("O2_N2_Tatio");

                entity.Property(e => e.ThreeTatio_Code)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("ThreeTatio_Code");

                entity.Property(e => e.Time).HasColumnType("bigint(20)");

                entity.Property(e => e.TotHyd_Inc).HasColumnName("TotHyd_Inc");
            });

            modelBuilder.Entity<BklFactory>(entity =>
            {
                entity.ToTable("bkl_factory");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CityCode)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Country)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.Distribute)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DistributeCode)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Province)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProvinceCode)
                    .IsRequired()
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<BklFactoryFacility>(entity =>
            {
                entity.ToTable("bkl_factory_facility");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.CreatorName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FacilityType)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.GPSLocation)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("GPSLocation");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<BklFactoryUser>(entity =>
            {
                entity.ToTable("bkl_factory_user");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Account)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Positions)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<BklInspectionTask>(entity =>
            {
                entity.ToTable("bkl_inspection_task");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TaskDescription)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TaskName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TaskStatus)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TaskType)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TotalNumber).HasColumnType("int(11)");

                entity.Property(e => e.Updatetime).HasColumnType("datetime");
            });

            modelBuilder.Entity<BklInspectionTaskDetail>(entity =>
            {
                entity.ToTable("bkl_inspection_task_detail");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.Error).HasColumnType("int(11)");

                entity.Property(e => e.FacilityId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ImageHeight)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ImageType)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.ImageWidth)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.LocalImagePath)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Position)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.RemoteImagePath)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.TaskId).HasColumnType("bigint(20)");
            });

            modelBuilder.Entity<BklInspectionTaskResult>(entity =>
            {
                entity.ToTable("bkl_inspection_task_result");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.DamageDescription)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DamageHeight)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.DamageLevel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.DamagePosition)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DamageSize)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DamageType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.DamageWidth)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.DamageX)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.DamageY)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FacilityId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Position)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TaskDetailId).HasColumnType("bigint(20)");

                entity.Property(e => e.TaskId).HasColumnType("bigint(20)");

                entity.Property(e => e.TreatmentSuggestion)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<BklLinkageAction>(entity =>
            {
                entity.ToTable("bkl_linkage_action");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.AttributeId).HasColumnType("bigint(20)");

                entity.Property(e => e.ConnectionUuid)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.LinkageActionId).HasColumnType("bigint(20)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Order).HasColumnType("int(11)");

                entity.Property(e => e.PairId).HasColumnType("bigint(20)");

                entity.Property(e => e.Sleep).HasColumnType("int(11)");

                entity.Property(e => e.ValueCN)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("ValueCN");

                entity.Property(e => e.ValueHexString)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.WriteStatusName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.WriteStatusNameCN)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("WriteStatusNameCN");

                entity.Property(e => e.WriteType).HasColumnType("int(11)");
            });

            modelBuilder.Entity<BklNotificationContact>(entity =>
            {
                entity.ToTable("bkl_notification_contact");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.ContactInfo)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ContactName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ContactType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.UserId).HasColumnType("bigint(20)");
            });

            modelBuilder.Entity<BklPermission>(entity =>
            {
                entity.ToTable("bkl_permission");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Access)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Control)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TargetId).HasColumnType("bigint(20)");

                entity.Property(e => e.TargetName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TargetType)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<BklThermalCamera>(entity =>
            {
                entity.ToTable("bkl_thermal_camera");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Account)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.DeviceId).HasColumnType("bigint(20)");

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Port).HasColumnType("int(11)");

                entity.Property(e => e.UserId).HasColumnType("bigint(20)");
            });

            modelBuilder.Entity<BklUserGranted>(entity =>
            {
                entity.ToTable("bkl_user_granted");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Createtime).HasColumnType("datetime");

                entity.Property(e => e.CreatorId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityId).HasColumnType("bigint(20)");

                entity.Property(e => e.FacilityName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.FactoryId).HasColumnType("bigint(20)");

                entity.Property(e => e.FactoryName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.UserId).HasColumnType("bigint(20)");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(20);
            }); 

            modelBuilder.Entity<ModbusConnInfo>(entity =>
            {
                entity.ToTable("modbus_conn_info");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.ConnStr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ConnType)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.ModbusType)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Uuid)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ModbusDevicePair>(entity =>
            {
                entity.ToTable("modbus_device_pair");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.BusId).HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.ConnUuid)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.ConnectionId).HasColumnType("bigint(20)");

                entity.Property(e => e.DeviceId).HasColumnType("bigint(20)");

                entity.Property(e => e.NodeId).HasColumnType("bigint(20)");

                entity.Property(e => e.NodeIndex).HasColumnType("smallint(6)");

                entity.Property(e => e.ProtocolName)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<ModbusNodeInfo>(entity =>
            {
                entity.ToTable("modbus_node_info");

                entity.Property(e => e.Id)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever();

                entity.Property(e => e.DataOrder).HasColumnType("int(11)");

                entity.Property(e => e.DataSize).HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.DataType).HasColumnType("int(11)");

                entity.Property(e => e.NodeCount).HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.ProtocolName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ReadType).HasColumnType("int(11)");

                entity.Property(e => e.Scale)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.StartAddress).HasColumnType("smallint(6)");

                entity.Property(e => e.StatusName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.StatusNameCN)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("StatusNameCN");

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.UnitCN)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("UnitCN");

                entity.Property(e => e.ValueMap)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
