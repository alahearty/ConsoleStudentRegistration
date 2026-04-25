using ConsoleStudentRegistration.Data;
using ConsoleStudentRegistration.Models;

namespace ConsoleStudentRegistration.Helpers;

public static class AuditHelper
{
    public static void TryLog(string action, string entityType, string entityId, string? details = null)
    {
        try
        {
            using var db = new AppDbContext();
            db.AuditLogs.Add(new AuditLog
            {
                OccurredAt = DateTime.Now,
                Actor = Environment.UserName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details ?? string.Empty
            });
            db.SaveChanges();
        }
        catch
        {
            // Never fail business operations because audit storage failed
        }
    }
}
