using System.Globalization;
using System.Text;
using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class ExportService
{
    public void RunExportMenu()
    {
        ConsoleHelper.PrintSubHeader("Export to CSV");
        ConsoleHelper.PrintMenuItem("1", "Export Students");
        ConsoleHelper.PrintMenuItem("2", "Export Courses");
        ConsoleHelper.PrintMenuItem("3", "Export Enrollments (active)");
        ConsoleHelper.PrintMenuItem("0", "Back");

        var choice = ConsoleHelper.ReadInput("Choice");
        switch (choice)
        {
            case "1": ExportStudents(); break;
            case "2": ExportCourses(); break;
            case "3": ExportEnrollments(); break;
            case "0": break;
            default: ConsoleHelper.PrintError("Invalid choice."); break;
        }
    }

    private static string EnsureExportDir()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, AppOptions.ExportDirectory);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void WriteCsv(string path, IEnumerable<string[]> rows)
    {
        static string Esc(string? s)
        {
            s ??= string.Empty;
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        var sb = new StringBuilder();
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Esc)));
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private void ExportStudents()
    {
        using var db = new AppDbContext();
        var list = db.Students
            .Include(s => s.Department)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToList();

        var dir = EnsureExportDir();
        var path = Path.Combine(dir, $"students_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var rows = new List<string[]>
        {
            new[] { "StudentId", "FirstName", "LastName", "Email", "Phone", "Department", "IsActive", "EnrollmentDate" }
        };
        rows.AddRange(list.Select(s => new[]
        {
            s.StudentId.ToString(CultureInfo.InvariantCulture),
            s.FirstName,
            s.LastName,
            s.Email,
            s.Phone,
            s.Department.DeptName,
            s.IsActive ? "1" : "0",
            s.EnrollmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        }));

        WriteCsv(path, rows);
        AuditHelper.TryLog("Export", "StudentsCsv", path, $"{list.Count} rows");
        ConsoleHelper.PrintSuccess($"Exported {list.Count} students to:\n  {path}");
    }

    private void ExportCourses()
    {
        using var db = new AppDbContext();
        var list = db.Courses
            .Include(c => c.Department)
            .OrderBy(c => c.CourseCode)
            .ToList();

        var dir = EnsureExportDir();
        var path = Path.Combine(dir, $"courses_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var rows = new List<string[]>
        {
            new[] { "CourseId", "CourseCode", "CourseName", "Department", "CreditHours", "MaxCapacity", "IsActive" }
        };
        rows.AddRange(list.Select(c => new[]
        {
            c.CourseId.ToString(CultureInfo.InvariantCulture),
            c.CourseCode,
            c.CourseName,
            c.Department.DeptName,
            c.CreditHours.ToString(CultureInfo.InvariantCulture),
            c.MaxCapacity.ToString(CultureInfo.InvariantCulture),
            c.IsActive ? "1" : "0"
        }));

        WriteCsv(path, rows);
        AuditHelper.TryLog("Export", "CoursesCsv", path, $"{list.Count} rows");
        ConsoleHelper.PrintSuccess($"Exported {list.Count} courses to:\n  {path}");
    }

    private void ExportEnrollments()
    {
        using var db = new AppDbContext();
        var list = db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.Status == EnrollmentStatus.Active)
            .OrderBy(e => e.EnrolledDate)
            .ToList();

        var dir = EnsureExportDir();
        var path = Path.Combine(dir, $"enrollments_active_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var rows = new List<string[]>
        {
            new[] { "EnrollmentId", "StudentId", "StudentName", "CourseId", "CourseCode", "EnrolledDate" }
        };
        rows.AddRange(list.Select(e => new[]
        {
            e.EnrollmentId.ToString(CultureInfo.InvariantCulture),
            e.StudentId.ToString(CultureInfo.InvariantCulture),
            e.Student.FullName,
            e.CourseId.ToString(CultureInfo.InvariantCulture),
            e.Course.CourseCode,
            e.EnrolledDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        }));

        WriteCsv(path, rows);
        AuditHelper.TryLog("Export", "EnrollmentsCsv", path, $"{list.Count} rows");
        ConsoleHelper.PrintSuccess($"Exported {list.Count} enrollments to:\n  {path}");
    }
}
