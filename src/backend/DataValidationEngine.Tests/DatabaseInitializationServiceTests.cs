using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using DataValidationEngine.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataValidationEngine.Tests;

public class DatabaseInitializationServiceTests
{
    private readonly IDatabaseSchemaInitializer _schemaInitializer;
    private readonly IRuleRepository _ruleRepository;
    private readonly DatabaseInitializationService _service;

    public DatabaseInitializationServiceTests()
    {
        _schemaInitializer = Substitute.For<IDatabaseSchemaInitializer>();
        _ruleRepository = Substitute.For<IRuleRepository>();
        var logger = Substitute.For<ILogger<DatabaseInitializationService>>();

        _schemaInitializer.EnsureCreatedAsync(Arg.Any<CancellationToken>())
            .Returns(new DatabaseSchemaInitializationResult
            {
                DatabaseCreated = true,
                TablesCreated = 2
            });

        _service = new DatabaseInitializationService(_schemaInitializer, _ruleRepository, logger);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsSchemaInitializerStatus()
    {
        _schemaInitializer.DatabaseExistsAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _service.GetStatusAsync();

        Assert.True(result.Exists);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoRulesExist_InsertsAllSampleRules()
    {
        _ruleRepository.GetAllAsync(Arg.Any<bool?>())
            .Returns(Enumerable.Empty<ValidationRule>());

        var result = await _service.InitializeAsync();

        Assert.True(result.DatabaseCreated);
        Assert.Equal(2, result.TablesCreated);
        Assert.Equal(5, result.SampleRulesInserted);
        Assert.Equal(0, result.SampleRulesSkipped);
        Assert.Equal(5, result.SampleRulesTotal);
        await _ruleRepository.Received(5).CreateAsync(Arg.Any<RuleCreateRequest>());
    }

    [Fact]
    public async Task InitializeAsync_WhenSampleRulesAlreadyExist_DoesNotInsertDuplicates()
    {
        _ruleRepository.GetAllAsync(Arg.Any<bool?>())
            .Returns(CreateExistingRules());

        var result = await _service.InitializeAsync();

        Assert.Equal(0, result.SampleRulesInserted);
        Assert.Equal(5, result.SampleRulesSkipped);
        await _ruleRepository.DidNotReceive().CreateAsync(Arg.Any<RuleCreateRequest>());
    }

    [Fact]
    public async Task InitializeAsync_WhenSomeSampleRulesExist_OnlyInsertsMissingRules()
    {
        _ruleRepository.GetAllAsync(Arg.Any<bool?>())
            .Returns(CreateExistingRules().Take(2));

        var result = await _service.InitializeAsync();

        Assert.Equal(3, result.SampleRulesInserted);
        Assert.Equal(2, result.SampleRulesSkipped);
        await _ruleRepository.Received(3).CreateAsync(Arg.Any<RuleCreateRequest>());
    }

    private static IReadOnlyList<ValidationRule> CreateExistingRules()
    {
        return
        [
            new() { Id = Guid.NewGuid(), Name = "Minimum data rows", Scope = "FILE", FileType = "ALL" },
            new() { Id = Guid.NewGuid(), Name = "Required columns", Scope = "HEADER", FileType = "CSV" },
            new() { Id = Guid.NewGuid(), Name = "Footer total check", Scope = "FOOTER", FileType = "CSV" },
            new() { Id = Guid.NewGuid(), Name = "No negative amounts", Scope = "RECORD", FileType = "ALL" },
            new() { Id = Guid.NewGuid(), Name = "Valid currency codes", Scope = "RECORD", FileType = "ALL" }
        ];
    }
}