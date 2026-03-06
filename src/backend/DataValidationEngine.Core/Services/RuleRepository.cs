using System.Data;
using Dapper;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class RuleRepository : IRuleRepository
{
    private readonly IDbConnection _db;
    private readonly ILogger<RuleRepository> _logger;

    public RuleRepository(IDbConnection db, ILogger<RuleRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ValidationRule>> GetAllAsync(bool? isActive = null)
    {
        _logger.LogInformation("Fetching all rules with IsActive={IsActive}", isActive);

        if (isActive.HasValue)
        {
            const string sql = """
                SELECT id, name, description, rule_text AS RuleText, scope, file_type AS FileType,
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM validation_rules
                WHERE is_active = @IsActive
                ORDER BY name
                """;
            return await _db.QueryAsync<ValidationRule>(sql, new { IsActive = isActive.Value });
        }
        else
        {
            const string sql = """
                SELECT id, name, description, rule_text AS RuleText, scope, file_type AS FileType,
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM validation_rules
                ORDER BY name
                """;
            return await _db.QueryAsync<ValidationRule>(sql);
        }
    }

    public async Task<ValidationRule?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching rule by Id={RuleId}", id);

        const string sql = """
            SELECT id, name, description, rule_text AS RuleText, scope, file_type AS FileType,
                   is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM validation_rules
            WHERE id = @Id
            """;
        return await _db.QuerySingleOrDefaultAsync<ValidationRule>(sql, new { Id = id });
    }

    public async Task<ValidationRule> CreateAsync(RuleCreateRequest request)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _logger.LogInformation("Creating rule Id={RuleId}, Name={RuleName}", id, request.Name);

        const string sql = """
            INSERT INTO validation_rules (id, name, description, rule_text, scope, file_type, is_active, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @RuleText, @Scope, @FileType, @IsActive, @CreatedAt, @UpdatedAt)
            """;

        await _db.ExecuteAsync(sql, new
        {
            Id = id,
            request.Name,
            request.Description,
            request.RuleText,
            request.Scope,
            request.FileType,
            request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        });

        return new ValidationRule
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            RuleText = request.RuleText,
            Scope = request.Scope,
            FileType = request.FileType,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task<ValidationRule?> UpdateAsync(Guid id, RuleCreateRequest request)
    {
        var now = DateTime.UtcNow;

        _logger.LogInformation("Updating rule Id={RuleId}", id);

        const string sql = """
            UPDATE validation_rules
            SET name = @Name,
                description = @Description,
                rule_text = @RuleText,
                scope = @Scope,
                file_type = @FileType,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id
            """;

        var rowsAffected = await _db.ExecuteAsync(sql, new
        {
            Id = id,
            request.Name,
            request.Description,
            request.RuleText,
            request.Scope,
            request.FileType,
            request.IsActive,
            UpdatedAt = now
        });

        if (rowsAffected == 0)
            return null;

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting rule Id={RuleId}", id);

        const string sql = "DELETE FROM validation_rules WHERE id = @Id";
        var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<ValidationRule>> GetActiveByFileTypeAsync(string fileType)
    {
        _logger.LogInformation("Fetching active rules for FileType={FileType}", fileType);

        const string sql = """
            SELECT id, name, description, rule_text AS RuleText, scope, file_type AS FileType,
                   is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM validation_rules
            WHERE is_active = 1 AND (file_type = @FileType OR file_type = 'ALL')
            ORDER BY name
            """;
        return await _db.QueryAsync<ValidationRule>(sql, new { FileType = fileType });
    }
}
