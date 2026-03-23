using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class DatabaseInitializationService : IDatabaseInitializationService
{
    private static readonly IReadOnlyList<RuleCreateRequest> SampleRules =
    [
        new()
        {
            Name = "Minimum data rows",
            Description = "Sample FILE scope rule for smoke testing.",
            RuleText = "The file must contain at least one data row beyond the header",
            Scope = "FILE",
            FileType = "ALL",
            IsActive = true
        },
        new()
        {
            Name = "Required columns",
            Description = "Sample HEADER scope rule for smoke testing.",
            RuleText = "The first row must contain these exact column headers: Date, Account, Amount, Currency, Description",
            Scope = "HEADER",
            FileType = "CSV",
            IsActive = true
        },
        new()
        {
            Name = "Footer total check",
            Description = "Sample FOOTER scope rule for smoke testing.",
            RuleText = "The last row must contain a total that equals the sum of the Amount column",
            Scope = "FOOTER",
            FileType = "CSV",
            IsActive = true
        },
        new()
        {
            Name = "No negative amounts",
            Description = "Sample RECORD scope rule for smoke testing.",
            RuleText = "The Amount column must not contain negative values",
            Scope = "RECORD",
            FileType = "ALL",
            IsActive = true
        },
        new()
        {
            Name = "Valid currency codes",
            Description = "Sample RECORD scope rule for smoke testing.",
            RuleText = "The Currency column must contain only valid ISO 4217 codes (e.g., USD, EUR, GBP)",
            Scope = "RECORD",
            FileType = "ALL",
            IsActive = true
        }
    ];

    private readonly IDatabaseSchemaInitializer _schemaInitializer;
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        IDatabaseSchemaInitializer schemaInitializer,
        IRuleRepository ruleRepository,
        ILogger<DatabaseInitializationService> logger)
    {
        _schemaInitializer = schemaInitializer;
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<DatabaseStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _schemaInitializer.DatabaseExistsAsync(cancellationToken);
        return new DatabaseStatusResult
        {
            Exists = exists
        };
    }

    public async Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var schemaResult = await _schemaInitializer.EnsureCreatedAsync(cancellationToken);
        var existingRules = (await _ruleRepository.GetAllAsync()).ToList();
        var existingRuleKeys = existingRules
            .Select(CreateRuleKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var insertedCount = 0;
        var skippedCount = 0;

        foreach (var sampleRule in SampleRules)
        {
            if (existingRuleKeys.Contains(CreateRuleKey(sampleRule)))
            {
                skippedCount++;
                continue;
            }

            await _ruleRepository.CreateAsync(sampleRule);
            insertedCount++;
        }

        _logger.LogInformation(
            "Database initialization complete. DatabaseCreated={DatabaseCreated}, TablesCreated={TablesCreated}, SampleRulesInserted={SampleRulesInserted}, SampleRulesSkipped={SampleRulesSkipped}",
            schemaResult.DatabaseCreated,
            schemaResult.TablesCreated,
            insertedCount,
            skippedCount);

        return new DatabaseInitializationResult
        {
            DatabaseCreated = schemaResult.DatabaseCreated,
            TablesCreated = schemaResult.TablesCreated,
            SampleRulesInserted = insertedCount,
            SampleRulesSkipped = skippedCount,
            SampleRulesTotal = SampleRules.Count
        };
    }

    private static string CreateRuleKey(ValidationRule rule)
    {
        return $"{rule.Name}|{rule.Scope}|{rule.FileType}";
    }

    private static string CreateRuleKey(RuleCreateRequest rule)
    {
        return $"{rule.Name}|{rule.Scope}|{rule.FileType}";
    }
}