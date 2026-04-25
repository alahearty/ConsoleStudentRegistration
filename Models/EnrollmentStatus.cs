namespace ConsoleStudentRegistration.Models;

public static class EnrollmentStatus
{
    public const int Active = 0;
    public const int Dropped = 1;
    public const int Completed = 2;

    public static string ToLabel(int status) => status switch
    {
        Active => "Active",
        Dropped => "Dropped",
        Completed => "Completed",
        _ => "Unknown"
    };
}
