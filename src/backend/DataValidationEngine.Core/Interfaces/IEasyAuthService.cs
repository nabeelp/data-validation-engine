using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IEasyAuthService
{
    UserInfo? ParsePrincipal(string? headerValue);
}
