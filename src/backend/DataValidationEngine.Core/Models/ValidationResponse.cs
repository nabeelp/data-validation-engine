namespace DataValidationEngine.Core.Models;

public class ValidationResponse
{
    public string Status { get; set; } = string.Empty;
    public int RulesEvaluated { get; set; }
    public int RulesFailed { get; set; }
    public string? ScopeShortCircuitedAt { get; set; }
    public List<RuleFailureDetail> Failures { get; set; } = [];
}
