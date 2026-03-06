using DataValidationEngine.Core.Services;

namespace DataValidationEngine.Tests;

public class PromptBuilderTests
{
    private readonly PromptBuilder _builder = new();

    [Fact]
    public void BuildSystemPrompt_ContainsJsonSchema()
    {
        var prompt = _builder.BuildSystemPrompt();

        Assert.Contains("results", prompt);
        Assert.Contains("rule_id", prompt);
        Assert.Contains("passed", prompt);
        Assert.Contains("reason", prompt);
        Assert.Contains("JSON", prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesAllExpectedParts()
    {
        var rules = new List<DataValidationEngine.Core.Models.ValidationRule>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Test Rule",
                RuleText = "Amount must be positive",
                Scope = "RECORD",
                FileType = "CSV",
                IsActive = true
            }
        };

        var prompt = _builder.BuildUserPrompt(
            fileType: "CSV",
            rowCount: 5,
            headers: ["Date", "Amount"],
            footerRows: ["TOTAL,,100"],
            scope: "RECORD",
            fileContent: "2026-01-01,100\n2026-01-02,200",
            rules: rules);

        Assert.Contains("File type: CSV", prompt);
        Assert.Contains("Row count: 5", prompt);
        Assert.Contains("Date, Amount", prompt);
        Assert.Contains("scope: RECORD", prompt);
        Assert.Contains("2026-01-01,100", prompt);
        Assert.Contains("Amount must be positive", prompt);
        Assert.Contains("11111111-1111-1111-1111-111111111111", prompt);
    }
}
