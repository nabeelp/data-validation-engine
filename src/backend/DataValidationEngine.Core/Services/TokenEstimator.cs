namespace DataValidationEngine.Core.Services;

public static class TokenEstimator
{
    private const double CharsPerToken = 4.0;
    private const double SafetyMargin = 0.80;

    public static int EstimateTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    public static int GetEffectiveBudget(int maxTokensPerRequest)
    {
        return (int)(maxTokensPerRequest * SafetyMargin);
    }

    public static int CalculateBatchSize(
        int totalDataRows,
        int defaultBatchThreshold,
        int maxTokensPerRequest,
        string systemPrompt,
        string ruleText,
        int avgCharsPerRow)
    {
        var budget = GetEffectiveBudget(maxTokensPerRequest);
        var fixedTokens = EstimateTokens(systemPrompt) + EstimateTokens(ruleText);
        var availableTokens = budget - fixedTokens;

        if (availableTokens <= 0)
            return 1;

        var tokensPerRow = EstimateTokens(new string('x', avgCharsPerRow));
        var maxRowsPerBatch = availableTokens / tokensPerRow;

        if (maxRowsPerBatch <= 0)
            return 1;

        return Math.Min(defaultBatchThreshold, maxRowsPerBatch);
    }
}
