using System;
using System.Collections.Generic;
using DeliveryOrderAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryOrderAPI.Contexts;

public partial class DBSCM : DbContext
{
    public DBSCM()
    {
    }

    public DBSCM(DbContextOptions<DBSCM> options)
        : base(options)
    {
    }

    public virtual DbSet<AlGstDatpid> AlGstDatpids { get; set; }

    public virtual DbSet<AlVendor> AlVendors { get; set; }

    public virtual DbSet<DoDictMstr> DoDictMstrs { get; set; }

    public virtual DbSet<DoDoDev> DoDoDevs { get; set; }

    public virtual DbSet<DoHistory> DoHistories { get; set; }

    public virtual DbSet<DoHistoryDev> DoHistoryDevs { get; set; }

    public virtual DbSet<DoMaster> DoMasters { get; set; }

    public virtual DbSet<DoPartMaster> DoPartMasters { get; set; }

    public virtual DbSet<DoPlan> DoPlans { get; set; }

    public virtual DbSet<DoPlanDev> DoPlanDevs { get; set; }

    public virtual DbSet<DoReq> DoReqs { get; set; }

    public virtual DbSet<DoStockAlpha> DoStockAlphas { get; set; }

    public virtual DbSet<DoVenderMaster> DoVenderMasters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.226.86;Database=dbSCM;TrustServerCertificate=True;uid=sa;password=decjapan");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Thai_CI_AS");

        modelBuilder.Entity<AlGstDatpid>(entity =>
        {
            entity.HasKey(e => new { e.Wcno, e.Idate, e.Itime, e.Partno, e.Kotei, e.Brusn, e.Slipno });

            entity.ToTable("AL_GST_DATPID");

            entity.HasIndex(e => e.Idate, "IX_AL_GST_DATPID");

            entity.HasIndex(e => new { e.Wcno, e.Idate }, "IX_AL_GST_DATPID_1");

            entity.HasIndex(e => new { e.Idate, e.Wcno, e.Brusn, e.Partno }, "IX_AL_GST_DATPID_2");

            entity.HasIndex(e => e.Slipno, "IX_AL_GST_DATPID_3");

            entity.Property(e => e.Wcno)
                .HasMaxLength(3)
                .HasColumnName("WCNO");
            entity.Property(e => e.Idate)
                .HasMaxLength(8)
                .HasColumnName("IDATE");
            entity.Property(e => e.Itime)
                .HasMaxLength(4)
                .HasColumnName("ITIME");
            entity.Property(e => e.Partno)
                .HasMaxLength(25)
                .HasColumnName("PARTNO");
            entity.Property(e => e.Kotei)
                .HasMaxLength(2)
                .HasColumnName("KOTEI");
            entity.Property(e => e.Brusn)
                .HasMaxLength(2)
                .HasColumnName("BRUSN");
            entity.Property(e => e.Slipno)
                .HasMaxLength(15)
                .HasColumnName("SLIPNO");
            entity.Property(e => e.Fqty)
                .HasColumnType("decimal(25, 5)")
                .HasColumnName("FQTY");
            entity.Property(e => e.Iqty)
                .HasColumnType("decimal(25, 5)")
                .HasColumnName("IQTY");
            entity.Property(e => e.Prgbit)
                .HasMaxLength(1)
                .HasColumnName("PRGBIT");
            entity.Property(e => e.Rqty)
                .HasColumnType("decimal(25, 5)")
                .HasColumnName("RQTY");
            entity.Property(e => e.Sqty)
                .HasColumnType("decimal(25, 5)")
                .HasColumnName("SQTY");
            entity.Property(e => e.Whum)
                .HasMaxLength(3)
                .HasColumnName("WHUM");
        });

        modelBuilder.Entity<AlVendor>(entity =>
        {
            entity.HasKey(e => e.Vender);

            entity.ToTable("AL_Vendor");

            entity.Property(e => e.Vender).HasMaxLength(20);
            entity.Property(e => e.AbbreName).HasMaxLength(50);
            entity.Property(e => e.Boitype)
                .HasMaxLength(10)
                .HasColumnName("BOIType");
            entity.Property(e => e.Currency).HasMaxLength(5);
            entity.Property(e => e.EmailPo)
                .HasMaxLength(300)
                .HasColumnName("EMailPO");
            entity.Property(e => e.IsMilkRun)
                .HasDefaultValueSql("((0))")
                .HasColumnName("isMilkRun");
            entity.Property(e => e.PersonIncharge).HasMaxLength(50);
            entity.Property(e => e.Route).HasMaxLength(1);
            entity.Property(e => e.VenderCard).HasMaxLength(20);
            entity.Property(e => e.VenderName).HasMaxLength(300);
        });

        modelBuilder.Entity<DoDictMstr>(entity =>
        {
            entity.HasKey(e => e.DictId);

            entity.ToTable("DO_DictMstr");

            entity.Property(e => e.DictId).HasColumnName("DICT_ID");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("CODE");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("CREATE_DATE");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.DictStatus)
                .HasMaxLength(20)
                .HasColumnName("DICT_STATUS");
            entity.Property(e => e.DictType)
                .HasMaxLength(20)
                .HasColumnName("DICT_TYPE");
            entity.Property(e => e.Note)
                .HasMaxLength(50)
                .HasColumnName("NOTE");
            entity.Property(e => e.RefCode)
                .HasMaxLength(20)
                .HasColumnName("REF_CODE");
            entity.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATE_DATE");
        });

        modelBuilder.Entity<DoDoDev>(entity =>
        {
            entity.HasKey(e => e.RunningId);

            entity.ToTable("DO_DO_DEV");

            entity.Property(e => e.RunningId).HasColumnName("RUNNING_ID");
            entity.Property(e => e.DoAct).HasColumnName("DO_ACT");
            entity.Property(e => e.PartNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PART_NO");
            entity.Property(e => e.RunningCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUNNING_CODE");
            entity.Property(e => e.Ymd)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("YMD");
        });

        modelBuilder.Entity<DoHistory>(entity =>
        {
            entity.ToTable("DO_HISTORY");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateVal)
                .HasMaxLength(8)
                .HasColumnName("DATE_VAL");
            entity.Property(e => e.DoVal).HasColumnName("DO_VAL");
            entity.Property(e => e.InsertBy)
                .HasMaxLength(30)
                .HasColumnName("INSERT_BY");
            entity.Property(e => e.InsertDt)
                .HasColumnType("datetime")
                .HasColumnName("INSERT_DT");
            entity.Property(e => e.Model)
                .HasMaxLength(25)
                .HasColumnName("MODEL");
            entity.Property(e => e.Partno)
                .HasMaxLength(50)
                .HasColumnName("PARTNO");
            entity.Property(e => e.PlanVal).HasColumnName("PLAN_VAL");
            entity.Property(e => e.Rev).HasColumnName("REV");
            entity.Property(e => e.Revision).HasColumnName("REVISION");
            entity.Property(e => e.RunningCode)
                .HasMaxLength(8)
                .HasColumnName("RUNNING_CODE");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("STATUS");
            entity.Property(e => e.Stock).HasColumnName("STOCK");
            entity.Property(e => e.StockVal).HasColumnName("STOCK_VAL");
            entity.Property(e => e.TimeScheduleDelivery)
                .HasMaxLength(8)
                .HasColumnName("TIME_SCHEDULE_DELIVERY");
            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .HasColumnName("VD_CODE");
        });

        modelBuilder.Entity<DoHistoryDev>(entity =>
        {
            entity.ToTable("DO_HISTORY_DEV");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateVal)
                .HasMaxLength(8)
                .HasColumnName("DATE_VAL");
            entity.Property(e => e.DoVal).HasColumnName("DO_VAL");
            entity.Property(e => e.InsertBy)
                .HasMaxLength(30)
                .HasColumnName("INSERT_BY");
            entity.Property(e => e.InsertDt)
                .HasColumnType("datetime")
                .HasColumnName("INSERT_DT");
            entity.Property(e => e.Model)
                .HasMaxLength(25)
                .HasColumnName("MODEL");
            entity.Property(e => e.Partno)
                .HasMaxLength(50)
                .HasColumnName("PARTNO");
            entity.Property(e => e.PlanVal).HasColumnName("PLAN_VAL");
            entity.Property(e => e.Rev).HasColumnName("REV");
            entity.Property(e => e.Revision).HasColumnName("REVISION");
            entity.Property(e => e.RunningCode)
                .HasMaxLength(8)
                .HasColumnName("RUNNING_CODE");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("STATUS");
            entity.Property(e => e.Stock).HasColumnName("STOCK");
            entity.Property(e => e.StockVal).HasColumnName("STOCK_VAL");
            entity.Property(e => e.TimeScheduleDelivery)
                .HasMaxLength(8)
                .HasColumnName("TIME_SCHEDULE_DELIVERY");
            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .HasColumnName("VD_CODE");
        });

        modelBuilder.Entity<DoMaster>(entity =>
        {
            entity.HasKey(e => e.PartNo).HasName("PK_DO_DW_MASTER");

            entity.ToTable("DO_MASTER");

            entity.Property(e => e.PartNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PART_NO");
            entity.Property(e => e.PartCm)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PART_CM");
            entity.Property(e => e.PartDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PART_DESC");
            entity.Property(e => e.PartFixedDate)
                .HasDefaultValueSql("((7))")
                .HasColumnName("PART_FIXED_DATE");
            entity.Property(e => e.PartQtyBox)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PART_QTY_BOX");
            entity.Property(e => e.PartUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PART_UNIT");
            entity.Property(e => e.VdCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("VD_CODE");
            entity.Property(e => e.VdDay1)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY1");
            entity.Property(e => e.VdDay10)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY10");
            entity.Property(e => e.VdDay11)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY11");
            entity.Property(e => e.VdDay12)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY12");
            entity.Property(e => e.VdDay13)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY13");
            entity.Property(e => e.VdDay14)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY14");
            entity.Property(e => e.VdDay15)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY15");
            entity.Property(e => e.VdDay16)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY16");
            entity.Property(e => e.VdDay17)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY17");
            entity.Property(e => e.VdDay18)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY18");
            entity.Property(e => e.VdDay19)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY19");
            entity.Property(e => e.VdDay2)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY2");
            entity.Property(e => e.VdDay20)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY20");
            entity.Property(e => e.VdDay21)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY21");
            entity.Property(e => e.VdDay22)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY22");
            entity.Property(e => e.VdDay23)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY23");
            entity.Property(e => e.VdDay24)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY24");
            entity.Property(e => e.VdDay25)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY25");
            entity.Property(e => e.VdDay26)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY26");
            entity.Property(e => e.VdDay27)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY27");
            entity.Property(e => e.VdDay28)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY28");
            entity.Property(e => e.VdDay29)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY29");
            entity.Property(e => e.VdDay3)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY3");
            entity.Property(e => e.VdDay30)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY30");
            entity.Property(e => e.VdDay31)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY31");
            entity.Property(e => e.VdDay4)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY4");
            entity.Property(e => e.VdDay5)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY5");
            entity.Property(e => e.VdDay6)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY6");
            entity.Property(e => e.VdDay7)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY7");
            entity.Property(e => e.VdDay8)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY8");
            entity.Property(e => e.VdDay9)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_DAY9");
            entity.Property(e => e.VdFri)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_FRI");
            entity.Property(e => e.VdMaxDelivery).HasColumnName("VD_MAX_DELIVERY");
            entity.Property(e => e.VdMinDelivery).HasColumnName("VD_MIN_DELIVERY");
            entity.Property(e => e.VdMon)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_MON");
            entity.Property(e => e.VdName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("VD_NAME");
            entity.Property(e => e.VdProdLeadtime)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_PROD_LEADTIME");
            entity.Property(e => e.VdRound)
                .HasDefaultValueSql("((1))")
                .HasColumnName("VD_ROUND");
            entity.Property(e => e.VdSat)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_SAT");
            entity.Property(e => e.VdSun)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_SUN");
            entity.Property(e => e.VdThu)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_THU");
            entity.Property(e => e.VdTue)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_TUE");
            entity.Property(e => e.VdWed)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_WED");
        });

        modelBuilder.Entity<DoPartMaster>(entity =>
        {
            entity.HasKey(e => e.PartId);

            entity.ToTable("DO_PART_MASTER");

            entity.Property(e => e.PartId).HasColumnName("PART_ID");
            entity.Property(e => e.BoxMax).HasColumnName("BOX_MAX");
            entity.Property(e => e.BoxMin).HasColumnName("BOX_MIN");
            entity.Property(e => e.BoxQty).HasColumnName("BOX_QTY");
            entity.Property(e => e.Cm)
                .HasMaxLength(5)
                .HasColumnName("CM");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.Partno)
                .HasMaxLength(30)
                .HasColumnName("PARTNO");
            entity.Property(e => e.Pdlt).HasColumnName("PDLT");
            entity.Property(e => e.Unit)
                .HasMaxLength(10)
                .HasColumnName("UNIT");
            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .HasColumnName("VD_CODE");
        });

        modelBuilder.Entity<DoPlan>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DO_PLAN");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("((0))")
                .HasColumnType("datetime")
                .HasColumnName("CREATE_DATE");
            entity.Property(e => e.PartNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PART_NO");
            entity.Property(e => e.PlanDate)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("PLAN_DATE");
            entity.Property(e => e.PlanDay1)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY1");
            entity.Property(e => e.PlanDay10)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY10");
            entity.Property(e => e.PlanDay11)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY11");
            entity.Property(e => e.PlanDay12)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY12");
            entity.Property(e => e.PlanDay13)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY13");
            entity.Property(e => e.PlanDay14)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY14");
            entity.Property(e => e.PlanDay15)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY15");
            entity.Property(e => e.PlanDay16)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY16");
            entity.Property(e => e.PlanDay17)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY17");
            entity.Property(e => e.PlanDay18)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY18");
            entity.Property(e => e.PlanDay19)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY19");
            entity.Property(e => e.PlanDay2)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY2");
            entity.Property(e => e.PlanDay20)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY20");
            entity.Property(e => e.PlanDay21)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY21");
            entity.Property(e => e.PlanDay22)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY22");
            entity.Property(e => e.PlanDay23)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY23");
            entity.Property(e => e.PlanDay24)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY24");
            entity.Property(e => e.PlanDay25)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY25");
            entity.Property(e => e.PlanDay26)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY26");
            entity.Property(e => e.PlanDay27)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY27");
            entity.Property(e => e.PlanDay28)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY28");
            entity.Property(e => e.PlanDay29)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY29");
            entity.Property(e => e.PlanDay3)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY3");
            entity.Property(e => e.PlanDay30)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY30");
            entity.Property(e => e.PlanDay31)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY31");
            entity.Property(e => e.PlanDay4)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY4");
            entity.Property(e => e.PlanDay5)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY5");
            entity.Property(e => e.PlanDay6)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY6");
            entity.Property(e => e.PlanDay7)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY7");
            entity.Property(e => e.PlanDay8)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY8");
            entity.Property(e => e.PlanDay9)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PLAN_DAY9");
            entity.Property(e => e.PlanEndDate)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("PLAN_END_DATE");
            entity.Property(e => e.PlanMonth)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("PLAN_MONTH");
            entity.Property(e => e.PlanYear)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("PLAN_YEAR");
            entity.Property(e => e.RunningCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUNNING_CODE");
            entity.Property(e => e.UpdateBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("UPDATE_BY");
            entity.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATE_DATE");
            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("VD_CODE");
        });

        modelBuilder.Entity<DoPlanDev>(entity =>
        {
            entity.HasKey(e => e.RunningId);

            entity.ToTable("DO_PLAN_DEV");

            entity.Property(e => e.RunningId).HasColumnName("RUNNING_ID");
            entity.Property(e => e.PartNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PART_NO");
            entity.Property(e => e.PlanAct).HasColumnName("PLAN_ACT");
            entity.Property(e => e.RunningCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUNNING_CODE");
            entity.Property(e => e.Ymd)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("YMD");
        });

        modelBuilder.Entity<DoReq>(entity =>
        {
            entity.HasKey(e => e.ReqId);

            entity.ToTable("DO_REQ");

            entity.Property(e => e.ReqId).HasColumnName("REQ_ID");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("((0))")
                .HasColumnType("datetime")
                .HasColumnName("CREATE_DATE");
            entity.Property(e => e.PartNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PART_NO");
            entity.Property(e => e.RefNo).HasColumnName("REF_NO");
            entity.Property(e => e.ReqDate)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("REQ_DATE");
            entity.Property(e => e.ReqDay1)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY1");
            entity.Property(e => e.ReqDay10)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY10");
            entity.Property(e => e.ReqDay11)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY11");
            entity.Property(e => e.ReqDay12)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY12");
            entity.Property(e => e.ReqDay13)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY13");
            entity.Property(e => e.ReqDay14)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY14");
            entity.Property(e => e.ReqDay15)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY15");
            entity.Property(e => e.ReqDay16)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY16");
            entity.Property(e => e.ReqDay17)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY17");
            entity.Property(e => e.ReqDay18)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY18");
            entity.Property(e => e.ReqDay19)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY19");
            entity.Property(e => e.ReqDay2)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY2");
            entity.Property(e => e.ReqDay20)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY20");
            entity.Property(e => e.ReqDay21)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY21");
            entity.Property(e => e.ReqDay22)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY22");
            entity.Property(e => e.ReqDay23)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY23");
            entity.Property(e => e.ReqDay24)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY24");
            entity.Property(e => e.ReqDay25)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY25");
            entity.Property(e => e.ReqDay26)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY26");
            entity.Property(e => e.ReqDay27)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY27");
            entity.Property(e => e.ReqDay28)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY28");
            entity.Property(e => e.ReqDay29)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY29");
            entity.Property(e => e.ReqDay3)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY3");
            entity.Property(e => e.ReqDay30)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY30");
            entity.Property(e => e.ReqDay31)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY31");
            entity.Property(e => e.ReqDay4)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY4");
            entity.Property(e => e.ReqDay5)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY5");
            entity.Property(e => e.ReqDay6)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY6");
            entity.Property(e => e.ReqDay7)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY7");
            entity.Property(e => e.ReqDay8)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY8");
            entity.Property(e => e.ReqDay9)
                .HasDefaultValueSql("((0))")
                .HasColumnName("REQ_DAY9");
            entity.Property(e => e.ReqEndDate)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("REQ_END_DATE");
            entity.Property(e => e.ReqMonth)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("REQ_MONTH");
            entity.Property(e => e.ReqYear)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("REQ_YEAR");
            entity.Property(e => e.RunningCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUNNING_CODE");
            entity.Property(e => e.UpdateBy)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("UPDATE_BY");
            entity.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATE_DATE");
            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("VD_CODE");
        });

        modelBuilder.Entity<DoStockAlpha>(entity =>
        {
            entity.HasKey(e => new { e.Partno, e.DatePd, e.Vdcode, e.Cm });

            entity.ToTable("DO_STOCK_ALPHA");

            entity.Property(e => e.Partno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PARTNO");
            entity.Property(e => e.DatePd)
                .HasComment("วันที่ มี STOCK ALPHA")
                .HasColumnName("DATE_PD");
            entity.Property(e => e.Vdcode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("VDCODE");
            entity.Property(e => e.Cm)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("CM");
            entity.Property(e => e.InsertDt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("INSERT_DT");
            entity.Property(e => e.Rev)
                .HasComment("999 คือใช้งาน นอกนั้นให้เก็บเป็น REV = 1, 2 , 3 , ....")
                .HasColumnName("REV");
            entity.Property(e => e.Stock).HasColumnName("STOCK");
        });

        modelBuilder.Entity<DoVenderMaster>(entity =>
        {
            entity.HasKey(e => e.VdCode);

            entity.ToTable("DO_VENDER_MASTER");

            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .HasColumnName("VD_CODE");
            entity.Property(e => e.VdBox).HasColumnName("VD_BOX");
            entity.Property(e => e.VdDay1).HasColumnName("VD_DAY1");
            entity.Property(e => e.VdDay10).HasColumnName("VD_DAY10");
            entity.Property(e => e.VdDay11).HasColumnName("VD_DAY11");
            entity.Property(e => e.VdDay12).HasColumnName("VD_DAY12");
            entity.Property(e => e.VdDay13).HasColumnName("VD_DAY13");
            entity.Property(e => e.VdDay14).HasColumnName("VD_DAY14");
            entity.Property(e => e.VdDay15).HasColumnName("VD_DAY15");
            entity.Property(e => e.VdDay16).HasColumnName("VD_DAY16");
            entity.Property(e => e.VdDay17).HasColumnName("VD_DAY17");
            entity.Property(e => e.VdDay18).HasColumnName("VD_DAY18");
            entity.Property(e => e.VdDay19).HasColumnName("VD_DAY19");
            entity.Property(e => e.VdDay2).HasColumnName("VD_DAY2");
            entity.Property(e => e.VdDay20).HasColumnName("VD_DAY20");
            entity.Property(e => e.VdDay21).HasColumnName("VD_DAY21");
            entity.Property(e => e.VdDay22).HasColumnName("VD_DAY22");
            entity.Property(e => e.VdDay23).HasColumnName("VD_DAY23");
            entity.Property(e => e.VdDay24).HasColumnName("VD_DAY24");
            entity.Property(e => e.VdDay25).HasColumnName("VD_DAY25");
            entity.Property(e => e.VdDay26).HasColumnName("VD_DAY26");
            entity.Property(e => e.VdDay27).HasColumnName("VD_DAY27");
            entity.Property(e => e.VdDay28).HasColumnName("VD_DAY28");
            entity.Property(e => e.VdDay29).HasColumnName("VD_DAY29");
            entity.Property(e => e.VdDay3).HasColumnName("VD_DAY3");
            entity.Property(e => e.VdDay30).HasColumnName("VD_DAY30");
            entity.Property(e => e.VdDay31).HasColumnName("VD_DAY31");
            entity.Property(e => e.VdDay4).HasColumnName("VD_DAY4");
            entity.Property(e => e.VdDay5).HasColumnName("VD_DAY5");
            entity.Property(e => e.VdDay6).HasColumnName("VD_DAY6");
            entity.Property(e => e.VdDay7).HasColumnName("VD_DAY7");
            entity.Property(e => e.VdDay8).HasColumnName("VD_DAY8");
            entity.Property(e => e.VdDay9).HasColumnName("VD_DAY9");
            entity.Property(e => e.VdDesc)
                .HasMaxLength(50)
                .HasColumnName("VD_DESC");
            entity.Property(e => e.VdFri).HasColumnName("VD_FRI");
            entity.Property(e => e.VdLimitBox).HasColumnName("VD_LIMIT_BOX");
            entity.Property(e => e.VdMaxDelivery).HasColumnName("VD_MAX_DELIVERY");
            entity.Property(e => e.VdMinDelivery).HasColumnName("VD_MIN_DELIVERY");
            entity.Property(e => e.VdMon).HasColumnName("VD_MON");
            entity.Property(e => e.VdRound).HasColumnName("VD_ROUND");
            entity.Property(e => e.VdSat).HasColumnName("VD_SAT");
            entity.Property(e => e.VdSun).HasColumnName("VD_SUN");
            entity.Property(e => e.VdThu).HasColumnName("VD_THU");
            entity.Property(e => e.VdTimeScheduleDelivery)
                .HasMaxLength(8)
                .HasColumnName("VD_TIME_SCHEDULE_DELIVERY");
            entity.Property(e => e.VdTue).HasColumnName("VD_TUE");
            entity.Property(e => e.VdWed).HasColumnName("VD_WED");
            entity.Property(e => e.VdProdLead).HasColumnName("VD_PROD_LEAD");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
