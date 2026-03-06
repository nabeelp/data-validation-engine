using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IRuleRepository
{
    Task<IEnumerable<ValidationRule>> GetAllAsync(bool? isActive = null);
    Task<ValidationRule?> GetByIdAsync(Guid id);
    Task<ValidationRule> CreateAsync(RuleCreateRequest request);
    Task<ValidationRule?> UpdateAsync(Guid id, RuleCreateRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<ValidationRule>> GetActiveByFileTypeAsync(string fileType);
}
