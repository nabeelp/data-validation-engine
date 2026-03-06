using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IAuditLogRepository
{
    Task InsertAsync(ValidationAuditLog auditLog);
}
