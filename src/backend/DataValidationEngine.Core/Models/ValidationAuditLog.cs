namespace DataValidationEngine.Core.Models;

public class ValidationAuditLog
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
    public string OverallResult { get; set; } = string.Empty;
    public string? RulesEvaluated { get; set; }
    public string? AiResponse { get; set; }
    public string? ScopesEvaluated { get; set; }
}
