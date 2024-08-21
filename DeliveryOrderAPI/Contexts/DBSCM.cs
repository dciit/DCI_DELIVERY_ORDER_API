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

    public virtual DbSet<DoDictMstr> DoDictMstrs { get; set; }

    public virtual DbSet<DoHistory> DoHistories { get; set; }

    public virtual DbSet<DoHistoryDev> DoHistoryDevs { get; set; }

    public virtual DbSet<DoMaster> DoMasters { get; set; }

    public virtual DbSet<DoPartMaster> DoPartMasters { get; set; }

    public virtual DbSet<DoVenderMaster> DoVenderMasters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.226.86;Database=dbSCM;TrustServerCertificate=True;uid=sa;password=decjapan");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Thai_CI_AS");

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

            entity.HasIndex(e => e.RunningCode, "IX_DO_HISTORY_DEV").IsDescending();

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
            entity.HasKey(e => new { e.PartId, e.Partno, e.Cm });

            entity.ToTable("DO_PART_MASTER");

            entity.Property(e => e.PartId)
                .ValueGeneratedOnAdd()
                .HasColumnName("PART_ID");
            entity.Property(e => e.Partno)
                .HasMaxLength(30)
                .HasColumnName("PARTNO");
            entity.Property(e => e.Cm)
                .HasMaxLength(5)
                .HasColumnName("CM");
            entity.Property(e => e.Active)
                .HasMaxLength(10)
                .HasColumnName("ACTIVE");
            entity.Property(e => e.BoxMax).HasColumnName("BOX_MAX");
            entity.Property(e => e.BoxMin).HasColumnName("BOX_MIN");
            entity.Property(e => e.BoxQty).HasColumnName("BOX_QTY");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.Diameter)
                .HasMaxLength(20)
                .HasColumnName("DIAMETER");
            entity.Property(e => e.Pdlt).HasColumnName("PDLT");
            entity.Property(e => e.Unit)
                .HasMaxLength(10)
                .HasColumnName("UNIT");
            entity.Property(e => e.UpdateBy)
                .HasMaxLength(30)
                .HasColumnName("UPDATE_BY");
            entity.Property(e => e.UpdateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("UPDATE_DATE");
            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .HasColumnName("VD_CODE");
        });

        modelBuilder.Entity<DoVenderMaster>(entity =>
        {
            entity.HasKey(e => e.VdCode);

            entity.ToTable("DO_VENDER_MASTER");

            entity.Property(e => e.VdCode)
                .HasMaxLength(10)
                .HasColumnName("VD_CODE");
            entity.Property(e => e.VdBox).HasColumnName("VD_BOX");
            entity.Property(e => e.VdBoxPeriod)
                .HasDefaultValueSql("((0))")
                .HasColumnName("VD_BOX_PERIOD");
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
            entity.Property(e => e.VdProdLead)
                .HasDefaultValueSql("((3))")
                .HasColumnName("VD_PROD_LEAD");
            entity.Property(e => e.VdRound).HasColumnName("VD_ROUND");
            entity.Property(e => e.VdSat).HasColumnName("VD_SAT");
            entity.Property(e => e.VdSun).HasColumnName("VD_SUN");
            entity.Property(e => e.VdThu).HasColumnName("VD_THU");
            entity.Property(e => e.VdTimeScheduleDelivery)
                .HasMaxLength(8)
                .HasColumnName("VD_TIME_SCHEDULE_DELIVERY");
            entity.Property(e => e.VdTue).HasColumnName("VD_TUE");
            entity.Property(e => e.VdWed).HasColumnName("VD_WED");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
