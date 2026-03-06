using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface INotificationService
{
    Task SendValidationFailureEmailAsync(
        string recipientEmail,
        string fileName,
        DateTime uploadTimestamp,
        List<RuleFailureDetail> failures,
        string uploadPageUrl);
}
