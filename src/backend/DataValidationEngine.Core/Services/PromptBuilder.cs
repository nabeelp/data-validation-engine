using System.Text;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Services;

public class PromptBuilder : IPromptBuilder
{
    public string BuildSystemPrompt()
    {
        return """
            You are a data file validation assistant. Evaluate the provided file content against each rule
            and return ONLY a valid JSON object matching this schema exactly:
            { "results": [ { "rule_id": string, "passed": boolean, "reason": string } ] }
            Do not include any text outside the JSON object.
            """;
    }

    public string BuildUserPrompt(
        string fileType,
        int rowCount,
        string[] headers,
        string[] footerRows,
        string scope,
        string fileContent,
        List<ValidationRule> rules)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"File type: {fileType}. Row count: {rowCount}. Headers: {string.Join(", ", headers)}. Footer rows: {string.Join("; ", footerRows)}.");
        sb.AppendLine();
        sb.AppendLine($"File content (scope: {scope}):");
        sb.AppendLine(fileContent);
        sb.AppendLine();
        sb.AppendLine("Rules to evaluate:");

        for (var i = 0; i < rules.Count; i++)
        {
            sb.AppendLine($"{i + 1}. [rule_id: {rules[i].Id}] \"{rules[i].RuleText}\"");
        }

        return sb.ToString();
    }
}
