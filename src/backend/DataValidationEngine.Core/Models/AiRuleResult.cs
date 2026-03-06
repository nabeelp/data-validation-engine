namespace DataValidationEngine.Core.Models;

public record AiRuleResult(Guid RuleId, bool Passed, string Reason);
