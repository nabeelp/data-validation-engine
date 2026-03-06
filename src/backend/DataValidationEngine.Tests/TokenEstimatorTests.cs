using DataValidationEngine.Core.Services;

namespace DataValidationEngine.Tests;

public class TokenEstimatorTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("abcd", 1)]
    [InlineData("12345678", 2)]
    [InlineData("abc", 1)]
    public void EstimateTokens_ReturnsCorrectCount(string text, int expected)
    {
        var result = TokenEstimator.EstimateTokens(text);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetEffectiveBudget_Applies20PercentSafetyMargin()
    {
        var budget = TokenEstimator.GetEffectiveBudget(128000);
        Assert.Equal(102400, budget);
    }

    [Fact]
    public void CalculateBatchSize_RespectsDefaultThreshold()
    {
        var batchSize = TokenEstimator.CalculateBatchSize(
            totalDataRows: 1000,
            defaultBatchThreshold: 500,
            maxTokensPerRequest: 128000,
            systemPrompt: "short",
            ruleText: "short rule",
            avgCharsPerRow: 50);

        Assert.True(batchSize <= 500);
        Assert.True(batchSize > 0);
    }

    [Fact]
    public void CalculateBatchSize_ReducesWhenTokenBudgetTight()
    {
        var batchSize = TokenEstimator.CalculateBatchSize(
            totalDataRows: 1000,
            defaultBatchThreshold: 500,
            maxTokensPerRequest: 100,
            systemPrompt: new string('x', 200),
            ruleText: new string('x', 100),
            avgCharsPerRow: 50);

        Assert.True(batchSize < 500);
    }

    [Fact]
    public void CalculateBatchSize_ReturnsAtLeastOne_WhenBudgetExhausted()
    {
        var batchSize = TokenEstimator.CalculateBatchSize(
            totalDataRows: 1000,
            defaultBatchThreshold: 500,
            maxTokensPerRequest: 10,
            systemPrompt: new string('x', 1000),
            ruleText: new string('x', 1000),
            avgCharsPerRow: 50);

        Assert.Equal(1, batchSize);
    }
}
