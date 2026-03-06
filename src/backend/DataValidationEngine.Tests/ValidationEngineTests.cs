using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using DataValidationEngine.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataValidationEngine.Tests;

public class ValidationEngineTests
{
    private readonly IRuleRepository _ruleRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IValidationAiService _aiService;
    private readonly INotificationService _notificationService;
    private readonly IIngestionService _ingestionService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly Core.Services.ValidationEngine _engine;
    private readonly UserInfo _testUser;
    private readonly ParsedFile _testFile;

    public ValidationEngineTests()
    {
        _ruleRepo = Substitute.For<IRuleRepository>();
        _auditRepo = Substitute.For<IAuditLogRepository>();
        _aiService = Substitute.For<IValidationAiService>();
        _notificationService = Substitute.For<INotificationService>();
        _ingestionService = Substitute.For<IIngestionService>();
        _promptBuilder = new PromptBuilder();
        var logger = Substitute.For<ILogger<Core.Services.ValidationEngine>>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Validation:TimeoutSeconds"] = "30",
                ["AzureOpenAI:MaxTokensPerRequest"] = "128000",
                ["FileProcessing:BatchRowThreshold"] = "500"
            })
            .Build();

        _engine = new Core.Services.ValidationEngine(
            _ruleRepo, _auditRepo, _aiService, _notificationService,
            _ingestionService, _promptBuilder, logger, config);

        _testUser = new UserInfo { UserId = "user-1", Email = "test@example.com", Role = "FinanceUser" };
        _testFile = new ParsedFile
        {
            FileType = "CSV",
            HeaderRows = [["Date", "Amount"]],
            DataRows = [["2026-01-01", "100"], ["2026-01-02", "200"]],
            FooterRows = [["TOTAL", "300"]],
            TotalRowCount = 4,
            ColumnHeaders = ["Date", "Amount"]
        };
    }

    [Fact]
    public async Task ValidateAsync_NoActiveRules_ReturnsPASS_CallsIngestion()
    {
        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(Enumerable.Empty<ValidationRule>());

        var result = await _engine.ValidateAsync(_testFile, "test.csv", _testUser);

        Assert.Equal("PASS", result.Status);
        Assert.Equal(0, result.RulesEvaluated);
        await _ingestionService.Received(1).IngestFileAsync("test.csv");
        await _notificationService.DidNotReceiveWithAnyArgs()
            .SendValidationFailureEmailAsync(default!, default!, default, default!, default!);
    }

    [Fact]
    public async Task ValidateAsync_AllPass_ReturnsPASS_CallsIngestion()
    {
        var rules = CreateRules(("FILE", "Check file"));

        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(rules);
        _aiService.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiValidationResponse([new AiRuleResult(rules[0].Id, true, "All good")]));

        var result = await _engine.ValidateAsync(_testFile, "test.csv", _testUser);

        Assert.Equal("PASS", result.Status);
        await _ingestionService.Received(1).IngestFileAsync("test.csv");
    }

    [Fact]
    public async Task ValidateAsync_Failure_ReturnsFAIL_CallsNotification()
    {
        var rules = CreateRules(("FILE", "Must have data"));

        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(rules);
        _aiService.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiValidationResponse([new AiRuleResult(rules[0].Id, false, "No data found")]));

        var result = await _engine.ValidateAsync(_testFile, "test.csv", _testUser);

        Assert.Equal("FAIL", result.Status);
        Assert.Single(result.Failures);
        Assert.Equal("No data found", result.Failures[0].Reason);
        await _notificationService.Received(1)
            .SendValidationFailureEmailAsync("test@example.com", "test.csv", Arg.Any<DateTime>(), Arg.Any<List<RuleFailureDetail>>(), Arg.Any<string>());
        await _ingestionService.DidNotReceiveWithAnyArgs().IngestFileAsync(default!);
    }

    [Fact]
    public async Task ValidateAsync_ShortCircuits_OnFirstScopeFailure()
    {
        var fileRule = new ValidationRule { Id = Guid.NewGuid(), Name = "File check", Scope = "FILE", RuleText = "Must exist", FileType = "CSV", IsActive = true };
        var recordRule = new ValidationRule { Id = Guid.NewGuid(), Name = "Record check", Scope = "RECORD", RuleText = "Positive amounts", FileType = "CSV", IsActive = true };

        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(new List<ValidationRule> { fileRule, recordRule });
        _aiService.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiValidationResponse([new AiRuleResult(fileRule.Id, false, "File invalid")]));

        var result = await _engine.ValidateAsync(_testFile, "test.csv", _testUser);

        Assert.Equal("FAIL", result.Status);
        Assert.Equal("FILE", result.ScopeShortCircuitedAt);
        Assert.Equal(1, result.RulesEvaluated); // Only FILE rules evaluated
        // AI should only be called once (FILE scope), not for RECORD
        await _aiService.Received(1).ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_ProcessesScopesInOrder()
    {
        var headerRule = new ValidationRule { Id = Guid.NewGuid(), Name = "Header check", Scope = "HEADER", RuleText = "Valid headers", FileType = "CSV", IsActive = true };
        var recordRule = new ValidationRule { Id = Guid.NewGuid(), Name = "Record check", Scope = "RECORD", RuleText = "Positive amounts", FileType = "CSV", IsActive = true };

        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(new List<ValidationRule> { recordRule, headerRule });

        var callOrder = new List<string>();
        _aiService.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var userPrompt = callInfo.ArgAt<string>(1);
                if (userPrompt.Contains("scope: HEADER")) callOrder.Add("HEADER");
                else if (userPrompt.Contains("scope: RECORD")) callOrder.Add("RECORD");
                var ruleId = userPrompt.Contains("Header check") ? headerRule.Id : recordRule.Id;
                return new AiValidationResponse([new AiRuleResult(ruleId, true, "OK")]);
            });

        await _engine.ValidateAsync(_testFile, "test.csv", _testUser);

        Assert.Equal(["HEADER", "RECORD"], callOrder);
    }

    [Fact]
    public async Task ValidateAsync_RecordBatching_ProcessesAllBatches()
    {
        var dataRows = Enumerable.Range(1, 10).Select(i => new[] { $"2026-01-{i:D2}", $"{i * 100}" }).ToList();
        var file = new ParsedFile
        {
            FileType = "CSV",
            HeaderRows = [["Date", "Amount"]],
            DataRows = dataRows,
            FooterRows = [["TOTAL", "5500"]],
            TotalRowCount = 12,
            ColumnHeaders = ["Date", "Amount"]
        };

        var rule = new ValidationRule { Id = Guid.NewGuid(), Name = "Amount check", Scope = "RECORD", RuleText = "Positive amounts", FileType = "CSV", IsActive = true };
        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(new List<ValidationRule> { rule });

        var aiCallCount = 0;
        _aiService.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                aiCallCount++;
                return new AiValidationResponse([new AiRuleResult(rule.Id, true, "OK")]);
            });

        var result = await _engine.ValidateAsync(file, "test.csv", _testUser);

        Assert.Equal("PASS", result.Status);
        Assert.True(aiCallCount >= 1); // At least one batch processed
    }

    [Fact]
    public async Task ValidateAsync_RecordBatching_AggregatesFailures()
    {
        var dataRows = Enumerable.Range(1, 10).Select(i => new[] { $"2026-01-{i:D2}", i == 3 ? "-100" : $"{i * 100}" }).ToList();
        var file = new ParsedFile
        {
            FileType = "CSV",
            HeaderRows = [["Date", "Amount"]],
            DataRows = dataRows,
            FooterRows = [["TOTAL", "5500"]],
            TotalRowCount = 12,
            ColumnHeaders = ["Date", "Amount"]
        };

        var rule = new ValidationRule { Id = Guid.NewGuid(), Name = "Amount check", Scope = "RECORD", RuleText = "No negatives", FileType = "CSV", IsActive = true };
        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(new List<ValidationRule> { rule });

        _aiService.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiValidationResponse([new AiRuleResult(rule.Id, false, "Negative value found")]));

        var result = await _engine.ValidateAsync(file, "test.csv", _testUser);

        Assert.Equal("FAIL", result.Status);
        Assert.True(result.Failures.Count >= 1);
    }

    [Fact]
    public async Task ValidateAsync_WritesAuditLog_OnEveryRun()
    {
        _ruleRepo.GetActiveByFileTypeAsync("CSV").Returns(Enumerable.Empty<ValidationRule>());

        await _engine.ValidateAsync(_testFile, "test.csv", _testUser);

        await _auditRepo.Received(1).InsertAsync(Arg.Is<ValidationAuditLog>(l =>
            l.FileName == "test.csv" &&
            l.UserId == "user-1" &&
            l.OverallResult == "PASS"));
    }

    private static List<ValidationRule> CreateRules(params (string Scope, string RuleText)[] rules)
    {
        return rules.Select(r => new ValidationRule
        {
            Id = Guid.NewGuid(),
            Name = r.RuleText,
            RuleText = r.RuleText,
            Scope = r.Scope,
            FileType = "CSV",
            IsActive = true
        }).ToList();
    }
}
