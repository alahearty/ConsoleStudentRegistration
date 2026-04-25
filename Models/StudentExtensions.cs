namespace ConsoleStudentRegistration.Models;

public partial class Student
{
    public string FullName => $"{FirstName} {LastName}";
}
