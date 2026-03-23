using DataValidationEngine.Core.Interfaces;

namespace DataValidationEngine.Api.Endpoints;

public static class DatabaseSetupEndpoints
{
    public static void MapDatabaseSetupEndpoints(this WebApplication app)
    {
        app.MapGet("/api/database/status", async (
            HttpContext context,
            IEasyAuthService easyAuth,
            IDatabaseInitializationService databaseInitializationService) =>
        {
            var authResult = EndpointAuthorization.RequireRole(context, easyAuth, "Admin", "FinanceUser");
            if (authResult is not null)
                return authResult;

            var result = await databaseInitializationService.GetStatusAsync(context.RequestAborted);
            return Results.Ok(result);
        });

        app.MapPost("/api/admin/database/initialize", async (
            HttpContext context,
            IEasyAuthService easyAuth,
            IDatabaseInitializationService databaseInitializationService) =>
        {
            var authResult = EndpointAuthorization.RequireRole(context, easyAuth, "Admin", "FinanceUser");
            if (authResult is not null)
                return authResult;

            var result = await databaseInitializationService.InitializeAsync(context.RequestAborted);
            return Results.Ok(result);
        });
    }
}