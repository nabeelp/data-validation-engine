using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Stubs;

public class StubNotificationService : INotificationService
{
    private readonly ILogger<StubNotificationService> _logger;

    public StubNotificationService(ILogger<StubNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendValidationFailureEmailAsync(
        string recipientEmail,
        string fileName,
        DateTime uploadTimestamp,
        List<RuleFailureDetail> failures,
        string uploadPageUrl)
    {
        _logger.LogInformation(
            "Stub: SendValidationFailureEmailAsync called — Recipient={RecipientEmail}, File={FileName}, Timestamp={UploadTimestamp}, FailureCount={FailureCount}, Url={UploadPageUrl}",
            recipientEmail, fileName, uploadTimestamp, failures.Count, uploadPageUrl);

        return Task.CompletedTask;
    }
}
