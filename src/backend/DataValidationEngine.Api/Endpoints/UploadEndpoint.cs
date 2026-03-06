using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Api.Endpoints;

public static class UploadEndpoint
{
    public static void MapUploadEndpoint(this WebApplication app)
    {
        app.MapPost("/api/cds/upload/validate", async (
            HttpContext context,
            IEasyAuthService easyAuth,
            IFileParser fileParser,
            IValidationEngine validationEngine,
            IConfiguration config) =>
        {
            var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
            var user = easyAuth.ParsePrincipal(header);
            if (user is null)
                return Results.Unauthorized();

            if (!context.Request.HasFormContentType || context.Request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");

            var file = context.Request.Form.Files[0];
            var maxFileSize = config.GetValue("FileProcessing:MaxFileSizeBytes", 10485760);

            if (file.Length > maxFileSize)
                return Results.BadRequest("File exceeds the maximum allowed size of 10 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".csv" and not ".xlsx")
                return Results.BadRequest("Only CSV and XLSX files are supported.");

            var headerRowCount = config.GetValue("FileProcessing:HeaderRowCount", 1);
            var footerRowCount = config.GetValue("FileProcessing:FooterRowCount", 1);

            ParsedFile parsedFile;
            try
            {
                using var stream = file.OpenReadStream();
                parsedFile = fileParser.Parse(stream, file.FileName, headerRowCount, footerRowCount);
            }
            catch (Exception)
            {
                return Results.Ok(new ValidationResponse
                {
                    Status = "ERROR",
                    Failures = [new RuleFailureDetail(Guid.Empty, "ParseError", "SYSTEM",
                        "The uploaded file could not be parsed. Please verify the file format.")]
                });
            }

            var result = await validationEngine.ValidateAsync(parsedFile, file.FileName, user, context.RequestAborted);
            return Results.Ok(result);
        })
        .DisableAntiforgery();
    }
}
