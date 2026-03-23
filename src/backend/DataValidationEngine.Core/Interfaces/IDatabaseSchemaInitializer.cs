using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IDatabaseSchemaInitializer
{
    Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default);
    Task<DatabaseSchemaInitializationResult> EnsureCreatedAsync(CancellationToken cancellationToken = default);
}