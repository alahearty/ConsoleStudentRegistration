using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class StudentService
{
    private readonly DepartmentService _departmentService = new();

    public void ManageStudents()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("Student Management");
            ConsoleHelper.PrintMenuItem("1", "Register New Student");
            ConsoleHelper.PrintMenuItem("2", "View All Students");
            ConsoleHelper.PrintMenuItem("3", "Search Student");
            ConsoleHelper.PrintMenuItem("4", "Update Student");
            ConsoleHelper.PrintMenuItem("5", "Deactivate Student");
            ConsoleHelper.PrintMenuItem("6", "View Student Profile");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": RegisterStudent(); break;
                case "2": ViewAllStudents(); break;
                case "3": SearchStudent(); break;
                case "4": UpdateStudent(); break;
                case "5": DeactivateStudent(); break;
                case "6": ViewStudentProfile(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("manage more students");
        }
    }

    private void RegisterStudent()
    {
        ConsoleHelper.PrintSubHeader("Register New Student");

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
            ConsoleHelper.PrintError("Cannot register a student to an inactive department.");
            return;
        }

        var firstName = ConsoleHelper.ReadInput("First Name");
        var lastName = ConsoleHelper.ReadInput("Last Name");
        var email = ConsoleHelper.ReadInput("Email");
        var phone = ConsoleHelper.ReadInput("Phone Number");
        var dob = ConsoleHelper.ReadDate("Date of Birth");

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
        {
            ConsoleHelper.PrintError("First Name, Last Name, and Email are required.");
            return;
        }

        var emailErr = InputValidation.ValidateEmail(email);
        if (emailErr != null)
        {
            ConsoleHelper.PrintError(emailErr);
            return;
        }

        var phoneErr = InputValidation.ValidatePhone(phone);
        if (phoneErr != null)
        {
            ConsoleHelper.PrintError(phoneErr);
            return;
        }

        var dobErr = InputValidation.ValidateDateOfBirth(dob);
        if (dobErr != null)
        {
            ConsoleHelper.PrintError(dobErr);
            return;
        }

        if (db.Students.Any(s => s.Email == email))
        {
            ConsoleHelper.PrintError($"A student with email '{email}' already exists.");
            return;
        }

        var student = new Student
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            DateOfBirth = dob,
            EnrollmentDate = DateTime.Now,
            IsActive = true,
            DepartmentId = departmentId
        };

        db.Students.Add(student);
        db.SaveChanges();

        AuditHelper.TryLog("Register", nameof(Student), student.StudentId.ToString(), student.Email);
        ConsoleHelper.PrintSuccess($"Student '{student.FullName}' registered successfully! (ID: {student.StudentId})");
    }

    private void ViewAllStudents()
    {
        ConsoleHelper.PrintSubHeader("All Students");
        using var db = new AppDbContext();

        var students = db.Students
            .Include(s => s.Department)
            .OrderBy(s => s.LastName)
            .ToList();
        var headers = new[] { "ID", "Name", "Email", "Department", "Status", "Enrolled" };
        var rows = students.Select(s => new[]
        {
            s.StudentId.ToString(),
            s.FullName,
            s.Email,
            s.Department.DeptName,
            s.IsActive ? "Active" : "Inactive",
            s.EnrollmentDate.ToString("yyyy-MM-dd")
        }).ToList();

        ConsoleHelper.PrintTablePaged(headers, rows, AppOptions.DefaultPageSize);
    }

    private void SearchStudent()
    {
        ConsoleHelper.PrintSubHeader("Search Student");
        var keyword = ConsoleHelper.ReadInput("Enter name or email to search");

        using var db = new AppDbContext();
        var students = db.Students
            .Where(s => s.FirstName.ToLower().Contains(keyword.ToLower())
                     || s.LastName.ToLower().Contains(keyword.ToLower())
                     || s.Email.ToLower().Contains(keyword.ToLower()))
            .ToList();

        var headers = new[] { "ID", "Name", "Email", "Phone", "Status" };
        var rows = students.Select(s => new[]
        {
            s.StudentId.ToString(),
            s.FullName,
            s.Email,
            s.Phone,
            s.IsActive ? "Active" : "Inactive"
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
    }

    private void UpdateStudent()
    {
        ConsoleHelper.PrintSubHeader("Update Student");
        var id = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students.Find(id);

        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        ConsoleHelper.PrintInfo($"Updating: {student.FullName} ({student.Email})");
        ConsoleHelper.PrintInfo("Leave blank to keep current value.\n");

        var firstName = ConsoleHelper.ReadInput($"First Name [{student.FirstName}]");
        var lastName = ConsoleHelper.ReadInput($"Last Name [{student.LastName}]");
        var email = ConsoleHelper.ReadInput($"Email [{student.Email}]");
        var phone = ConsoleHelper.ReadInput($"Phone [{student.Phone}]");

        if (!string.IsNullOrWhiteSpace(firstName)) student.FirstName = firstName;
        if (!string.IsNullOrWhiteSpace(lastName)) student.LastName = lastName;

        if (!string.IsNullOrWhiteSpace(email) && email != student.Email)
        {
            var ev = InputValidation.ValidateEmail(email);
            if (ev != null)
            {
                ConsoleHelper.PrintError(ev);
                return;
            }
            if (db.Students.Any(s => s.Email == email && s.StudentId != id))
            {
                ConsoleHelper.PrintError("That email is already taken by another student.");
                return;
            }
            student.Email = email;
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var pv = InputValidation.ValidatePhone(phone);
            if (pv != null)
            {
                ConsoleHelper.PrintError(pv);
                return;
            }

            student.Phone = phone;
        }

        try
        {
            db.SaveChanges();
            AuditHelper.TryLog("Update", nameof(Student), student.StudentId.ToString(), student.Email);
            ConsoleHelper.PrintSuccess($"Student '{student.FullName}' updated successfully!");
        }
        catch (DbUpdateConcurrencyException)
        {
            ConsoleHelper.PrintError("This student was modified elsewhere. Refresh and try again.");
        }
    }

    private void DeactivateStudent()
    {
        ConsoleHelper.PrintSubHeader("Deactivate Student");
        var id = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students.Find(id);

        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        if (!student.IsActive)
        {
            ConsoleHelper.PrintWarning("Student is already inactive.");
            if (ConsoleHelper.AskContinue("reactivate this student"))
            {
                student.IsActive = true;
                db.SaveChanges();
                AuditHelper.TryLog("Reactivate", nameof(Student), student.StudentId.ToString(), student.FullName);
                ConsoleHelper.PrintSuccess($"Student '{student.FullName}' reactivated!");
            }
            return;
        }

        ConsoleHelper.PrintWarning($"You are about to deactivate: {student.FullName}");
        if (ConsoleHelper.AskContinue("deactivate this student"))
        {
            student.IsActive = false;
            db.SaveChanges();
            AuditHelper.TryLog("Deactivate", nameof(Student), student.StudentId.ToString(), student.FullName);
            ConsoleHelper.PrintSuccess($"Student '{student.FullName}' has been deactivated.");
        }
    }

    private void ViewStudentProfile()
    {
        ConsoleHelper.PrintSubHeader("Student Profile");
        var id = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students
            .Include(s => s.Department).ThenInclude(d => d.School)
            .Include(s => s.Enrollments).ThenInclude(e => e.Course)
            .Include(s => s.Results).ThenInclude(r => r.Course)
            .FirstOrDefault(s => s.StudentId == id);

        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        ConsoleHelper.PrintHeader($"Profile: {student.FullName}");
        ConsoleHelper.PrintInfo($"Student ID   : {student.StudentId}");
        ConsoleHelper.PrintInfo($"Email        : {student.Email}");
        ConsoleHelper.PrintInfo($"Phone        : {student.Phone}");
        ConsoleHelper.PrintInfo($"Department   : {student.Department.DeptName}");
        ConsoleHelper.PrintInfo($"School       : {student.Department.School.SchoolName}");
        ConsoleHelper.PrintInfo($"Date of Birth: {student.DateOfBirth:yyyy-MM-dd}");
        ConsoleHelper.PrintInfo($"Enrolled On  : {student.EnrollmentDate:yyyy-MM-dd}");
        ConsoleHelper.PrintInfo($"Status       : {(student.IsActive ? "Active" : "Inactive")}");

        var activeEnrollments = student.Enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
        if (activeEnrollments.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Enrolled Courses");
            var headers = new[] { "Course Code", "Course Name", "Enrolled Date" };
            var rows = activeEnrollments.Select(e => new[]
            {
                e.Course.CourseCode,
                e.Course.CourseName,
                e.EnrolledDate.ToString("yyyy-MM-dd")
            }).ToList();
            ConsoleHelper.PrintTable(headers, rows);
        }

        if (student.Results.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Academic Results");
            var headers = new[] { "Course", "Score", "Grade", "GP", "Semester" };
            var rows = student.Results.Select(r => new[]
            {
                r.Course.CourseCode,
                r.Score.ToString("F1"),
                r.Grade,
                r.GradePoint.ToString("F1"),
                $"{r.Semester} {r.AcademicYear}"
            }).ToList();
            ConsoleHelper.PrintTable(headers, rows);

            var gpa = student.Results.Average(r => r.GradePoint);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  Cumulative GPA: {gpa:F2} / 4.00");
            Console.ResetColor();
        }

        ConsoleHelper.Pause();
    }
}
