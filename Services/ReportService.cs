using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class ReportService
{
    private readonly ExportService _exportService = new();

    public void ViewReports()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("Reports & Statistics");
            ConsoleHelper.PrintMenuItem("1", "Dashboard Summary");
            ConsoleHelper.PrintMenuItem("2", "Student Enrollment Report");
            ConsoleHelper.PrintMenuItem("3", "Course Popularity Report");
            ConsoleHelper.PrintMenuItem("4", "Grade Distribution");
            ConsoleHelper.PrintMenuItem("5", "Department Summary (students, courses, GPA)");
            ConsoleHelper.PrintMenuItem("6", "School Summary");
            ConsoleHelper.PrintMenuItem("7", "Recent Audit Log");
            ConsoleHelper.PrintMenuItem("8", "Export Data (CSV)");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": DashboardSummary(); break;
                case "2": EnrollmentReport(); break;
                case "3": CoursePopularityReport(); break;
                case "4": GradeDistribution(); break;
                case "5": DepartmentSummary(); break;
                case "6": SchoolSummary(); break;
                case "7": RecentAuditLog(); break;
                case "8": _exportService.RunExportMenu(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("view more reports");
        }
    }

    private void DashboardSummary()
    {
        ConsoleHelper.PrintHeader("Dashboard Summary");

        using var db = new AppDbContext();

        var totalStudents = db.Students.Count();
        var activeStudents = db.Students.Count(s => s.IsActive);
        var totalCourses = db.Courses.Count(c => c.IsActive);
        var activeEnrollments = db.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        var totalResults = db.Results.Count();
        var totalSchools = db.Schools.Count(s => s.IsActive);
        var totalDepartments = db.Departments.Count(d => d.IsActive);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  ┌─────────────────────────────────────────┐");
        Console.WriteLine("  │           SYSTEM DASHBOARD               │");
        Console.WriteLine("  ├─────────────────────────────────────────┤");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  │  Active Schools       : {totalSchools,-16}│");
        Console.WriteLine($"  │  Active Departments   : {totalDepartments,-16}│");
        Console.WriteLine($"  │  Total Students      : {totalStudents,-16}│");
        Console.WriteLine($"  │  Active Students     : {activeStudents,-16}│");
        Console.WriteLine($"  │  Inactive Students   : {totalStudents - activeStudents,-16}│");
        Console.WriteLine($"  │  Active Courses      : {totalCourses,-16}│");
        Console.WriteLine($"  │  Active Enrollments  : {activeEnrollments,-16}│");
        Console.WriteLine($"  │  Results Recorded    : {totalResults,-16}│");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  └─────────────────────────────────────────┘");
        Console.ResetColor();

        if (totalResults > 0)
        {
            var avgScore = db.Results.Average(r => r.Score);
            var avgGpa = db.Results.Average(r => r.GradePoint);
            var passRate = (double)db.Results.Count(r => r.Score >= 60) / totalResults * 100;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  Average Score : {avgScore:F1}");
            Console.WriteLine($"  Average GPA   : {avgGpa:F2}");
            Console.WriteLine($"  Pass Rate     : {passRate:F1}%");
            Console.ResetColor();
        }

        ConsoleHelper.Pause();
    }

    private void EnrollmentReport()
    {
        ConsoleHelper.PrintSubHeader("Student Enrollment Report");

        using var db = new AppDbContext();
        var students = db.Students
            .Include(s => s.Enrollments)
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName)
            .ToList();

        var headers = new[] { "Student", "Active", "Dropped", "Completed", "Total" };
        var rows = students.Select(s =>
        {
            var active = s.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
            var dropped = s.Enrollments.Count(e => e.Status == EnrollmentStatus.Dropped);
            var completed = s.Enrollments.Count(e => e.Status == EnrollmentStatus.Completed);
            return new[]
            {
                s.FullName,
                active.ToString(),
                dropped.ToString(),
                completed.ToString(),
                s.Enrollments.Count.ToString()
            };
        }).ToList();

        ConsoleHelper.PrintTablePaged(headers, rows, AppOptions.DefaultPageSize);
        ConsoleHelper.Pause();
    }

    private void CoursePopularityReport()
    {
        ConsoleHelper.PrintSubHeader("Course Popularity Report");

        using var db = new AppDbContext();
        var courses = db.Courses
            .Include(c => c.Enrollments)
            .Where(c => c.IsActive)
            .ToList()
            .OrderByDescending(c => c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active))
            .ToList();

        var headers = new[] { "Course", "Enrolled", "Capacity", "Fill %", "Dropped" };
        var rows = courses.Select(c =>
        {
            var active = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
            var dropped = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Dropped);
            var fill = c.MaxCapacity > 0 ? (double)active / c.MaxCapacity * 100 : 0;
            return new[]
            {
                $"{c.CourseCode} - {c.CourseName}",
                active.ToString(),
                c.MaxCapacity.ToString(),
                $"{fill:F0}%",
                dropped.ToString()
            };
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
        ConsoleHelper.Pause();
    }

    private void GradeDistribution()
    {
        ConsoleHelper.PrintSubHeader("Grade Distribution");

        using var db = new AppDbContext();
        var results = db.Results.ToList();

        if (results.Count == 0)
        {
            ConsoleHelper.PrintWarning("No results recorded yet.");
            return;
        }

        var grades = new[] { "A", "B", "C", "D", "F" };
        var total = results.Count;

        Console.WriteLine();
        foreach (var grade in grades)
        {
            var count = results.Count(r => r.Grade == grade);
            var pct = (double)count / total * 100;
            var bar = new string('#', (int)(pct / 2));

            Console.ForegroundColor = grade switch
            {
                "A" => ConsoleColor.Green,
                "B" => ConsoleColor.Cyan,
                "C" => ConsoleColor.Yellow,
                "D" => ConsoleColor.DarkYellow,
                "F" => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.WriteLine($"  Grade {grade}: {bar,-25} {count,4} ({pct:F1}%)");
        }

        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  Total results: {total}");
        Console.ResetColor();

        ConsoleHelper.Pause();
    }

    private void DepartmentSummary()
    {
        ConsoleHelper.PrintSubHeader("Department Summary");

        using var db = new AppDbContext();
        var departments = db.Departments
            .Include(d => d.School)
            .Include(d => d.Students)
            .Include(d => d.Courses)
            .ThenInclude(c => c.Enrollments)
            .Include(d => d.Courses)
            .ThenInclude(c => c.Results)
            .Where(d => d.IsActive)
            .OrderBy(d => d.School.SchoolName)
            .ThenBy(d => d.DeptName)
            .ToList();

        var headers = new[] { "Dept", "School", "Students", "Courses", "ActiveEnr", "AvgGPA" };
        var rows = departments.Select(d =>
        {
            var studentCount = d.Students.Count(s => s.IsActive);
            var courseCount = d.Courses.Count(c => c.IsActive);
            var activeEnr = d.Courses.SelectMany(c => c.Enrollments).Count(e => e.Status == EnrollmentStatus.Active);
            var grades = d.Courses.SelectMany(c => c.Results).Select(r => r.GradePoint).ToList();
            var avgGpa = grades.Count > 0 ? grades.Average() : 0d;
            return new[]
            {
                d.DeptName,
                d.School.SchoolName,
                studentCount.ToString(),
                courseCount.ToString(),
                activeEnr.ToString(),
                grades.Count > 0 ? avgGpa.ToString("F2") : "—"
            };
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
        ConsoleHelper.Pause();
    }

    private void SchoolSummary()
    {
        ConsoleHelper.PrintSubHeader("School Summary");

        using var db = new AppDbContext();
        var schools = db.Schools
            .Include(s => s.Departments)
            .ThenInclude(d => d.Students)
            .Include(s => s.Departments)
            .ThenInclude(d => d.Courses)
            .Where(s => s.IsActive)
            .OrderBy(s => s.SchoolName)
            .ToList();

        var headers = new[] { "School", "Departments", "Students", "Courses" };
        var rows = schools.Select(s =>
        {
            var deptCount = s.Departments.Count(d => d.IsActive);
            var students = s.Departments.SelectMany(d => d.Students).Count(st => st.IsActive);
            var courses = s.Departments.SelectMany(d => d.Courses).Count(c => c.IsActive);
            return new[]
            {
                s.SchoolName,
                deptCount.ToString(),
                students.ToString(),
                courses.ToString()
            };
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
        ConsoleHelper.Pause();
    }

    private void RecentAuditLog()
    {
        ConsoleHelper.PrintSubHeader("Recent Audit Log (latest 50)");

        using var db = new AppDbContext();
        var logs = db.AuditLogs
            .OrderByDescending(a => a.OccurredAt)
            .Take(50)
            .ToList();

        if (logs.Count == 0)
        {
            ConsoleHelper.PrintWarning("No audit entries yet.");
            ConsoleHelper.Pause();
            return;
        }

        var headers = new[] { "When", "Actor", "Action", "Entity", "Id", "Details" };
        var rows = logs.Select(a => new[]
        {
            a.OccurredAt.ToString("yyyy-MM-dd HH:mm"),
            a.Actor,
            a.Action,
            a.EntityType,
            a.EntityId,
            a.Details.Length > 28 ? a.Details[..25] + "..." : a.Details
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
        ConsoleHelper.Pause();
    }
}
