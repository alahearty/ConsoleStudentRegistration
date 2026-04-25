using ConsoleStudentRegistration.Helpers;

namespace ConsoleStudentRegistration.Tests;

public class InputValidationTests
{
    [Theory]
    [InlineData("a@b.co", null)]
    [InlineData("", "Email is required.")]
    [InlineData("not-an-email", "Email format is invalid.")]
    [InlineData("spaces @x.com", "Email format is invalid.")]
    public void ValidateEmail_returns_expected(string email, string? expectedError)
    {
        var result = InputValidation.ValidateEmail(email);
        Assert.Equal(expectedError, result);
    }

    [Fact]
    public void ValidatePhone_allows_empty()
    {
        Assert.Null(InputValidation.ValidatePhone(""));
        Assert.Null(InputValidation.ValidatePhone("   "));
    }

    [Theory]
    [InlineData("555-123-4567", null)]
    [InlineData("bad letter", "Phone may only contain")]
    public void ValidatePhone_patterns(string phone, string? containsMessage)
    {
        var result = InputValidation.ValidatePhone(phone);
        if (containsMessage == null)
            Assert.Null(result);
        else
            Assert.Contains(containsMessage, result!);
    }

    [Fact]
    public void ValidateDateOfBirth_rejects_future()
    {
        var err = InputValidation.ValidateDateOfBirth(DateTime.Today.AddDays(1));
        Assert.NotNull(err);
    }

    [Fact]
    public void ValidateDateOfBirth_accepts_reasonable_age()
    {
        var dob = DateTime.Today.AddYears(-20);
        Assert.Null(InputValidation.ValidateDateOfBirth(dob));
    }
}
