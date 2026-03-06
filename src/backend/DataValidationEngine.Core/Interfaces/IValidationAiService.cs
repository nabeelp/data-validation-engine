using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IValidationAiService
{
    Task<AiValidationResponse> ValidateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
