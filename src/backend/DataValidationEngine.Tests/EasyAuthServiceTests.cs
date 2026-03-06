using DataValidationEngine.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataValidationEngine.Tests;

public class EasyAuthServiceTests
{
    private readonly EasyAuthService _service;

    public EasyAuthServiceTests()
    {
        var logger = Substitute.For<ILogger<EasyAuthService>>();
        _service = new EasyAuthService(logger);
    }

    [Fact]
    public void ParsePrincipal_NullHeader_ReturnsNull()
    {
        var result = _service.ParsePrincipal(null);
        Assert.Null(result);
    }

    [Fact]
    public void ParsePrincipal_EmptyHeader_ReturnsNull()
    {
        var result = _service.ParsePrincipal("");
        Assert.Null(result);
    }

    [Fact]
    public void ParsePrincipal_InvalidBase64_ReturnsNull()
    {
        var result = _service.ParsePrincipal("not-valid-base64!!!");
        Assert.Null(result);
    }

    [Fact]
    public void ParsePrincipal_ValidHeader_ReturnsUserInfo()
    {
        var json = """
        {
            "claims": [
                { "typ": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "val": "user-123" },
                { "typ": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "val": "user@example.com" },
                { "typ": "roles", "val": "Admin" }
            ]
        }
        """;
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        var result = _service.ParsePrincipal(base64);

        Assert.NotNull(result);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public void ParsePrincipal_MissingClaims_ReturnsEmptyStrings()
    {
        var json = """{ "claims": [] }""";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        var result = _service.ParsePrincipal(base64);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.UserId);
        Assert.Equal(string.Empty, result.Email);
        Assert.Equal(string.Empty, result.Role);
    }
}
