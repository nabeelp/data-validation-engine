using System.Data;
using DataValidationEngine.Api.Endpoints;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Services;
using DataValidationEngine.Core.Stubs;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddScoped<IDbConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Services
builder.Services.AddSingleton<IDatabaseSchemaInitializer, SqlServerDatabaseSchemaInitializer>();
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddSingleton<IEasyAuthService, EasyAuthService>();
builder.Services.AddSingleton<INotificationService, StubNotificationService>();
builder.Services.AddSingleton<IIngestionService, StubIngestionService>();

var openAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
if (!string.IsNullOrWhiteSpace(openAiEndpoint))
    builder.Services.AddSingleton<IValidationAiService, AzureOpenAiValidationService>();
else
    builder.Services.AddSingleton<IValidationAiService, StubValidationAiService>();
builder.Services.AddSingleton<IFileParser, FileParser>();
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
builder.Services.AddScoped<IValidationEngine, ValidationEngine>();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// In Development, inject a fake EasyAuth header so the app is testable without Azure
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (!context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL"))
        {
            var fakePrincipal = System.Text.Json.JsonSerializer.Serialize(new
            {
                claims = new[]
                {
                    new { typ = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", val = "dev-user" },
                    new { typ = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", val = "dev@localhost" },
                    new { typ = "roles", val = "Admin" }
                }
            });
            context.Request.Headers["X-MS-CLIENT-PRINCIPAL"] =
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fakePrincipal));
        }
        await next();
    });
}

app.UseCors();

app.MapRuleEndpoints();
app.MapDatabaseSetupEndpoints();
app.MapMeEndpoint();
app.MapUploadEndpoint();

app.Run();
