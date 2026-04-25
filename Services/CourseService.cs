using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class CourseService
{
    private readonly DepartmentService _departmentService = new();

    public void ManageCourses()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("Course Management");
            ConsoleHelper.PrintMenuItem("1", "Add New Course");
            ConsoleHelper.PrintMenuItem("2", "View All Courses");
            ConsoleHelper.PrintMenuItem("3", "View Course Details");
            ConsoleHelper.PrintMenuItem("4", "Update Course");
            ConsoleHelper.PrintMenuItem("5", "Delete Course");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": AddCourse(); break;
                case "2": ViewAllCourses(); break;
                case "3": ViewCourseDetails(); break;
                case "4": UpdateCourse(); break;
                case "5": DeleteCourse(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("manage more courses");
        }
    }

    private void AddCourse()
    {
        ConsoleHelper.PrintSubHeader("Add New Course");

        using var db = new AppDbContext();

        _departmentService.ViewAllDepartments();
        var departmentId = ConsoleHelper.ReadInt("Enter Department ID");
        var department = db.Departments.Find(departmentId);

        if (department == null)
        {
            ConsoleHelper.PrintError("Department not found.");
            return;
        }

        if (!department.IsActive)
        {
            ConsoleHelper.PrintError("Cannot add a course to an inactive department.");
            return;
        }

        var code = ConsoleHelper.ReadInput("Course Code (e.g., CS101)").ToUpper();
        var name = ConsoleHelper.ReadInput("Course Name");
        var desc = ConsoleHelper.ReadInput("Description");
        var credits = ConsoleHelper.ReadInt("Credit Hours", 1, 6);
        var capacity = ConsoleHelper.ReadInt("Max Capacity", 1, 500);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            ConsoleHelper.PrintError("Course Code and Name are required.");
            return;
        }

        if (db.Courses.Any(c => c.CourseCode == code))
        {
            ConsoleHelper.PrintError($"Course code '{code}' already exists.");
            return;
        }

        var course = new Course
        {
            CourseCode = code,
            CourseName = name,
            Description = desc,
            CreditHours = credits,
            MaxCapacity = capacity,
            IsActive = true,
            DepartmentId = departmentId
        };

        db.Courses.Add(course);
        db.SaveChanges();

        AuditHelper.TryLog("Create", nameof(Course), course.CourseId.ToString(), $"{code} {name}");
        ConsoleHelper.PrintSuccess($"Course '{code} - {name}' added successfully! (ID: {course.CourseId})");
    }

    public void ViewAllCourses(int? restrictToDepartmentId = null)
    {
        if (restrictToDepartmentId.HasValue)
            ConsoleHelper.PrintSubHeader($"Courses (Department ID {restrictToDepartmentId} only)");
        else
            ConsoleHelper.PrintSubHeader("All Available Courses");

        using var db = new AppDbContext();

        var query = db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Department)
            .Where(c => c.IsActive);

        if (restrictToDepartmentId.HasValue)
            query = query.Where(c => c.DepartmentId == restrictToDepartmentId.Value);

        var courses = query.OrderBy(c => c.CourseCode).ToList();

        var headers = new[] { "ID", "Code", "Course Name", "Department", "Credits", "Enrolled", "Capacity" };
        var rows = courses.Select(c => new[]
        {
            c.CourseId.ToString(),
            c.CourseCode,
            c.CourseName,
            c.Department.DeptName,
            c.CreditHours.ToString(),
            c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active).ToString(),
            c.MaxCapacity.ToString()
        }).ToList();

        ConsoleHelper.PrintTablePaged(headers, rows, AppOptions.DefaultPageSize);
    }

    private void ViewCourseDetails()
    {
        ConsoleHelper.PrintSubHeader("Course Details");
        var id = ConsoleHelper.ReadInt("Enter Course ID");

        using var db = new AppDbContext();
        var course = db.Courses
            .Include(c => c.Department).ThenInclude(d => d.School)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .FirstOrDefault(c => c.CourseId == id);

        if (course == null)
        {
            ConsoleHelper.PrintError("Course not found.");
            return;
        }

        ConsoleHelper.PrintHeader($"{course.CourseCode} - {course.CourseName}");
        ConsoleHelper.PrintInfo($"Description  : {course.Description}");
        ConsoleHelper.PrintInfo($"Department   : {course.Department.DeptName}");
        ConsoleHelper.PrintInfo($"School       : {course.Department.School.SchoolName}");
        ConsoleHelper.PrintInfo($"Credit Hours : {course.CreditHours}");
        ConsoleHelper.PrintInfo($"Capacity     : {course.Enrollments.Count(e => e.Status == EnrollmentStatus.Active)} / {course.MaxCapacity}");
        ConsoleHelper.PrintInfo($"Status       : {(course.IsActive ? "Active" : "Inactive")}");

        var enrolledStudents = course.Enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
        if (enrolledStudents.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Enrolled Students");
            var headers = new[] { "Student ID", "Name", "Email", "Enrolled Date" };
            var rows = enrolledStudents.Select(e => new[]
            {
                e.Student.StudentId.ToString(),
                e.Student.FullName,
                e.Student.Email,
                e.EnrolledDate.ToString("yyyy-MM-dd")
            }).ToList();
            ConsoleHelper.PrintTable(headers, rows);
        }

        ConsoleHelper.Pause();
    }

    private void UpdateCourse()
    {
        ConsoleHelper.PrintSubHeader("Update Course");
        var id = ConsoleHelper.ReadInt("Enter Course ID");

        using var db = new AppDbContext();
        var course = db.Courses.Find(id);

        if (course == null)
        {
            ConsoleHelper.PrintError("Course not found.");
            return;
        }

        ConsoleHelper.PrintInfo($"Updating: {course.CourseCode} - {course.CourseName}");
        ConsoleHelper.PrintInfo("Leave blank to keep current value.\n");

        var name = ConsoleHelper.ReadInput($"Course Name [{course.CourseName}]");
        var desc = ConsoleHelper.ReadInput($"Description [{course.Description}]");
        var creditsStr = ConsoleHelper.ReadInput($"Credit Hours [{course.CreditHours}]");
        var capacityStr = ConsoleHelper.ReadInput($"Max Capacity [{course.MaxCapacity}]");

        if (!string.IsNullOrWhiteSpace(name)) course.CourseName = name;
        if (!string.IsNullOrWhiteSpace(desc)) course.Description = desc;
        if (int.TryParse(creditsStr, out int credits) && credits >= 1) course.CreditHours = credits;
        if (int.TryParse(capacityStr, out int capacity) && capacity >= 1) course.MaxCapacity = capacity;

        try
        {
            db.SaveChanges();
            AuditHelper.TryLog("Update", nameof(Course), course.CourseId.ToString(), course.CourseCode);
            ConsoleHelper.PrintSuccess($"Course '{course.CourseCode}' updated successfully!");
        }
        catch (DbUpdateConcurrencyException)
        {
            ConsoleHelper.PrintError("This course was modified by another process. Refresh and try again.");
        }
    }

    private void DeleteCourse()
    {
        ConsoleHelper.PrintSubHeader("Delete Course");
        var id = ConsoleHelper.ReadInt("Enter Course ID");

        using var db = new AppDbContext();
        var course = db.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefault(c => c.CourseId == id);

        if (course == null)
        {
            ConsoleHelper.PrintError("Course not found.");
            return;
        }

        if (course.Enrollments.Any(e => e.Status == EnrollmentStatus.Active))
        {
            ConsoleHelper.PrintError("Cannot delete a course with active enrollments. Drop all students first.");
            return;
        }

        ConsoleHelper.PrintWarning($"You are about to delete: {course.CourseCode} - {course.CourseName}");
        if (ConsoleHelper.AskContinue("permanently delete this course"))
        {
            course.IsActive = false;
            db.SaveChanges();
            AuditHelper.TryLog("Deactivate", nameof(Course), course.CourseId.ToString(), course.CourseCode);
            ConsoleHelper.PrintSuccess($"Course '{course.CourseCode}' deleted (deactivated).");
        }
    }
}
