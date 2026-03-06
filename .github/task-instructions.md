# Task Instructions

## Task
Scaffold the backend solution: create the .NET 10 solution structure, projects, and all interface/model definitions exactly as specified in architecture.md.

## Scope
- `src/backend/DataValidationEngine.Api/` — Minimal API host: `Program.cs`, empty `Endpoints/` folder, `appsettings.json` with config from architecture.md
- `src/backend/DataValidationEngine.Core/` — Domain logic: `Models/`, `Interfaces/` (all interfaces from architecture.md), `Services/` (empty), `Stubs/` (stub implementations that log only)
- `src/backend/DataValidationEngine.Tests/` — xUnit project: `Fixtures/` folder
- `db/` — empty folder with a `README.md` noting it holds sequential SQL migration scripts (`001_`, `002_`, ...)
- Solution file (`.sln`) wiring all three projects

## Out of Scope
- Any actual service logic (validation engine, file parsing, AI integration)
- Database migrations
- Frontend
- Tests beyond project scaffold

## Acceptance Criteria
- [ ] `dotnet build` succeeds with no errors across all three projects
- [ ] All interfaces from architecture.md are defined: `IValidationAiService`, `INotificationService`, `IIngestionService`
- [ ] All records from architecture.md are defined: `AiValidationResponse`, `AiRuleResult`, `RuleFailureDetail`
- [ ] Stub implementations of `INotificationService` and `IIngestionService` exist in `Core/Stubs/` and log their inputs via `ILogger<T>`
- [ ] `appsettings.json` matches the schema in architecture.md exactly
- [ ] `.gitignore` excludes `appsettings.Development.json` and standard .NET build artefacts

## Validation
- [ ] `dotnet build src/backend/DataValidationEngine.sln` exits 0
- [ ] `dotnet test src/backend/DataValidationEngine.Tests` exits 0 (no tests yet, but project must load)
