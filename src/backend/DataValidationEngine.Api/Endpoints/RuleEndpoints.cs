using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Api.Endpoints;

public static class RuleEndpoints
{
    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase)
        { "FILE", "HEADER", "FOOTER", "RECORD" };

    private static readonly HashSet<string> ValidFileTypes = new(StringComparer.OrdinalIgnoreCase)
        { "CSV", "XLSX", "ALL" };

    public static void MapRuleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/validation-rules");

        group.MapGet("/", async (HttpContext context, IEasyAuthService easyAuth, bool? is_active, IRuleRepository repo) =>
        {
            var authResult = EndpointAuthorization.RequireRole(context, easyAuth, "Admin");
            if (authResult is not null)
                return authResult;

            var rules = await repo.GetAllAsync(is_active);
            return Results.Ok(rules);
        });

        group.MapPost("/", async (HttpContext context, IEasyAuthService easyAuth, RuleCreateRequest request, IRuleRepository repo) =>
        {
            var authResult = EndpointAuthorization.RequireRole(context, easyAuth, "Admin");
            if (authResult is not null)
                return authResult;

            var error = ValidateRequest(request);
            if (error is not null)
                return error;

            var rule = await repo.CreateAsync(request);
            return Results.Created($"/api/validation-rules/{rule.Id}", rule);
        });

        group.MapPut("/{id:guid}", async (HttpContext context, IEasyAuthService easyAuth, Guid id, RuleCreateRequest request, IRuleRepository repo) =>
        {
            var authResult = EndpointAuthorization.RequireRole(context, easyAuth, "Admin");
            if (authResult is not null)
                return authResult;

            var error = ValidateRequest(request);
            if (error is not null)
                return error;

            var rule = await repo.UpdateAsync(id, request);
            if (rule is null)
                return Results.NotFound();

            return Results.Ok(rule);
        });

        group.MapDelete("/{id:guid}", async (HttpContext context, IEasyAuthService easyAuth, Guid id, IRuleRepository repo) =>
        {
            var authResult = EndpointAuthorization.RequireRole(context, easyAuth, "Admin");
            if (authResult is not null)
                return authResult;

            var deleted = await repo.DeleteAsync(id);
            if (!deleted)
                return Results.NotFound();

            return Results.NoContent();
        });
    }

    private static IResult? ValidateRequest(RuleCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("name is required.");

        if (string.IsNullOrWhiteSpace(request.RuleText))
            return Results.BadRequest("rule_text is required.");

        if (string.IsNullOrWhiteSpace(request.Scope))
            return Results.BadRequest("scope is required.");

        if (string.IsNullOrWhiteSpace(request.FileType))
            return Results.BadRequest("file_type is required.");

        if (!ValidScopes.Contains(request.Scope))
            return Results.BadRequest("scope must be one of: FILE, HEADER, FOOTER, RECORD.");

        if (!ValidFileTypes.Contains(request.FileType))
            return Results.BadRequest("file_type must be one of: CSV, XLSX, ALL.");

        return null;
    }
}
