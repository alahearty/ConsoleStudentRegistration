namespace ConsoleStudentRegistration.Configuration;

public static class AppOptions
{
    public static string ConnectionString { get; set; } = string.Empty;

    public static bool RequireSameDepartmentEnrollment { get; set; } = true;

    public static int DefaultPageSize { get; set; } = 15;

    public static string ExportDirectory { get; set; } = "exports";
}
