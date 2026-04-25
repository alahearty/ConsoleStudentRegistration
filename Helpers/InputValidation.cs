using System.Text.RegularExpressions;

namespace ConsoleStudentRegistration.Helpers;

public static class InputValidation
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email is required.";
        if (email.Length > 100)
            return "Email must be at most 100 characters.";
        if (!EmailRegex.IsMatch(email))
            return "Email format is invalid.";
        return null;
    }

    public static string? ValidatePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;
        if (phone.Length > 15)
            return "Phone must be at most 15 characters.";
        if (!Regex.IsMatch(phone, @"^[\d\s\-+().]+$"))
            return "Phone may only contain digits, spaces, and -+().";
        return null;
    }

    public static string? ValidateDateOfBirth(DateTime dob)
    {
        var today = DateTime.Today;
        if (dob.Date > today)
            return "Date of birth cannot be in the future.";
        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age)) age--;
        if (age < 5)
            return "Student must be at least 5 years old.";
        if (age > 120)
            return "Date of birth is not realistic.";
        return null;
    }
}
