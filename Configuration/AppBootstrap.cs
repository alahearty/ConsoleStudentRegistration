using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsoleStudentRegistration.Configuration;

public static class AppBootstrap
{
    public static IConfiguration Configuration { get; private set; } = null!;

    public static ILoggerFactory LoggerFactory { get; private set; } = null!;

    public static void Initialize(string[]? args)
    {
        var basePath = AppContext.BaseDirectory;
        Configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args ?? Array.Empty<string>())
            .Build();

        var cs = Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(cs))
            AppOptions.ConnectionString = cs;

        AppOptions.RequireSameDepartmentEnrollment = Configuration.GetValue(
            "Enrollment:RequireSameDepartment", true);
        AppOptions.DefaultPageSize = Math.Clamp(Configuration.GetValue("Ui:DefaultPageSize", 15), 5, 200);
        AppOptions.ExportDirectory = Configuration["Export:Directory"]?.Trim() ?? "exports";

        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .AddConfiguration(Configuration.GetSection("Logging"))
                .AddSimpleConsole(o =>
                {
                    o.TimestampFormat = "HH:mm:ss ";
                    o.SingleLine = true;
                });
        });
    }

    public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
}
