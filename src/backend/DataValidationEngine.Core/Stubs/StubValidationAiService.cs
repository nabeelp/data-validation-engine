using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Stubs;

public class StubValidationAiService : IValidationAiService
{
    private readonly ILogger<StubValidationAiService> _logger;

    public StubValidationAiService(ILogger<StubValidationAiService> logger)
    {
        _logger = logger;
    }

    public Task<AiValidationResponse> ValidateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub: ValidateAsync called — SystemPrompt length={SystemLen}, UserPrompt length={UserLen}",
            systemPrompt.Length, userPrompt.Length);

        // Stub returns all rules as passed
        return Task.FromResult(new AiValidationResponse([]));
    }
}
