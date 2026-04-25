using ConsoleStudentRegistration.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ConsoleStudentRegistration.Data;

public partial class AppDbContext
{
    public AppDbContext(): base(CreateOptions())
    {
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        builder.UseNpgsql(AppOptions.ConnectionString);
        return builder.Options;
    }
}
