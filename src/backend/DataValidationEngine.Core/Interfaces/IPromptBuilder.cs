using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IPromptBuilder
{
    string BuildSystemPrompt();
    string BuildUserPrompt(string fileType, int rowCount, string[] headers, string[] footerRows, string scope, string fileContent, List<ValidationRule> rules);
}
