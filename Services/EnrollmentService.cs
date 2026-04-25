using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class EnrollmentService
{
    private readonly CourseService _courseService = new();

    public void ManageEnrollments()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("Course Enrollment");
            ConsoleHelper.PrintMenuItem("1", "Enroll Student in Course");
            ConsoleHelper.PrintMenuItem("2", "View Student Enrollments");
            ConsoleHelper.PrintMenuItem("3", "Drop Course");
            ConsoleHelper.PrintMenuItem("4", "View All Enrollments");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": EnrollStudent(); break;
                case "2": ViewStudentEnrollments(); break;
                case "3": DropCourse(); break;
                case "4": ViewAllEnrollments(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("manage more enrollments");
        }
    }

    private void EnrollStudent()
    {
        ConsoleHelper.PrintSubHeader("Enroll Student in Course");

        using var db = new AppDbContext();

        var studentId = ConsoleHelper.ReadInt("Enter Student ID");
        var student = db.Students
            .Include(s => s.Department)
            .FirstOrDefault(s => s.StudentId == studentId);

        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        if (!student.IsActive)
        {
            ConsoleHelper.PrintError("Cannot enroll an inactive student.");
            return;
        }

        ConsoleHelper.PrintInfo($"Student: {student.FullName} (Department: {student.Department.DeptName})");
        if (AppOptions.RequireSameDepartmentEnrollment)
            ConsoleHelper.PrintInfo("Only courses in this student's department are listed.");
        Console.WriteLine();

        var deptFilter = AppOptions.RequireSameDepartmentEnrollment ? student.DepartmentId : (int?)null;
        _courseService.ViewAllCourses(deptFilter);

        var courseId = ConsoleHelper.ReadInt("\n  Enter Course ID to enroll in");
        var course = db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Department)
            .FirstOrDefault(c => c.CourseId == courseId);

        if (course == null)
        {
            ConsoleHelper.PrintError("Course not found.");
            return;
        }

        if (AppOptions.RequireSameDepartmentEnrollment && course.DepartmentId != student.DepartmentId)
        {
            ConsoleHelper.PrintError("Enrollment is restricted to courses in the student's department.");
            return;
        }

        if (!course.IsActive)
        {
            ConsoleHelper.PrintError("This course is no longer active.");
            return;
        }

        var activeCount = course.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        if (activeCount >= course.MaxCapacity)
        {
            ConsoleHelper.PrintError($"Course '{course.CourseCode}' is full ({activeCount}/{course.MaxCapacity}).");
            return;
        }

        var existing = db.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.CourseId == courseId);
        if (existing != null)
        {
            if (existing.Status == EnrollmentStatus.Active)
            {
                ConsoleHelper.PrintWarning("Student is already enrolled in this course.");
                return;
            }

            if (existing.Status == EnrollmentStatus.Dropped)
            {
                ConsoleHelper.PrintInfo("Student previously dropped this course.");
                if (ConsoleHelper.AskContinue("re-enroll"))
                {
                    existing.Status = EnrollmentStatus.Active;
                    existing.EnrolledDate = DateTime.Now;
                    db.SaveChanges();
                    AuditHelper.TryLog("ReEnroll", nameof(Enrollment), existing.EnrollmentId.ToString(), $"{student.FullName} -> {course.CourseCode}");
                    ConsoleHelper.PrintSuccess($"'{student.FullName}' re-enrolled in '{course.CourseCode} - {course.CourseName}'!");
                }
                return;
            }
        }

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolledDate = DateTime.Now,
            Status = EnrollmentStatus.Active
        };

        db.Enrollments.Add(enrollment);
        db.SaveChanges();

        AuditHelper.TryLog("Enroll", nameof(Enrollment), enrollment.EnrollmentId.ToString(), $"{student.FullName} -> {course.CourseCode}");
        ConsoleHelper.PrintSuccess($"'{student.FullName}' enrolled in '{course.CourseCode} - {course.CourseName}' successfully!");
    }

    private void ViewStudentEnrollments()
    {
        ConsoleHelper.PrintSubHeader("Student Enrollments");
        var studentId = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students
            .Include(s => s.Enrollments).ThenInclude(e => e.Course)
            .FirstOrDefault(s => s.StudentId == studentId);

        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        ConsoleHelper.PrintInfo($"Enrollments for: {student.FullName}\n");

        var headers = new[] { "Enrollment ID", "Course Code", "Course Name", "Status", "Date" };
        var rows = student.Enrollments.Select(e => new[]
        {
            e.EnrollmentId.ToString(),
            e.Course.CourseCode,
            e.Course.CourseName,
            EnrollmentStatus.ToLabel(e.Status),
            e.EnrolledDate.ToString("yyyy-MM-dd")
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
    }

    private void DropCourse()
    {
        ConsoleHelper.PrintSubHeader("Drop Course");

        var studentId = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var enrollments = db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .ToList();

        if (enrollments.Count == 0)
        {
            ConsoleHelper.PrintWarning("No active enrollments found for this student.");
            return;
        }

        ConsoleHelper.PrintInfo($"Active courses for Student ID {studentId}:\n");
        var headers = new[] { "Enrollment ID", "Course Code", "Course Name" };
        var rows = enrollments.Select(e => new[]
        {
            e.EnrollmentId.ToString(),
            e.Course.CourseCode,
            e.Course.CourseName
        }).ToList();
        ConsoleHelper.PrintTable(headers, rows);

        var enrollmentId = ConsoleHelper.ReadInt("Enter Enrollment ID to drop");
        var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == enrollmentId);

        if (enrollment == null)
        {
            ConsoleHelper.PrintError("Invalid enrollment ID.");
            return;
        }

        ConsoleHelper.PrintWarning($"Dropping: {enrollment.Course.CourseCode} - {enrollment.Course.CourseName}");
        if (ConsoleHelper.AskContinue("drop this course"))
        {
            enrollment.Status = EnrollmentStatus.Dropped;
            db.SaveChanges();
            AuditHelper.TryLog("Drop", nameof(Enrollment), enrollment.EnrollmentId.ToString(), enrollment.Course.CourseCode);
            ConsoleHelper.PrintSuccess($"Course '{enrollment.Course.CourseCode}' dropped successfully.");
        }
    }

    private void ViewAllEnrollments()
    {
        ConsoleHelper.PrintSubHeader("All Active Enrollments");

        using var db = new AppDbContext();
        var enrollments = db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.Status == EnrollmentStatus.Active)
            .OrderBy(e => e.Student.LastName)
            .ThenBy(e => e.Course.CourseCode)
            .ToList();

        var headers = new[] { "Student", "Course Code", "Course Name", "Enrolled" };
        var rows = enrollments.Select(e => new[]
        {
            e.Student.FullName,
            e.Course.CourseCode,
            e.Course.CourseName,
            e.EnrolledDate.ToString("yyyy-MM-dd")
        }).ToList();

        ConsoleHelper.PrintTablePaged(headers, rows, AppOptions.DefaultPageSize);
    }
}
