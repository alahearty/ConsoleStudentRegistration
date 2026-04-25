using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class SchoolService
{
    public void ManageSchools()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("School Management");
            ConsoleHelper.PrintMenuItem("1", "Add New School");
            ConsoleHelper.PrintMenuItem("2", "View All Schools");
            ConsoleHelper.PrintMenuItem("3", "View School Details");
            ConsoleHelper.PrintMenuItem("4", "Update School");
            ConsoleHelper.PrintMenuItem("5", "Deactivate School");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": AddSchool(); break;
                case "2": ViewAllSchools(); break;
                case "3": ViewSchoolDetails(); break;
                case "4": UpdateSchool(); break;
                case "5": DeactivateSchool(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("manage more schools");
        }
    }

    private void AddSchool()
    {
        ConsoleHelper.PrintSubHeader("Add New School");

        var name = ConsoleHelper.ReadInput("School Name");
        var desc = ConsoleHelper.ReadInput("Description");

        if (string.IsNullOrWhiteSpace(name))
        {
            ConsoleHelper.PrintError("School Name is required.");
            return;
        }

        using var db = new AppDbContext();
        if (db.Schools.Any(s => s.SchoolName == name))
        {
            ConsoleHelper.PrintError($"A school with name '{name}' already exists.");
            return;
        }

        var school = new School
        {
            SchoolName = name,
            Description = desc ?? string.Empty,
            IsActive = true
        };

        db.Schools.Add(school);
        db.SaveChanges();

        AuditHelper.TryLog("Create", nameof(School), school.SchoolId.ToString(), name);
        ConsoleHelper.PrintSuccess($"School '{name}' added successfully! (ID: {school.SchoolId})");
    }

    public void ViewAllSchools()
    {
        ConsoleHelper.PrintSubHeader("All Schools");
        using var db = new AppDbContext();

        var schools = db.Schools
            .Include(s => s.Departments)
            .OrderBy(s => s.SchoolName)
            .ToList();

        var headers = new[] { "ID", "School Name", "Description", "Departments", "Status" };
        var rows = schools.Select(s => new[]
        {
            s.SchoolId.ToString(),
            s.SchoolName,
            s.Description,
            s.Departments.Count(d => d.IsActive).ToString(),
            s.IsActive ? "Active" : "Inactive"
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
    }

    private void ViewSchoolDetails()
    {
        ConsoleHelper.PrintSubHeader("School Details");
        var id = ConsoleHelper.ReadInt("Enter School ID");

        using var db = new AppDbContext();
        var school = db.Schools
            .Include(s => s.Departments)
            .FirstOrDefault(s => s.SchoolId == id);

        if (school == null)
        {
            ConsoleHelper.PrintError("School not found.");
            return;
        }

        ConsoleHelper.PrintHeader($"School: {school.SchoolName}");
        ConsoleHelper.PrintInfo($"School ID    : {school.SchoolId}");
        ConsoleHelper.PrintInfo($"Description  : {school.Description}");
        ConsoleHelper.PrintInfo($"Status       : {(school.IsActive ? "Active" : "Inactive")}");

        var activeDepts = school.Departments.Where(d => d.IsActive).ToList();
        if (activeDepts.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Departments");
            var headers = new[] { "ID", "Department Name", "Description" };
            var rows = activeDepts.Select(d => new[]
            {
                d.DepartmentId.ToString(),
                d.DeptName,
                d.Description
            }).ToList();
            ConsoleHelper.PrintTable(headers, rows);
        }
        else
        {
            ConsoleHelper.PrintInfo("No active departments in this school.");
        }

        ConsoleHelper.Pause();
    }

    private void UpdateSchool()
    {
        ConsoleHelper.PrintSubHeader("Update School");
        var id = ConsoleHelper.ReadInt("Enter School ID");

        using var db = new AppDbContext();
        var school = db.Schools.Find(id);

        if (school == null)
        {
            ConsoleHelper.PrintError("School not found.");
            return;
        }

        ConsoleHelper.PrintInfo($"Updating: {school.SchoolName}");
        ConsoleHelper.PrintInfo("Leave blank to keep current value.\n");

        var name = ConsoleHelper.ReadInput($"School Name [{school.SchoolName}]");
        var desc = ConsoleHelper.ReadInput($"Description [{school.Description}]");

        if (!string.IsNullOrWhiteSpace(name)) school.SchoolName = name;
        if (!string.IsNullOrWhiteSpace(desc)) school.Description = desc;

        db.SaveChanges();
        ConsoleHelper.PrintSuccess($"School '{school.SchoolName}' updated successfully!");
    }

    private void DeactivateSchool()
    {
        ConsoleHelper.PrintSubHeader("Deactivate School");
        var id = ConsoleHelper.ReadInt("Enter School ID");

        using var db = new AppDbContext();
        var school = db.Schools.Find(id);

        if (school == null)
        {
            ConsoleHelper.PrintError("School not found.");
            return;
        }

        if (!school.IsActive)
        {
            ConsoleHelper.PrintWarning("School is already inactive.");
            if (ConsoleHelper.AskContinue("reactivate this school"))
            {
                school.IsActive = true;
                db.SaveChanges();
                ConsoleHelper.PrintSuccess($"School '{school.SchoolName}' reactivated!");
            }
            return;
        }

        ConsoleHelper.PrintWarning($"You are about to deactivate: {school.SchoolName}");
        if (ConsoleHelper.AskContinue("deactivate this school"))
        {
            school.IsActive = false;
            db.SaveChanges();
            ConsoleHelper.PrintSuccess($"School '{school.SchoolName}' has been deactivated.");
        }
    }
}
