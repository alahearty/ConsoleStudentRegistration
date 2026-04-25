using ConsoleStudentRegistration.Configuration;
using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Helpers;
using ConsoleStudentRegistration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Nodes;

AppBootstrap.Initialize(args);
var startupLog = AppBootstrap.CreateLogger("Startup");

Console.Title = "Student Registration System";

try
{
    ConsoleHelper.PrintHeader("Connecting to Database...");
    ConnectAndInitializeDatabase(startupLog);
}
catch (Exception ex)
{
    startupLog.LogError(ex, "Database connection failed");
    ConsoleHelper.PrintError($"Database connection failed: {ex.Message}");
    ConsoleHelper.PrintInfo("Make sure PostgreSQL is running and the database has been created.");
    ConsoleHelper.PrintInfo("The app now creates the target database automatically when the PostgreSQL server is reachable.");
    ConsoleHelper.PrintInfo("If the schema file is missing from the build output, rebuild the solution and run again.");
    ConsoleHelper.PrintInfo("Edit appsettings.json or set env ConnectionStrings__DefaultConnection for credentials.");
    ConsoleHelper.PrintInfo("Default DB: studentregistrationdb on localhost:5432");
    ConsoleHelper.Pause();
    return;
}

void ConnectAndInitializeDatabase(ILogger startupLog)
{
    Exception? lastError = null;

    for (var attempt = 1; attempt <= 2; attempt++)
    {
        try
        {
            startupLog.LogInformation("Opening database connection...");
            EnsureDatabaseExists(startupLog);

            using var db = new AppDbContext();
            if (!db.Database.CanConnect())
                throw new Exception("Cannot connect to the database.");

            InitializeDatabaseSchema(db, startupLog);

            var courseCount = db.Courses.Count();
            startupLog.LogInformation("Connected. Courses loaded: {Count}", courseCount);
            ConsoleHelper.PrintSuccess($"Connected to PostgreSQL successfully! ({courseCount} courses loaded)");
            return;
        }
        catch (Exception ex)
        {
            if (attempt == 1 && TryRepairConnectionSettings(ex, startupLog))
            {
                lastError = ex;
                startupLog.LogInformation("Retrying database connection with updated settings...");
                continue;
            }

            lastError = ex;
            break;
        }
    }

    throw lastError ?? new InvalidOperationException("Database initialization failed.");
}

void EnsureDatabaseExists(ILogger startupLog)
{
    var configuredConnection = AppOptions.ConnectionString;
    if (string.IsNullOrWhiteSpace(configuredConnection))
        throw new InvalidOperationException("No PostgreSQL connection string was configured.");

    var databaseBuilder = new NpgsqlConnectionStringBuilder(configuredConnection);
    if (string.IsNullOrWhiteSpace(databaseBuilder.Database))
        throw new InvalidOperationException("The PostgreSQL connection string must include a database name.");

    var targetDatabase = databaseBuilder.Database;
    var adminBuilder = new NpgsqlConnectionStringBuilder(configuredConnection)
    {
        Database = "postgres"
    };

    using var adminConnection = new NpgsqlConnection(adminBuilder.ConnectionString);
    adminConnection.Open();

    using var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", adminConnection);
    existsCommand.Parameters.AddWithValue("dbName", targetDatabase);

    if (existsCommand.ExecuteScalar() is not null)
    {
        startupLog.LogInformation("Verified database {Database} exists.", targetDatabase);
        return;
    }

    startupLog.LogInformation("Database {Database} was not found. Creating it now...", targetDatabase);
    var quotedDatabaseName = QuoteIdentifier(targetDatabase);
    using var createCommand = new NpgsqlCommand($"CREATE DATABASE {quotedDatabaseName}", adminConnection);
    createCommand.ExecuteNonQuery();
    startupLog.LogInformation("Database {Database} created successfully.", targetDatabase);
}

bool TryRepairConnectionSettings(Exception ex, ILogger startupLog)
{
    if (!IsConnectionSetupError(ex))
        return false;

    var current = new NpgsqlConnectionStringBuilder(AppOptions.ConnectionString);

    ConsoleHelper.PrintWarning("PostgreSQL rejected the current connection settings.");
    ConsoleHelper.PrintInfo("Enter working PostgreSQL details to retry immediately.");

    var host = ConsoleHelper.ReadInput("PostgreSQL host", string.IsNullOrWhiteSpace(current.Host) ? "localhost" : current.Host);
    var portText = ConsoleHelper.ReadInput("PostgreSQL port", current.Port <= 0 ? "5432" : current.Port.ToString());
    var database = ConsoleHelper.ReadInput("Database name", string.IsNullOrWhiteSpace(current.Database) ? "studentregistrationdb" : current.Database);
    var username = ConsoleHelper.ReadInput("Username", string.IsNullOrWhiteSpace(current.Username) ? "postgres" : current.Username);
    var password = ConsoleHelper.ReadSecret("Password");

    if (!int.TryParse(portText, out var port) || port <= 0)
    {
        ConsoleHelper.PrintError("The PostgreSQL port must be a positive number.");
        return false;
    }

    current.Host = host;
    current.Port = port;
    current.Database = database;
    current.Username = username;
    if (!string.IsNullOrWhiteSpace(password))
        current.Password = password;

    AppOptions.ConnectionString = current.ConnectionString;
    PersistConnectionString(current.ConnectionString, startupLog);
    return true;
}

bool IsConnectionSetupError(Exception ex)
{
    if (ex is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.InvalidPassword)
        return true;

    if (ex is NpgsqlException)
        return true;

    return ex.InnerException is not null && IsConnectionSetupError(ex.InnerException);
}

void PersistConnectionString(string connectionString, ILogger startupLog)
{
    foreach (var settingsPath in FindAppSettingsPaths())
    {
        try
        {
            JsonObject root;
            if (File.Exists(settingsPath))
            {
                root = JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var connectionStrings = root["ConnectionStrings"] as JsonObject ?? new JsonObject();
            connectionStrings["DefaultConnection"] = connectionString;
            root["ConnectionStrings"] = connectionStrings;

            File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception fileEx)
        {
            startupLog.LogWarning(fileEx, "Failed to persist connection string to {Path}", settingsPath);
        }
    }
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
        var scriptPath = FindDatabaseScriptPath();
        
        if (scriptPath is null)
        {
            startupLog.LogWarning("DatabaseSetup.sql was not found. Attempting EnsureCreated().");
            db.Database.EnsureCreated();
            return;
        }

        var sqlScript = File.ReadAllText(scriptPath);
        db.Database.ExecuteSqlRaw(sqlScript);
        startupLog.LogInformation("Database schema created successfully.");
    }
}

string? FindDatabaseScriptPath()
{
    var baseDirectory = AppContext.BaseDirectory;
    var candidates = new[]
    {
        Path.Combine(baseDirectory, "DatabaseSetup.sql"),
        Path.Combine(Directory.GetCurrentDirectory(), "DatabaseSetup.sql"),
        Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "DatabaseSetup.sql"))
    };

    return candidates.FirstOrDefault(File.Exists);
}

IEnumerable<string> FindAppSettingsPaths()
{
    var baseDirectory = AppContext.BaseDirectory;
    return new[]
    {
        Path.Combine(baseDirectory, "appsettings.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
        Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "appsettings.json"))
    }
    .Distinct(StringComparer.OrdinalIgnoreCase);
}

string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

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
