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
builder.Services.AddSingleton<IEasyAuthService, EasyAuthService>();
builder.Services.AddSingleton<INotificationService, StubNotificationService>();
builder.Services.AddSingleton<IIngestionService, StubIngestionService>();

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

app.UseCors();

app.MapRuleEndpoints();
app.MapMeEndpoint();

app.Run();
