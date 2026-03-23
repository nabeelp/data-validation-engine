using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IDatabaseInitializationService
{
    Task<DatabaseStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
}