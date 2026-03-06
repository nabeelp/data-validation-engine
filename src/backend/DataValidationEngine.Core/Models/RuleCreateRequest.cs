namespace DataValidationEngine.Core.Models;

public class RuleCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleText { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
