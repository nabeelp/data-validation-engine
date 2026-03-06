using DataValidationEngine.Core.Interfaces;

namespace DataValidationEngine.Api.Endpoints;

public static class MeEndpoint
{
    public static void MapMeEndpoint(this WebApplication app)
    {
        app.MapGet("/api/me", (HttpContext context, IEasyAuthService easyAuth) =>
        {
            var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
            var user = easyAuth.ParsePrincipal(header);

            if (user is null)
                return Results.Unauthorized();

            return Results.Ok(user);
        });
    }
}
