# Task Instructions

## Task
Implement the core validation engine: file parsing (CSV/XLSX), scope-ordered AI validation with short-circuit, RECORD batching, token estimation, and the upload/validate endpoint.

## Scope
- `Core/Services/FileParser.cs` + `Core/Interfaces/IFileParser.cs` — Parse CSV (CsvHelper) and XLSX (ClosedXML) into structured data (headers, data rows, footer rows)
- `Core/Services/ValidationEngine.cs` + `Core/Interfaces/IValidationEngine.cs` — Orchestrate validation: load rules, group by scope, call AI per scope in order (FILE→HEADER→FOOTER→RECORD), short-circuit on first scope failure, batch RECORD rows
- `Core/Services/PromptBuilder.cs` + `Core/Interfaces/IPromptBuilder.cs` — Build system and user prompts per spec
- `Core/Services/TokenEstimator.cs` — Estimate token count (~4 chars/token), determine batch sizes
- `Core/Models/ValidationResponse.cs` — Response DTO (status, rulesEvaluated, rulesFailed, scopeShortCircuitedAt, failures)
- `Core/Models/ParsedFile.cs` — Parsed file data (headers, dataRows, footerRows, fileType, rowCount)
- `Api/Endpoints/UploadEndpoint.cs` — POST `/api/cds/upload/validate` endpoint (multipart upload, size check, type check)
- Add NuGet packages: `CsvHelper`, `ClosedXML` to Core project
- DI registration in Program.cs for new services

## Out of Scope
- Frontend
- Unit tests (next task)
- Live Azure OpenAI integration (IValidationAiService stays abstract)

## Acceptance Criteria
- [ ] File parser handles CSV (CsvHelper) and XLSX (ClosedXML) with configurable header/footer row counts
- [ ] File parser rejects unsupported types and files >10MB
- [ ] Validation engine processes scopes in order: FILE → HEADER → FOOTER → RECORD
- [ ] Short-circuit: first scope with any failure stops processing remaining scopes
- [ ] RECORD batching: splits data rows when estimated tokens exceed budget (with 20% safety margin)
- [ ] FILE scope exceeding token budget returns ERROR (no batching fallback)
- [ ] All RECORD batches always processed (no inter-batch short-circuit); failures aggregated
- [ ] Prompt construction matches spec exactly (system prompt, user prompt template)
- [ ] Upload endpoint: 400 for file >10MB, unsupported type, missing file; 200 with status for PASS/FAIL/ERROR
- [ ] Audit log written for every validation run
- [ ] On PASS: IIngestionService called; on FAIL: INotificationService called; on ERROR: neither called
- [ ] No active rules for file type → PASS, proceed with ingestion, log warning
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes

## Validation
- [ ] `dotnet build src/backend/DataValidationEngine.slnx` exits 0
- [ ] `dotnet test src/backend/DataValidationEngine.Tests` exits 0
