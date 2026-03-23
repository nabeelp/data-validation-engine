using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace DataValidationEngine.Core.Services;

public class AzureOpenAiValidationService : IValidationAiService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<AzureOpenAiValidationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AzureOpenAiValidationService(IConfiguration config, ILogger<AzureOpenAiValidationService> logger)
    {
        _logger = logger;

        var endpoint = config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        var deploymentName = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

        // If config contains a TenantID, use that with the DefaultAzureCredential. This allows for more flexible authentication scenarios (e.g., using a specific service principal).
        // If no TenantID is provided, DefaultAzureCredential will fall back to other authentication methods, which may include Managed Identity if running in Azure.
        var tenantId = config["AzureOpenAI:TenantId"];
        var credential = string.IsNullOrEmpty(tenantId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId });

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    public async Task<AiValidationResponse> ValidateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling Azure OpenAI — SystemPrompt={SystemLen}chars, UserPrompt={UserLen}chars",
            systemPrompt.Length, userPrompt.Length);

        var completion = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            ],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() },
            cancellationToken);

        var responseText = completion.Value.Content[0].Text;

        _logger.LogDebug("Azure OpenAI raw response: {Response}", responseText);

        var parsed = JsonSerializer.Deserialize<AiValidationResponse>(responseText, JsonOptions);

        if (parsed is null)
            throw new JsonException("Azure OpenAI returned a null or unparseable response.");

        _logger.LogInformation("Azure OpenAI returned {ResultCount} rule results", parsed.Results.Count);

        return parsed;
    }
}
