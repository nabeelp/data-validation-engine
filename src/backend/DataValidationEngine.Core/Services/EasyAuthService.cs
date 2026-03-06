using System.Text;
using System.Text.Json;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class EasyAuthService : IEasyAuthService
{
    private readonly ILogger<EasyAuthService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EasyAuthService(ILogger<EasyAuthService> logger)
    {
        _logger = logger;
    }

    public UserInfo? ParsePrincipal(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            _logger.LogWarning("X-MS-CLIENT-PRINCIPAL header is missing or empty");
            return null;
        }

        try
        {
            var decoded = Convert.FromBase64String(headerValue);
            var json = Encoding.UTF8.GetString(decoded);
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOptions);

            if (principal?.Claims == null)
            {
                _logger.LogWarning("Decoded principal contains no claims");
                return null;
            }

            var userId = principal.Claims
                .FirstOrDefault(c => c.Typ == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                ?.Val ?? string.Empty;

            var email = principal.Claims
                .FirstOrDefault(c => c.Typ == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                ?.Val ?? string.Empty;

            var role = principal.Claims
                .FirstOrDefault(c => c.Typ == "roles")
                ?.Val ?? string.Empty;

            _logger.LogInformation("Parsed EasyAuth principal — UserId={UserId}, Role={Role}", userId, role);

            return new UserInfo
            {
                UserId = userId,
                Email = email,
                Role = role
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse X-MS-CLIENT-PRINCIPAL header");
            return null;
        }
    }

    private sealed class ClientPrincipal
    {
        public List<ClientPrincipalClaim>? Claims { get; set; }
    }

    private sealed class ClientPrincipalClaim
    {
        public string Typ { get; set; } = string.Empty;
        public string Val { get; set; } = string.Empty;
    }
}
