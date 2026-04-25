namespace ConsoleStudentRegistration.Models;

public partial class Result
{
    public static (string Grade, double GradePoint) CalculateGrade(double score) => score switch
    {
        >= 90 => ("A", 4.0),
        >= 80 => ("B", 3.0),
        >= 70 => ("C", 2.0),
        >= 60 => ("D", 1.0),
        _ => ("F", 0.0)
    };
}
