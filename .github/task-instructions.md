# Task Instructions

## Task
Implement API endpoints for validation rules CRUD, `/api/me`, and DI/middleware wiring in Program.cs — everything except the file upload/validation endpoint.

## Scope
- `Api/Endpoints/RuleEndpoints.cs` — Map CRUD endpoints for `/api/validation-rules` with request validation
- `Api/Endpoints/MeEndpoint.cs` — Map `/api/me` returning user identity from EasyAuth claims
- `Api/Program.cs` — DI registration (IDbConnection, IRuleRepository, IAuditLogRepository, stub services), CORS, middleware
- `Core/Models/UserInfo.cs` — DTO for `/api/me` response (userId, email, role)
- `Core/Services/EasyAuthService.cs` — Parse `X-MS-CLIENT-PRINCIPAL` header into UserInfo
- `Core/Interfaces/IEasyAuthService.cs` — Interface for EasyAuth parsing

## Out of Scope
- `/api/cds/upload/validate` endpoint (depends on validation engine)
- Validation engine logic
- Frontend
- Auth middleware enforcement (just parsing; actual role checking is inline in endpoints)

## Acceptance Criteria
- [ ] GET `/api/validation-rules` returns all rules; supports `?is_active=true` filter
- [ ] POST `/api/validation-rules` creates rule, returns 201 + body; validates required fields, returns 400 on invalid input
- [ ] PUT `/api/validation-rules/{id}` updates rule, returns 200 + body; returns 404 if not found
- [ ] DELETE `/api/validation-rules/{id}` returns 204; returns 404 if not found
- [ ] GET `/api/me` returns `{ userId, email, role }` from EasyAuth claims
- [ ] Request body validation: name, rule_text, scope, file_type required; scope must be FILE/HEADER/FOOTER/RECORD; file_type must be CSV/XLSX/ALL
- [ ] DI wiring: IDbConnection (SqlConnection), IRuleRepository, IAuditLogRepository, INotificationService (stub), IIngestionService (stub), IEasyAuthService
- [ ] CORS configured from `Cors:AllowedOrigins` in appsettings.json
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes

## Validation
- [ ] `dotnet build src/backend/DataValidationEngine.slnx` exits 0
- [ ] `dotnet test src/backend/DataValidationEngine.Tests` exits 0
