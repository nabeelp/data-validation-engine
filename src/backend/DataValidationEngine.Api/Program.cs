var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "DataValidationEngine API");

app.Run();
