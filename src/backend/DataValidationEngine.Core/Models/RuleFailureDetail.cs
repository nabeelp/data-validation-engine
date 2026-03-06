namespace DataValidationEngine.Core.Models;

public record RuleFailureDetail(Guid RuleId, string RuleName, string Scope, string Reason);
