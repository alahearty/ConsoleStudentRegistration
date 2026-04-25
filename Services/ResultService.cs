using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Services;

public class ResultService
{
    private static void MarkEnrollmentCompleted(AppDbContext db, int studentId, int courseId)
    {
        var enrollment = db.Enrollments.FirstOrDefault(e =>
            e.StudentId == studentId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active);
        if (enrollment == null)
            return;
        enrollment.Status = EnrollmentStatus.Completed;
        db.SaveChanges();
    }

    public void ManageResults()
    {
        bool keepGoing = true;
        while (keepGoing)
        {
            ConsoleHelper.PrintHeader("Results & Grades");
            ConsoleHelper.PrintMenuItem("1", "Record Student Result");
            ConsoleHelper.PrintMenuItem("2", "View Student Results");
            ConsoleHelper.PrintMenuItem("3", "View Course Results");
            ConsoleHelper.PrintMenuItem("4", "Calculate GPA");
            ConsoleHelper.PrintMenuItem("5", "Generate Transcript");
            ConsoleHelper.PrintMenuItem("6", "Class Rankings");
            ConsoleHelper.PrintMenuItem("0", "Back to Main Menu");

            var choice = ConsoleHelper.ReadInput("Enter your choice");
            switch (choice)
            {
                case "1": RecordResult(); break;
                case "2": ViewStudentResults(); break;
                case "3": ViewCourseResults(); break;
                case "4": CalculateGpa(); break;
                case "5": GenerateTranscript(); break;
                case "6": ClassRankings(); break;
                case "0": keepGoing = false; continue;
                default: ConsoleHelper.PrintError("Invalid choice."); break;
            }

            if (keepGoing)
                keepGoing = ConsoleHelper.AskContinue("manage more results");
        }
    }

    private void RecordResult()
    {
        ConsoleHelper.PrintSubHeader("Record Student Result");

        using var db = new AppDbContext();

        var studentId = ConsoleHelper.ReadInt("Enter Student ID");
        var student = db.Students.Find(studentId);
        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        var enrollments = db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .ToList();

        if (enrollments.Count == 0)
        {
            ConsoleHelper.PrintError("Student has no active enrollments.");
            return;
        }

        ConsoleHelper.PrintInfo($"Recording result for: {student.FullName}\n");
        ConsoleHelper.PrintInfo("Enrolled courses:");
        foreach (var e in enrollments)
            ConsoleHelper.PrintInfo($"  [{e.Course.CourseId}] {e.Course.CourseCode} - {e.Course.CourseName}");

        var courseId = ConsoleHelper.ReadInt("\n  Enter Course ID");
        if (!enrollments.Any(e => e.Course.CourseId == courseId))
        {
            ConsoleHelper.PrintError("Student is not enrolled in this course.");
            return;
        }

        var semester = ConsoleHelper.ReadInput("Semester (e.g., Fall, Spring, Summer)");
        var year = ConsoleHelper.ReadInput("Academic Year (e.g., 2025/2026)");
        var score = ConsoleHelper.ReadDouble("Score (0-100)", 0, 100);

        if (string.IsNullOrWhiteSpace(semester) || string.IsNullOrWhiteSpace(year))
        {
            ConsoleHelper.PrintError("Semester and Academic Year are required.");
            return;
        }

        var existing = db.Results.FirstOrDefault(r =>
            r.StudentId == studentId && r.CourseId == courseId
            && r.Semester == semester && r.AcademicYear == year);

        if (existing != null)
        {
            ConsoleHelper.PrintWarning($"A result already exists (Score: {existing.Score}, Grade: {existing.Grade}).");
            if (ConsoleHelper.AskContinue("overwrite this result"))
            {
                var (grade, gp) = Result.CalculateGrade(score);
                existing.Score = score;
                existing.Grade = grade;
                existing.GradePoint = gp;
                existing.DateRecorded = DateTime.Now;
                db.SaveChanges();
                MarkEnrollmentCompleted(db, studentId, courseId);
                AuditHelper.TryLog("ResultUpsert", nameof(Result), $"{studentId}/{courseId}", $"{semester} {year} score={score}");
                ConsoleHelper.PrintSuccess($"Result updated: {score:F1} -> Grade {grade} (GP: {gp:F1})");
            }
            return;
        }

        var (newGrade, newGp) = Result.CalculateGrade(score);
        var result = new Result
        {
            StudentId = studentId,
            CourseId = courseId,
            Score = score,
            Grade = newGrade,
            GradePoint = newGp,
            Semester = semester,
            AcademicYear = year,
            DateRecorded = DateTime.Now
        };

        db.Results.Add(result);
        db.SaveChanges();

        MarkEnrollmentCompleted(db, studentId, courseId);
        AuditHelper.TryLog("ResultCreate", nameof(Result), $"{studentId}/{courseId}", $"{semester} {year} score={score}");
        ConsoleHelper.PrintSuccess($"Result recorded: {score:F1} -> Grade {newGrade} (GP: {newGp:F1})");
    }

    private void ViewStudentResults()
    {
        ConsoleHelper.PrintSubHeader("Student Results");
        var studentId = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students.Find(studentId);
        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        var results = db.Results
            .Include(r => r.Course)
            .Where(r => r.StudentId == studentId)
            .OrderBy(r => r.AcademicYear)
            .ThenBy(r => r.Semester)
            .ToList();

        ConsoleHelper.PrintInfo($"Results for: {student.FullName}\n");

        var headers = new[] { "Course", "Score", "Grade", "GP", "Semester", "Year" };
        var rows = results.Select(r => new[]
        {
            r.Course.CourseCode,
            r.Score.ToString("F1"),
            r.Grade,
            r.GradePoint.ToString("F1"),
            r.Semester,
            r.AcademicYear
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);

        if (results.Count != 0)
        {
            var gpa = results.Average(r => r.GradePoint);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  Cumulative GPA: {gpa:F2} / 4.00");
            Console.ResetColor();
        }
    }

    private void ViewCourseResults()
    {
        ConsoleHelper.PrintSubHeader("Course Results");
        var courseId = ConsoleHelper.ReadInt("Enter Course ID");

        using var db = new AppDbContext();
        var course = db.Courses.Find(courseId);
        if (course == null)
        {
            ConsoleHelper.PrintError("Course not found.");
            return;
        }

        var results = db.Results
            .Include(r => r.Student)
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.Score)
            .ToList();

        ConsoleHelper.PrintInfo($"Results for: {course.CourseCode} - {course.CourseName}\n");

        var headers = new[] { "Rank", "Student", "Score", "Grade", "GP", "Semester" };
        var rows = results.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.Student.FullName,
            r.Score.ToString("F1"),
            r.Grade,
            r.GradePoint.ToString("F1"),
            $"{r.Semester} {r.AcademicYear}"
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);

        if (results.Count != 0)
        {
            ConsoleHelper.PrintSubHeader("Statistics");
            ConsoleHelper.PrintInfo($"Highest Score : {results.Max(r => r.Score):F1}");
            ConsoleHelper.PrintInfo($"Lowest Score  : {results.Min(r => r.Score):F1}");
            ConsoleHelper.PrintInfo($"Average Score : {results.Average(r => r.Score):F1}");
            ConsoleHelper.PrintInfo($"Pass Rate     : {(double)results.Count(r => r.Score >= 60) / results.Count * 100:F1}%");
        }
    }

    private void CalculateGpa()
    {
        ConsoleHelper.PrintSubHeader("GPA Calculator");
        var studentId = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students.Find(studentId);
        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        var results = db.Results
            .Include(r => r.Course)
            .Where(r => r.StudentId == studentId)
            .ToList();

        if (results.Count == 0)
        {
            ConsoleHelper.PrintWarning("No results found for this student.");
            return;
        }

        ConsoleHelper.PrintInfo($"GPA Report for: {student.FullName}\n");

        double totalWeightedGp = 0;
        int totalCredits = 0;

        foreach (var r in results)
        {
            totalWeightedGp += r.GradePoint * r.Course.CreditHours;
            totalCredits += r.Course.CreditHours;
        }

        double weightedGpa = totalCredits > 0 ? totalWeightedGp / totalCredits : 0;

        ConsoleHelper.PrintInfo($"Total Courses  : {results.Count}");
        ConsoleHelper.PrintInfo($"Total Credits  : {totalCredits}");
        ConsoleHelper.PrintInfo($"Simple GPA     : {results.Average(r => r.GradePoint):F2}");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n  Weighted GPA (by credit hours): {weightedGpa:F2} / 4.00");

        string standing = weightedGpa switch
        {
            >= 3.7 => "First Class Honours / Dean's List",
            >= 3.3 => "Second Class Upper",
            >= 2.7 => "Second Class Lower",
            >= 2.0 => "Pass",
            _ => "Academic Probation"
        };

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"  Academic Standing: {standing}");
        Console.ResetColor();
    }

    private void GenerateTranscript()
    {
        ConsoleHelper.PrintSubHeader("Academic Transcript");
        var studentId = ConsoleHelper.ReadInt("Enter Student ID");

        using var db = new AppDbContext();
        var student = db.Students.Find(studentId);
        if (student == null)
        {
            ConsoleHelper.PrintError("Student not found.");
            return;
        }

        var results = db.Results
            .Include(r => r.Course)
            .Where(r => r.StudentId == studentId)
            .OrderBy(r => r.AcademicYear)
            .ThenBy(r => r.Semester)
            .ToList();

        if (results.Count == 0)
        {
            ConsoleHelper.PrintWarning("No results found.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  ╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║          OFFICIAL ACADEMIC TRANSCRIPT               ║");
        Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
        Console.ResetColor();

        ConsoleHelper.PrintInfo($"  Student    : {student.FullName}");
        ConsoleHelper.PrintInfo($"  Student ID : {student.StudentId}");
        ConsoleHelper.PrintInfo($"  Email      : {student.Email}");
        ConsoleHelper.PrintInfo($"  Enrolled   : {student.EnrollmentDate:yyyy-MM-dd}");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
        Console.ResetColor();

        var grouped = results.GroupBy(r => $"{r.Semester} {r.AcademicYear}");
        double totalWeightedGp = 0;
        int totalCredits = 0;

        foreach (var group in grouped)
        {
            ConsoleHelper.PrintSubHeader(group.Key);
            var headers = new[] { "Code", "Course Name", "Credits", "Score", "Grade", "GP" };
            var rows = group.Select(r => new[]
            {
                r.Course.CourseCode,
                r.Course.CourseName,
                r.Course.CreditHours.ToString(),
                r.Score.ToString("F1"),
                r.Grade,
                r.GradePoint.ToString("F1")
            }).ToList();

            ConsoleHelper.PrintTable(headers, rows);

            double semGp = 0;
            int semCredits = 0;
            foreach (var r in group)
            {
                semGp += r.GradePoint * r.Course.CreditHours;
                semCredits += r.Course.CreditHours;
                totalWeightedGp += r.GradePoint * r.Course.CreditHours;
                totalCredits += r.Course.CreditHours;
            }

            var semGpa = semCredits > 0 ? semGp / semCredits : 0;
            ConsoleHelper.PrintInfo($"  Semester GPA: {semGpa:F2} | Credits: {semCredits}");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  ╠══════════════════════════════════════════════════════╣");
        Console.ResetColor();

        double cumulativeGpa = totalCredits > 0 ? totalWeightedGp / totalCredits : 0;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Cumulative GPA  : {cumulativeGpa:F2} / 4.00");
        Console.WriteLine($"  Total Credits   : {totalCredits}");
        Console.WriteLine($"  Total Courses   : {results.Count}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ╚══════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.ResetColor();

        ConsoleHelper.Pause();
    }

    private void ClassRankings()
    {
        ConsoleHelper.PrintSubHeader("Class Rankings");

        using var db = new AppDbContext();
        var studentsWithResults = db.Students
            .Include(s => s.Results).ThenInclude(r => r.Course)
            .Where(s => s.Results.Any() && s.IsActive)
            .ToList();

        if (studentsWithResults.Count == 0)
        {
            ConsoleHelper.PrintWarning("No student results available for ranking.");
            return;
        }

        var rankings = studentsWithResults.Select(s =>
        {
            double totalWGp = 0;
            int totalCr = 0;
            foreach (var r in s.Results)
            {
                totalWGp += r.GradePoint * r.Course.CreditHours;
                totalCr += r.Course.CreditHours;
            }
            var gpa = totalCr > 0 ? totalWGp / totalCr : 0;
            return new { Student = s, Gpa = gpa, Courses = s.Results.Count };
        })
        .OrderByDescending(x => x.Gpa)
        .ToList();

        var headers = new[] { "Rank", "Student", "GPA", "Courses", "Standing" };
        var rows = rankings.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.Student.FullName,
            r.Gpa.ToString("F2"),
            r.Courses.ToString(),
            r.Gpa switch
            {
                >= 3.7 => "First Class",
                >= 3.3 => "2nd Upper",
                >= 2.7 => "2nd Lower",
                >= 2.0 => "Pass",
                _ => "Probation"
            }
        }).ToList();

        ConsoleHelper.PrintTable(headers, rows);
        ConsoleHelper.Pause();
    }
}
