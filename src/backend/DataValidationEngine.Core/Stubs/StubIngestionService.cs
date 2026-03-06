using DataValidationEngine.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Stubs;

public class StubIngestionService : IIngestionService
{
    private readonly ILogger<StubIngestionService> _logger;

    public StubIngestionService(ILogger<StubIngestionService> logger)
    {
        _logger = logger;
    }

    public Task IngestFileAsync(string fileName)
    {
        _logger.LogInformation("Stub: IngestFileAsync called — FileName={FileName}", fileName);

        return Task.CompletedTask;
    }
}
