using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class ValidationEngine : IValidationEngine
{
    private readonly IRuleRepository _ruleRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IValidationAiService _aiService;
    private readonly INotificationService _notificationService;
    private readonly IIngestionService _ingestionService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILogger<ValidationEngine> _logger;
    private readonly IConfiguration _config;

    private static readonly string[] ScopeOrder = ["FILE", "HEADER", "FOOTER", "RECORD"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValidationEngine(
        IRuleRepository ruleRepo,
        IAuditLogRepository auditRepo,
        IValidationAiService aiService,
        INotificationService notificationService,
        IIngestionService ingestionService,
        IPromptBuilder promptBuilder,
        ILogger<ValidationEngine> logger,
        IConfiguration config)
    {
        _ruleRepo = ruleRepo;
        _auditRepo = auditRepo;
        _aiService = aiService;
        _notificationService = notificationService;
        _ingestionService = ingestionService;
        _promptBuilder = promptBuilder;
        _logger = logger;
        _config = config;
    }

    public async Task<ValidationResponse> ValidateAsync(
        ParsedFile parsedFile,
        string fileName,
        UserInfo user,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Validation started — FileName={FileName}, FileType={FileType}", fileName, parsedFile.FileType);

        var allRules = (await _ruleRepo.GetActiveByFileTypeAsync(parsedFile.FileType)).ToList();

        if (allRules.Count == 0)
        {
            _logger.LogWarning("No active rules for FileType={FileType}. Passing file.", parsedFile.FileType);
            await _ingestionService.IngestFileAsync(fileName);
            await WriteAuditLogAsync(fileName, user, "PASS", allRules, [], []);
            return new ValidationResponse { Status = "PASS", RulesEvaluated = 0 };
        }

        var rulesGrouped = allRules
            .GroupBy(r => r.Scope.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Name).ToList());

        var allFailures = new List<RuleFailureDetail>();
        var allAiResponses = new List<object>();
        var scopesEvaluated = new List<string>();
        int totalRulesEvaluated = 0;
        string? shortCircuitedAt = null;

        var timeoutSeconds = _config.GetValue("Validation:TimeoutSeconds", 30);
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            foreach (var scope in ScopeOrder)
            {
                if (!rulesGrouped.TryGetValue(scope, out var scopeRules) || scopeRules.Count == 0)
                    continue;

                _logger.LogInformation("Processing scope={Scope}, RuleCount={RuleCount}", scope, scopeRules.Count);
                scopesEvaluated.Add(scope);
                totalRulesEvaluated += scopeRules.Count;

                var scopeFailures = scope == "RECORD"
                    ? await ProcessRecordScopeAsync(parsedFile, scopeRules, allAiResponses, linkedCts.Token)
                    : await ProcessSingleScopeAsync(parsedFile, scope, scopeRules, allAiResponses, linkedCts.Token);

                allFailures.AddRange(scopeFailures);

                if (scopeFailures.Count > 0)
                {
                    _logger.LogInformation("Scope={Scope} failed with {FailureCount} failure(s). Short-circuiting.", scope, scopeFailures.Count);
                    shortCircuitedAt = scope;
                    break;
                }

                _logger.LogInformation("Scope={Scope} passed", scope);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError("Validation timed out after {TimeoutSeconds}s — FileName={FileName}", timeoutSeconds, fileName);
            await WriteAuditLogAsync(fileName, user, "ERROR", allRules, allAiResponses, scopesEvaluated);
            return new ValidationResponse
            {
                Status = "ERROR",
                RulesEvaluated = totalRulesEvaluated,
                Failures = [new RuleFailureDetail(Guid.Empty, "Timeout", "SYSTEM", "Validation service timeout. Please retry.")]
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed AI response — FileName={FileName}", fileName);
            await WriteAuditLogAsync(fileName, user, "ERROR", allRules, allAiResponses, scopesEvaluated);
            return new ValidationResponse
            {
                Status = "ERROR",
                RulesEvaluated = totalRulesEvaluated,
                Failures = [new RuleFailureDetail(Guid.Empty, "ParseError", "SYSTEM", "Validation service returned an unexpected response.")]
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Validation error — FileName={FileName}", fileName);
            await WriteAuditLogAsync(fileName, user, "ERROR", allRules, allAiResponses, scopesEvaluated);
            return new ValidationResponse
            {
                Status = "ERROR",
                RulesEvaluated = totalRulesEvaluated,
                Failures = [new RuleFailureDetail(Guid.Empty, "Error", "SYSTEM", ex.Message)]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation — FileName={FileName}", fileName);
            await WriteAuditLogAsync(fileName, user, "ERROR", allRules, allAiResponses, scopesEvaluated);
            return new ValidationResponse
            {
                Status = "ERROR",
                RulesEvaluated = totalRulesEvaluated,
                Failures = [new RuleFailureDetail(Guid.Empty, "Error", "SYSTEM", "Validation service is temporarily unavailable. Please retry.")]
            };
        }

        sw.Stop();
        var status = allFailures.Count > 0 ? "FAIL" : "PASS";
        _logger.LogInformation("Validation complete — FileName={FileName}, Status={Status}, Duration={Duration}ms", fileName, status, sw.ElapsedMilliseconds);

        await WriteAuditLogAsync(fileName, user, status, allRules, allAiResponses, scopesEvaluated);

        if (status == "PASS")
        {
            await _ingestionService.IngestFileAsync(fileName);
        }
        else
        {
            await _notificationService.SendValidationFailureEmailAsync(
                user.Email, fileName, DateTime.UtcNow, allFailures, "/upload");
        }

        return new ValidationResponse
        {
            Status = status,
            RulesEvaluated = totalRulesEvaluated,
            RulesFailed = allFailures.Count,
            ScopeShortCircuitedAt = shortCircuitedAt,
            Failures = allFailures
        };
    }

    private async Task<List<RuleFailureDetail>> ProcessSingleScopeAsync(
        ParsedFile parsedFile,
        string scope,
        List<ValidationRule> rules,
        List<object> aiResponses,
        CancellationToken ct)
    {
        var content = GetContentForScope(parsedFile, scope);

        var maxTokens = _config.GetValue("AzureOpenAI:MaxTokensPerRequest", 128000);
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildUserPrompt(
            parsedFile.FileType, parsedFile.TotalRowCount, parsedFile.ColumnHeaders,
            parsedFile.FooterRows.Select(r => string.Join(",", r)).ToArray(),
            scope, content, rules);

        if (scope == "FILE")
        {
            var totalTokens = TokenEstimator.EstimateTokens(systemPrompt) + TokenEstimator.EstimateTokens(userPrompt);
            var budget = TokenEstimator.GetEffectiveBudget(maxTokens);
            if (totalTokens > budget)
            {
                throw new InvalidOperationException(
                    "File is too large for full-file validation. Reduce the file size or split it into smaller uploads.");
            }
        }

        var scopeSw = Stopwatch.StartNew();
        var response = await _aiService.ValidateAsync(systemPrompt, userPrompt, ct);
        scopeSw.Stop();
        _logger.LogInformation("AI call for scope={Scope} completed in {Duration}ms", scope, scopeSw.ElapsedMilliseconds);

        aiResponses.Add(new { scope, batchIndex = 0, response });

        return ExtractFailures(response, rules, scope);
    }

    private async Task<List<RuleFailureDetail>> ProcessRecordScopeAsync(
        ParsedFile parsedFile,
        List<ValidationRule> rules,
        List<object> aiResponses,
        CancellationToken ct)
    {
        var allFailures = new List<RuleFailureDetail>();
        var batchThreshold = _config.GetValue("FileProcessing:BatchRowThreshold", 500);
        var maxTokens = _config.GetValue("AzureOpenAI:MaxTokensPerRequest", 128000);
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var ruleText = string.Join("\n", rules.Select(r => r.RuleText));

        var avgCharsPerRow = parsedFile.DataRows.Count > 0
            ? (int)parsedFile.DataRows.Average(r => string.Join(",", r).Length)
            : 100;

        var batchSize = TokenEstimator.CalculateBatchSize(
            parsedFile.DataRows.Count, batchThreshold, maxTokens, systemPrompt, ruleText, avgCharsPerRow);

        var totalBatches = (int)Math.Ceiling((double)parsedFile.DataRows.Count / batchSize);
        _logger.LogInformation("RECORD scope — TotalRows={TotalRows}, BatchSize={BatchSize}, TotalBatches={TotalBatches}",
            parsedFile.DataRows.Count, batchSize, totalBatches);

        for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batchRows = parsedFile.DataRows
                .Skip(batchIndex * batchSize)
                .Take(batchSize)
                .ToList();

            var content = FormatRows(batchRows);
            var userPrompt = _promptBuilder.BuildUserPrompt(
                parsedFile.FileType, parsedFile.TotalRowCount, parsedFile.ColumnHeaders,
                parsedFile.FooterRows.Select(r => string.Join(",", r)).ToArray(),
                "RECORD", content, rules);

            _logger.LogInformation("Processing RECORD batch {BatchIndex}/{TotalBatches}, Rows={RowCount}",
                batchIndex + 1, totalBatches, batchRows.Count);

            var batchSw = Stopwatch.StartNew();
            var response = await _aiService.ValidateAsync(systemPrompt, userPrompt, ct);
            batchSw.Stop();
            _logger.LogInformation("AI call for RECORD batch {BatchIndex} completed in {Duration}ms",
                batchIndex + 1, batchSw.ElapsedMilliseconds);

            aiResponses.Add(new { scope = "RECORD", batchIndex, response });

            var batchFailures = ExtractFailures(response, rules, "RECORD");
            allFailures.AddRange(batchFailures);
        }

        return allFailures;
    }

    private string GetContentForScope(ParsedFile parsedFile, string scope)
    {
        return scope switch
        {
            "FILE" => FormatRows([.. parsedFile.HeaderRows, .. parsedFile.DataRows, .. parsedFile.FooterRows]),
            "HEADER" => FormatRows(parsedFile.HeaderRows),
            "FOOTER" => FormatRows(parsedFile.FooterRows),
            "RECORD" => FormatRows(parsedFile.DataRows),
            _ => string.Empty
        };
    }

    private static string FormatRows(List<string[]> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row));
        return sb.ToString();
    }

    private static List<RuleFailureDetail> ExtractFailures(
        AiValidationResponse response,
        List<ValidationRule> rules,
        string scope)
    {
        var ruleMap = rules.ToDictionary(r => r.Id);
        var failures = new List<RuleFailureDetail>();

        foreach (var result in response.Results)
        {
            if (!result.Passed && ruleMap.TryGetValue(result.RuleId, out var rule))
            {
                failures.Add(new RuleFailureDetail(rule.Id, rule.Name, scope, result.Reason));
            }
        }

        return failures;
    }

    private async Task WriteAuditLogAsync(
        string fileName,
        UserInfo user,
        string overallResult,
        List<ValidationRule> allRules,
        List<object> aiResponses,
        List<string> scopesEvaluated)
    {
        var auditLog = new ValidationAuditLog
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            UserId = user.UserId,
            UserEmail = user.Email,
            ValidatedAt = DateTime.UtcNow,
            OverallResult = overallResult,
            RulesEvaluated = JsonSerializer.Serialize(allRules.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                rule_text = r.RuleText,
                scope = r.Scope,
                file_type = r.FileType
            }), JsonOptions),
            AiResponse = JsonSerializer.Serialize(aiResponses, JsonOptions),
            ScopesEvaluated = string.Join(",", scopesEvaluated)
        };

        await _auditRepo.InsertAsync(auditLog);
    }
}
