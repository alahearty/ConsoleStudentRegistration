using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

AppBootstrap.Initialize(args);
var startupLog = AppBootstrap.CreateLogger("Startup");

Console.Title = "Student Registration System";

try
{
    ConsoleHelper.PrintHeader("Connecting to Database...");
    startupLog.LogInformation("Opening database connection...");
    using (var db = new AppDbContext())
    {
        if (!db.Database.CanConnect())
            throw new Exception("Cannot connect to the database.");

        InitializeDatabaseSchema(db, startupLog);

        var courseCount = db.Courses.Count();
        startupLog.LogInformation("Connected. Courses loaded: {Count}", courseCount);
        ConsoleHelper.PrintSuccess($"Connected to PostgreSQL successfully! ({courseCount} courses loaded)");
    }
}
catch (Exception ex)
{
    startupLog.LogError(ex, "Database connection failed");
    ConsoleHelper.PrintError($"Database connection failed: {ex.Message}");
    ConsoleHelper.PrintInfo("Make sure PostgreSQL is running and the database has been created.");
    ConsoleHelper.PrintInfo("Run DatabaseSetup.sql (or Database_AddAuditLogsOnly.sql if schema exists) against PostgreSQL.");
    ConsoleHelper.PrintInfo("Edit appsettings.json or set env ConnectionStrings__DefaultConnection for credentials.");
    ConsoleHelper.PrintInfo("Default DB: studentregistrationdb on localhost:5432");
    ConsoleHelper.Pause();
    return;
}

void InitializeDatabaseSchema(AppDbContext db, ILogger startupLog)
{
    try
    {
        // Check if Schools table exists by attempting a simple query
        db.Schools.Any();
        startupLog.LogInformation("Database schema already exists.");
    }
    catch
    {
        // Table doesn't exist, create schema from SQL file
        startupLog.LogInformation("Creating database schema from DatabaseSetup.sql...");
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "DatabaseSetup.sql");
        
        if (!File.Exists(scriptPath))
        {
            startupLog.LogWarning("DatabaseSetup.sql not found at {Path}. Attempting EnsureCreated().", scriptPath);
            db.Database.EnsureCreated();
            return;
        }

        var sqlScript = File.ReadAllText(scriptPath);
        
        // Split SQL statements and execute them individually
        var statements = sqlScript.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var statement in statements)
        {
            var trimmedStatement = statement.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedStatement))
            {
                try
                {
                    db.Database.ExecuteSqlRaw(trimmedStatement);
                }
                catch (Exception ex)
                {
                    startupLog.LogWarning(ex, "Failed to execute SQL statement: {Statement}", trimmedStatement.Substring(0, Math.Min(50, trimmedStatement.Length)));
                }
            }
        }
        startupLog.LogInformation("Database schema created successfully.");
    }
}

var studentService = new StudentService();
var courseService = new CourseService();
var enrollmentService = new EnrollmentService();
var resultService = new ResultService();
var reportService = new ReportService();
var schoolService = new SchoolService();
var departmentService = new DepartmentService();

bool running = true;

while (running)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(@"
  ╔══════════════════════════════════════════════════════════════╗
  ║                                                              ║
  ║        STUDENT REGISTRATION MANAGEMENT SYSTEM                ║
  ║              (Database-First Approach)                       ║
  ║                                                              ║
  ╠══════════════════════════════════════════════════════════════╣
  ║                                                              ║");
    
    Console.ResetColor();

    ConsoleHelper.PrintMenuItem("1", "School Management        (Add, View, Update, Deactivate)");
    ConsoleHelper.PrintMenuItem("2", "Department Management    (Add, View, Update, Deactivate)");
    ConsoleHelper.PrintMenuItem("3", "Student Management       (Register, View, Update, Search)");
    ConsoleHelper.PrintMenuItem("4", "Course Management        (Add, View, Update, Delete)");
    ConsoleHelper.PrintMenuItem("5", "Course Enrollment        (Enroll, Drop, View)");
    ConsoleHelper.PrintMenuItem("6", "Results & Grades         (Record, GPA, Transcript, Rankings)");
    ConsoleHelper.PrintMenuItem("7", "Reports & Statistics     (Dashboard, Charts, Analytics)");

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ║                                                              ║");
    Console.ResetColor();

    ConsoleHelper.PrintMenuItem("0", "Exit System");

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ║                                                              ║");
    Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
    Console.ResetColor();

    var choice = ConsoleHelper.ReadInput("Select an option");

    switch (choice)
    {
        case "1":
            schoolService.ManageSchools();
            break;
        case "2":
            departmentService.ManageDepartments();
            break;
        case "3":
            studentService.ManageStudents();
            break;
        case "4":
            courseService.ManageCourses();
            break;
        case "5":
            enrollmentService.ManageEnrollments();
            break;
        case "6":
            resultService.ManageResults();
            break;
        case "7":
            reportService.ViewReports();
            break;
        case "0":
            if (ConsoleHelper.AskContinue("exit the system"))
            {
                running = false;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(@"
  ╔══════════════════════════════════════════════════════════════╗
  ║                                                              ║
  ║     Thank you for using Student Registration System!         ║
  ║                       Goodbye!                               ║
  ║                                                              ║
  ╚══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
            }
            break;
        default:
            ConsoleHelper.PrintError("Invalid option. Please try again.");
            ConsoleHelper.Pause();
            break;
    }
}
