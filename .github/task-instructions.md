# Task Instructions

## Task
Create SQL migration scripts for both database tables (`validation_rules` and `validation_audit_log`) and implement the Dapper-based data access layer with full CRUD for validation rules and audit log insertion.

## Scope
- `db/001_create_validation_rules.sql` — CREATE TABLE exactly as specified in data-model-api.md
- `db/002_create_validation_audit_log.sql` — CREATE TABLE exactly as specified in data-model-api.md
- `Core/Models/ValidationRule.cs` — Domain model for `validation_rules` table
- `Core/Models/ValidationAuditLog.cs` — Domain model for `validation_audit_log` table
- `Core/Models/RuleCreateRequest.cs` — Request DTO for rule create/update
- `Core/Interfaces/IRuleRepository.cs` — Data access interface for rules (GetAll, GetById, Create, Update, Delete, GetActiveByFileType)
- `Core/Interfaces/IAuditLogRepository.cs` — Data access interface for audit log (Insert)
- `Core/Services/RuleRepository.cs` — Dapper implementation of IRuleRepository
- `Core/Services/AuditLogRepository.cs` — Dapper implementation of IAuditLogRepository
- Add `Dapper` NuGet package to Core project
- Add `Microsoft.Data.SqlClient` NuGet package to Core project
- Unit tests for repository logic using an in-memory approach or verifying SQL correctness

## Out of Scope
- API endpoint wiring (next task)
- Validation engine logic
- Frontend
- Running migrations against a real database

## Acceptance Criteria
- [ ] `db/001_create_validation_rules.sql` creates table with all columns, types, constraints, and defaults from data-model-api.md
- [ ] `db/002_create_validation_audit_log.sql` creates table with all columns, types, constraints, and defaults from data-model-api.md
- [ ] `ValidationRule` model maps all columns from `validation_rules` table
- [ ] `ValidationAuditLog` model maps all columns from `validation_audit_log` table
- [ ] `RuleCreateRequest` DTO includes: name, description, rule_text, scope, file_type, is_active
- [ ] `IRuleRepository` has methods: GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, GetActiveByFileTypeAsync
- [ ] `IAuditLogRepository` has method: InsertAsync
- [ ] Dapper implementations use parameterized queries (no string concatenation)
- [ ] `updated_at` is set by application code on every write (as specified in data-model-api.md)
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes

## Validation
- [ ] `dotnet build src/backend/DataValidationEngine.slnx` exits 0
- [ ] `dotnet test src/backend/DataValidationEngine.Tests` exits 0
