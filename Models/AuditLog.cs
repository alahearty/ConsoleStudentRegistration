namespace ConsoleStudentRegistration.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }

    public DateTime OccurredAt { get; set; }

    public string Actor { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}
