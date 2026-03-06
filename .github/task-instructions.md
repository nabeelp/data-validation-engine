# Task Instructions

## Task
Add comprehensive unit tests for the core backend services: validation engine, file parser, EasyAuth service, prompt builder, and token estimator.

## Scope
- `Tests/ValidationEngineTests.cs` — Test scope ordering, short-circuit, RECORD batching, PASS/FAIL/ERROR flows, no-rules case, notification/ingestion calls
- `Tests/FileParserTests.cs` — Test CSV and XLSX parsing, header/footer extraction, edge cases
- `Tests/EasyAuthServiceTests.cs` — Test header parsing, missing/invalid headers
- `Tests/PromptBuilderTests.cs` — Test system/user prompt construction
- `Tests/TokenEstimatorTests.cs` — Test token estimation and batch size calculation
- `Tests/Fixtures/` — Sample CSV/XLSX files and AI response JSON fixtures

## Out of Scope
- Integration tests against real database
- Frontend tests
- Live AI service tests

## Acceptance Criteria
- [ ] ValidationEngine tests: PASS flow calls IIngestionService, FAIL flow calls INotificationService, ERROR flow calls neither
- [ ] ValidationEngine tests: scopes processed in order FILE→HEADER→FOOTER→RECORD
- [ ] ValidationEngine tests: short-circuit on first scope with failures
- [ ] ValidationEngine tests: RECORD batching processes all batches, aggregates failures
- [ ] ValidationEngine tests: no active rules → PASS with ingestion call
- [ ] FileParser tests: CSV parsing with header/data/footer separation
- [ ] FileParser tests: XLSX parsing with header/data/footer separation
- [ ] FileParser tests: unsupported file type throws
- [ ] EasyAuth tests: valid header returns UserInfo, missing header returns null
- [ ] TokenEstimator tests: token estimation and batch size calculation
- [ ] All tests pass: `dotnet test` exits 0 with all green

## Validation
- [ ] `dotnet build src/backend/DataValidationEngine.slnx` exits 0
- [ ] `dotnet test src/backend/DataValidationEngine.Tests` exits 0 with all tests passing
