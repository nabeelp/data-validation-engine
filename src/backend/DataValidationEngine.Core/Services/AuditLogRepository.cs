using System.Data;
using Dapper;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnection _db;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(IDbConnection db, ILogger<AuditLogRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InsertAsync(ValidationAuditLog auditLog)
    {
        _logger.LogInformation(
            "Inserting audit log — FileName={FileName}, UserId={UserId}, Result={OverallResult}",
            auditLog.FileName, auditLog.UserId, auditLog.OverallResult);

        const string sql = """
            INSERT INTO validation_audit_log
                (id, file_name, user_id, user_email, validated_at, overall_result, rules_evaluated, ai_response, scopes_evaluated)
            VALUES
                (@Id, @FileName, @UserId, @UserEmail, @ValidatedAt, @OverallResult, @RulesEvaluated, @AiResponse, @ScopesEvaluated)
            """;

        await _db.ExecuteAsync(sql, new
        {
            auditLog.Id,
            auditLog.FileName,
            auditLog.UserId,
            auditLog.UserEmail,
            auditLog.ValidatedAt,
            auditLog.OverallResult,
            auditLog.RulesEvaluated,
            auditLog.AiResponse,
            auditLog.ScopesEvaluated
        });
    }
}
