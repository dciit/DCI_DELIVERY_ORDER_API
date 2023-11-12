using System;
using System.Collections.Generic;
using DeliveryOrderAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryOrderAPI.Contexts;

public partial class employee_context : DbContext
{
    public employee_context()
    {
    }

    public employee_context(DbContextOptions<employee_context> options)
        : base(options)
    {
    }

    public virtual DbSet<Employee> Employees { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.226.86;Database=dbHRM;TrustServerCertificate=True;uid=sa;password=decjapan");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Code);

            entity.ToTable("Employee");

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("CODE");
            entity.Property(e => e.Andon)
                .HasMaxLength(500)
                .HasColumnName("ANDON");
            entity.Property(e => e.AnnualcalDt).HasColumnName("ANNUALCAL_DT");
            entity.Property(e => e.Birth).HasColumnName("BIRTH");
            entity.Property(e => e.Budgettype)
                .HasMaxLength(10)
                .HasColumnName("BUDGETTYPE");
            entity.Property(e => e.Bus)
                .HasMaxLength(50)
                .HasColumnName("BUS");
            entity.Property(e => e.Company)
                .HasMaxLength(50)
                .HasColumnName("COMPANY");
            entity.Property(e => e.Costcenter)
                .HasMaxLength(50)
                .HasColumnName("COSTCENTER");
            entity.Property(e => e.Dlrate)
                .HasMaxLength(50)
                .HasColumnName("DLRATE");
            entity.Property(e => e.Dvcd)
                .HasMaxLength(10)
                .HasColumnName("DVCD");
            entity.Property(e => e.Grpot)
                .HasMaxLength(50)
                .HasColumnName("GRPOT");
            entity.Property(e => e.Join).HasColumnName("JOIN");
            entity.Property(e => e.LineCode).HasMaxLength(100);
            entity.Property(e => e.Location)
                .HasMaxLength(500)
                .HasColumnName("LOCATION");
            entity.Property(e => e.Mail)
                .HasMaxLength(50)
                .HasColumnName("MAIL");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("NAME");
            entity.Property(e => e.Nickname)
                .HasMaxLength(50)
                .HasColumnName("NICKNAME");
            entity.Property(e => e.Otype)
                .HasMaxLength(50)
                .HasColumnName("OTYPE");
            entity.Property(e => e.PGrade)
                .HasMaxLength(10)
                .HasColumnName("P_GRADE");
            entity.Property(e => e.PRank)
                .HasMaxLength(10)
                .HasColumnName("P_RANK");
            entity.Property(e => e.Posit)
                .HasMaxLength(50)
                .HasColumnName("POSIT");
            entity.Property(e => e.Pren)
                .HasMaxLength(50)
                .HasColumnName("PREN");
            entity.Property(e => e.Religion)
                .HasMaxLength(50)
                .HasColumnName("RELIGION");
            entity.Property(e => e.Resign).HasColumnName("RESIGN");
            entity.Property(e => e.Rstype)
                .HasMaxLength(50)
                .HasColumnName("RSTYPE");
            entity.Property(e => e.Sex)
                .HasMaxLength(20)
                .HasColumnName("SEX");
            entity.Property(e => e.Stop)
                .HasMaxLength(50)
                .HasColumnName("STOP");
            entity.Property(e => e.Surn)
                .HasMaxLength(200)
                .HasColumnName("SURN");
            entity.Property(e => e.Tcaddr3)
                .HasMaxLength(50)
                .HasColumnName("TCADDR3");
            entity.Property(e => e.Tcaddr4)
                .HasMaxLength(50)
                .HasColumnName("TCADDR4");
            entity.Property(e => e.Tctel)
                .HasMaxLength(50)
                .HasColumnName("TCTEL");
            entity.Property(e => e.Telephone)
                .HasMaxLength(50)
                .HasColumnName("TELEPHONE");
            entity.Property(e => e.Tname)
                .HasMaxLength(200)
                .HasColumnName("TNAME");
            entity.Property(e => e.TposijoinDt).HasColumnName("TPOSIJOIN_DT");
            entity.Property(e => e.Tposiname)
                .HasMaxLength(150)
                .HasColumnName("TPOSINAME");
            entity.Property(e => e.Tpren)
                .HasMaxLength(20)
                .HasColumnName("TPREN");
            entity.Property(e => e.Tsex)
                .HasMaxLength(20)
                .HasColumnName("TSEX");
            entity.Property(e => e.Tsurn)
                .HasMaxLength(200)
                .HasColumnName("TSURN");
            entity.Property(e => e.Workcenter)
                .HasMaxLength(10)
                .HasColumnName("WORKCENTER");
            entity.Property(e => e.Wsts)
                .HasMaxLength(50)
                .HasColumnName("WSTS");
            entity.Property(e => e.Wtype)
                .HasMaxLength(50)
                .HasColumnName("WTYPE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
