using System;
using System.Collections.Generic;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("Schools_pkey");

            entity.Property(e => e.SchoolName).HasMaxLength(100);
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("Departments_pkey");

            entity.Property(e => e.DeptName).HasMaxLength(100);
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.School).WithMany(p => p.Departments)
                .HasForeignKey(d => d.SchoolId)
                .HasConstraintName("FK_Departments_Schools");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("Courses_pkey");

            entity.HasIndex(e => e.CourseCode, "IX_Courses_CourseCode").IsUnique();

            entity.Property(e => e.CourseCode).HasMaxLength(10);
            entity.Property(e => e.CourseName).HasMaxLength(100);
            entity.Property(e => e.CreditHours).HasDefaultValue(3);
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxCapacity).HasDefaultValue(40);

            entity.HasOne(d => d.Department).WithMany(p => p.Courses)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Courses_Departments");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("Enrollments_pkey");

            entity.HasIndex(e => new { e.StudentId, e.CourseId }, "IX_Enrollments_StudentId_CourseId").IsUnique();

            entity.Property(e => e.EnrolledDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Course).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_Enrollments_Courses");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Enrollments_Students");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("Results_pkey");

            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.Semester, e.AcademicYear }, "IX_Results_StudentId_CourseId_Semester_AcademicYear").IsUnique();

            entity.Property(e => e.AcademicYear)
                .HasMaxLength(10)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.DateRecorded)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Grade)
                .HasMaxLength(2)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.Semester)
                .HasMaxLength(20)
                .HasDefaultValueSql("''::character varying");

            entity.HasOne(d => d.Course).WithMany(p => p.Results)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_Results_Courses");

            entity.HasOne(d => d.Student).WithMany(p => p.Results)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Results_Students");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("Students_pkey");

            entity.HasIndex(e => e.Email, "IX_Students_Email").IsUnique();

            entity.Property(e => e.DateOfBirth).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EnrollmentDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasDefaultValueSql("''::character varying");

            entity.HasOne(d => d.Department).WithMany(p => p.Students)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Students_Departments");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
