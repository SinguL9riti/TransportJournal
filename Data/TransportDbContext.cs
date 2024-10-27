using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TransportJournal.Models;

namespace TransportJournal.Data;

public partial class TransportDbContext : DbContext
{
    public TransportDbContext()
    {
    }

    public TransportDbContext(DbContextOptions<TransportDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Personnel> Personnel { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Stop> Stops { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server=KENG;database=TransportDB;Integrated Security=true;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Personnel>(entity =>
        {
            entity.HasKey(e => e.PersonnelId).HasName("PK__Personne__CAFBCB6F9EAC3E10");

            entity.Property(e => e.PersonnelId).HasColumnName("PersonnelID");
            entity.Property(e => e.EmployeeList).HasMaxLength(400);
            entity.Property(e => e.RouteId).HasColumnName("RouteID");
            entity.Property(e => e.Shift).HasMaxLength(10);

            entity.HasOne(d => d.Route).WithMany(p => p.Personnel)
                .HasForeignKey(d => d.RouteId)
                .HasConstraintName("FK__Personnel__Route__3E52440B");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId).HasName("PK__Routes__80979AADEC73A096");

            entity.Property(e => e.RouteId).HasColumnName("RouteID");
            entity.Property(e => e.Distance).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.TransportType).HasMaxLength(50);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B699903EF54");

            entity.ToTable("Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.RouteId).HasColumnName("RouteID");
            entity.Property(e => e.Weekday).HasMaxLength(15);

            entity.HasOne(d => d.Route).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RouteId)
                .HasConstraintName("FK__Schedule__RouteI__3B75D760");
        });

        modelBuilder.Entity<Stop>(entity =>
        {
            entity.HasKey(e => e.StopId).HasName("PK__Stops__EB6A38D44B26B2F9");

            entity.Property(e => e.StopId).HasColumnName("StopID");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
