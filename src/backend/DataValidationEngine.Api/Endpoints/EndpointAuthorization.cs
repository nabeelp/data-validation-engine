using DataValidationEngine.Core.Interfaces;

namespace DataValidationEngine.Api.Endpoints;

internal static class EndpointAuthorization
{
    public static IResult? RequireRole(HttpContext context, IEasyAuthService easyAuth, params string[] allowedRoles)
    {
        var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
        var user = easyAuth.ParsePrincipal(header);

        if (user is null)
            return Results.Unauthorized();

        if (allowedRoles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
            return null;

        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
}