using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IValidationEngine
{
    Task<ValidationResponse> ValidateAsync(
        ParsedFile parsedFile,
        string fileName,
        UserInfo user,
        CancellationToken cancellationToken = default);
}
