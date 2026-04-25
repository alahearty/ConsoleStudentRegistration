using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class DepartmentService
{
    private readonly SchoolService _schoolService = new();

    public void ManageDepartments()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("Department Management");
            ConsoleHelper.PrintMenuItem("1", "Add New Department");
            ConsoleHelper.PrintMenuItem("2", "View All Departments");
            ConsoleHelper.PrintMenuItem("3", "View Department Details");
            ConsoleHelper.PrintMenuItem("4", "Update Department");
            ConsoleHelper.PrintMenuItem("5", "Deactivate Department");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": AddDepartment(); break;
                case "2": ViewAllDepartments(); break;
                case "3": ViewDepartmentDetails(); break;
                case "4": UpdateDepartment(); break;
                case "5": DeactivateDepartment(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("manage more departments");
        }
    }

    private void AddDepartment()
    {
        ConsoleHelper.PrintSubHeader("Add New Department");

        using var db = new AppDbContext();

        _schoolService.ViewAllSchools();
        var schoolId = ConsoleHelper.ReadInt("Enter School ID");
        var school = db.Schools.Find(schoolId);

        if (school == null)
        {
            ConsoleHelper.PrintError("School not found.");
            return;
        }

        if (!school.IsActive)
        {
            ConsoleHelper.PrintError("Cannot add a department to an inactive school.");
            return;
        }

        var name = ConsoleHelper.ReadInput("Department Name");
        var desc = ConsoleHelper.ReadInput("Description");

        if (string.IsNullOrWhiteSpace(name))
        {
            ConsoleHelper.PrintError("Department Name is required.");
            return;
        }

        if (db.Departments.Any(d => d.DeptName == name && d.SchoolId == schoolId))
        {
            ConsoleHelper.PrintError($"Department '{name}' already exists in this school.");
            return;
        }

        var department = new Department
        {
            DeptName = name,
            Description = desc ?? string.Empty,
            IsActive = true,
            SchoolId = schoolId
        };

        db.Departments.Add(department);
        db.SaveChanges();

        AuditHelper.TryLog("Create", nameof(Department), department.DepartmentId.ToString(), $"{name} / school {schoolId}");
        ConsoleHelper.PrintSuccess($"Department '{name}' added under '{school.SchoolName}'! (ID: {department.DepartmentId})");
    }

    public void ViewAllDepartments()
    {
        ConsoleHelper.PrintSubHeader("All Departments");
        using var db = new AppDbContext();

        var departments = db.Departments
            .Include(d => d.School)
            .Include(d => d.Students)
            .Include(d => d.Courses)
            .OrderBy(d => d.School.SchoolName)
            .ThenBy(d => d.DeptName)
            .ToList();

        var headers = new[] { "ID", "Department", "School", "Students", "Courses", "Status" };
        var rows = departments.Select(d => new[]
        {
            d.DepartmentId.ToString(),
            d.DeptName,
            d.School.SchoolName,
            d.Students.Count(s => s.IsActive).ToString(),
            d.Courses.Count(c => c.IsActive).ToString(),
            d.IsActive ? "Active" : "Inactive"
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
    }

    private void ViewDepartmentDetails()
    {
        ConsoleHelper.PrintSubHeader("Department Details");
        var id = ConsoleHelper.ReadInt("Enter Department ID");

        using var db = new AppDbContext();
        var dept = db.Departments
            .Include(d => d.School)
            .Include(d => d.Students)
            .Include(d => d.Courses)
            .FirstOrDefault(d => d.DepartmentId == id);

        if (dept == null)
        {
            ConsoleHelper.PrintError("Department not found.");
            return;
        }

        ConsoleHelper.PrintHeader($"Department: {dept.DeptName}");
        ConsoleHelper.PrintInfo($"Department ID : {dept.DepartmentId}");
        ConsoleHelper.PrintInfo($"School        : {dept.School.SchoolName}");
        ConsoleHelper.PrintInfo($"Description   : {dept.Description}");
        ConsoleHelper.PrintInfo($"Status        : {(dept.IsActive ? "Active" : "Inactive")}");

        var activeStudents = dept.Students.Where(s => s.IsActive).ToList();
        if (activeStudents.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Students");
            var sHeaders = new[] { "ID", "Name", "Email" };
            var sRows = activeStudents.Select(s => new[]
            {
                s.StudentId.ToString(),
                s.FullName,
                s.Email
            }).ToList();
            ConsoleHelper.PrintTable(sHeaders, sRows);
        }

        var activeCourses = dept.Courses.Where(c => c.IsActive).ToList();
        if (activeCourses.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Courses");
            var cHeaders = new[] { "ID", "Code", "Course Name", "Credits" };
            var cRows = activeCourses.Select(c => new[]
            {
                c.CourseId.ToString(),
                c.CourseCode,
                c.CourseName,
                c.CreditHours.ToString()
            }).ToList();
            ConsoleHelper.PrintTable(cHeaders, cRows);
        }

        ConsoleHelper.Pause();
    }

    private void UpdateDepartment()
    {
        ConsoleHelper.PrintSubHeader("Update Department");
        var id = ConsoleHelper.ReadInt("Enter Department ID");

        using var db = new AppDbContext();
        var dept = db.Departments.Include(d => d.School).FirstOrDefault(d => d.DepartmentId == id);

        if (dept == null)
        {
            ConsoleHelper.PrintError("Department not found.");
            return;
        }

        ConsoleHelper.PrintInfo($"Updating: {dept.DeptName} (School: {dept.School.SchoolName})");
        ConsoleHelper.PrintInfo("Leave blank to keep current value.\n");

        var name = ConsoleHelper.ReadInput($"Department Name [{dept.DeptName}]");
        var desc = ConsoleHelper.ReadInput($"Description [{dept.Description}]");

        if (!string.IsNullOrWhiteSpace(name)) dept.DeptName = name;
        if (!string.IsNullOrWhiteSpace(desc)) dept.Description = desc;

        db.SaveChanges();
        ConsoleHelper.PrintSuccess($"Department '{dept.DeptName}' updated successfully!");
    }

    private void DeactivateDepartment()
    {
        ConsoleHelper.PrintSubHeader("Deactivate Department");
        var id = ConsoleHelper.ReadInt("Enter Department ID");

        using var db = new AppDbContext();
        var dept = db.Departments.Find(id);

        if (dept == null)
        {
            ConsoleHelper.PrintError("Department not found.");
            return;
        }

        if (!dept.IsActive)
        {
            ConsoleHelper.PrintWarning("Department is already inactive.");
            if (ConsoleHelper.AskContinue("reactivate this department"))
            {
                dept.IsActive = true;
                db.SaveChanges();
                ConsoleHelper.PrintSuccess($"Department '{dept.DeptName}' reactivated!");
            }
            return;
        }

        ConsoleHelper.PrintWarning($"You are about to deactivate: {dept.DeptName}");
        if (ConsoleHelper.AskContinue("deactivate this department"))
        {
            dept.IsActive = false;
            db.SaveChanges();
            ConsoleHelper.PrintSuccess($"Department '{dept.DeptName}' has been deactivated.");
        }
    }
}
