using Dapper;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class SqlServerDatabaseSchemaInitializer : IDatabaseSchemaInitializer
{
    private const string CreateValidationRulesTableSql = """
        CREATE TABLE validation_rules (
            id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            name            NVARCHAR(255)    NOT NULL,
            description     NVARCHAR(MAX)    NULL,
            rule_text       NVARCHAR(MAX)    NOT NULL,
            scope           NVARCHAR(20)     NOT NULL,
            file_type       NVARCHAR(10)     NOT NULL,
            is_active       BIT              NOT NULL DEFAULT 1,
            created_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
            updated_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT PK_validation_rules PRIMARY KEY (id),
            CONSTRAINT CK_validation_rules_scope CHECK (scope IN ('FILE', 'HEADER', 'FOOTER', 'RECORD')),
            CONSTRAINT CK_validation_rules_file_type CHECK (file_type IN ('CSV', 'XLSX', 'ALL'))
        );
        """;

    private const string CreateValidationAuditLogTableSql = """
        CREATE TABLE validation_audit_log (
            id                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            file_name         NVARCHAR(255)    NOT NULL,
            user_id           NVARCHAR(255)    NOT NULL,
            user_email        NVARCHAR(255)    NOT NULL,
            validated_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
            overall_result    NVARCHAR(10)     NOT NULL,
            rules_evaluated   NVARCHAR(MAX)    NULL,
            ai_response       NVARCHAR(MAX)    NULL,
            scopes_evaluated  NVARCHAR(100)    NULL,
            CONSTRAINT PK_validation_audit_log PRIMARY KEY (id),
            CONSTRAINT CK_validation_audit_log_overall_result CHECK (overall_result IN ('PASS', 'FAIL', 'ERROR'))
        );
        """;

    private readonly string _connectionString;
    private readonly ILogger<SqlServerDatabaseSchemaInitializer> _logger;

    public SqlServerDatabaseSchemaInitializer(
        IConfiguration configuration,
        ILogger<SqlServerDatabaseSchemaInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
    }

    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        var builder = CreateConnectionStringBuilder();
        var databaseName = builder.InitialCatalog;

        await using var masterConnection = await OpenMasterConnectionAsync(builder, cancellationToken);
        var exists = await masterConnection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys.databases WHERE name = @DatabaseName",
            new { DatabaseName = databaseName },
            cancellationToken: cancellationToken));

        return exists > 0;
    }

    public async Task<DatabaseSchemaInitializationResult> EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var builder = CreateConnectionStringBuilder();
        var databaseCreated = await EnsureDatabaseExistsAsync(builder, cancellationToken);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var tablesCreated = 0;
        if (await EnsureTableExistsAsync(connection, "validation_rules", CreateValidationRulesTableSql, cancellationToken))
            tablesCreated++;

        if (await EnsureTableExistsAsync(connection, "validation_audit_log", CreateValidationAuditLogTableSql, cancellationToken))
            tablesCreated++;

        return new DatabaseSchemaInitializationResult
        {
            DatabaseCreated = databaseCreated,
            TablesCreated = tablesCreated
        };
    }

    private async Task<bool> EnsureDatabaseExistsAsync(SqlConnectionStringBuilder builder, CancellationToken cancellationToken)
    {
        var databaseExists = await DatabaseExistsAsync(cancellationToken);
        if (databaseExists)
            return false;

        var databaseName = builder.InitialCatalog;
        await using var masterConnection = await OpenMasterConnectionAsync(builder, cancellationToken);

        var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        await masterConnection.ExecuteAsync(new CommandDefinition(
            $"CREATE DATABASE [{escapedDatabaseName}]",
            cancellationToken: cancellationToken));

        _logger.LogInformation("Created database {DatabaseName}", databaseName);
        return true;
    }

    private SqlConnectionStringBuilder CreateConnectionStringBuilder()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured before database initialization can run.");

        var builder = new SqlConnectionStringBuilder(_connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            throw new InvalidOperationException("The database name must be present in ConnectionStrings:DefaultConnection.");

        return builder;
    }

    private static async Task<SqlConnection> OpenMasterConnectionAsync(
        SqlConnectionStringBuilder builder,
        CancellationToken cancellationToken)
    {
        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<bool> EnsureTableExistsAsync(
        SqlConnection connection,
        string tableName,
        string createTableSql,
        CancellationToken cancellationToken)
    {
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END",
            new { TableName = tableName },
            cancellationToken: cancellationToken));

        if (exists == 1)
        {
            _logger.LogInformation("Table {TableName} already exists", tableName);
            return false;
        }

        await connection.ExecuteAsync(new CommandDefinition(createTableSql, cancellationToken: cancellationToken));
        _logger.LogInformation("Created table {TableName}", tableName);
        return true;
    }
}