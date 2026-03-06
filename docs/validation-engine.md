# Validation Engine

## Core Flow

1. `/api/cds/upload/validate` receives file upload
2. Reject if >10 MB or unsupported type — log to `validation_audit_log` with `overall_result = ERROR`, empty `rules_evaluated` and `ai_response`, then return `400`
3. Parse file (CsvHelper for CSV, ClosedXML for XLSX)
4. Load active rules filtered by `file_type` match (`ALL` matches any upload type)
5. Group rules by scope, process in order: `FILE` → `HEADER` → `FOOTER` → `RECORD`
6. Within each scope, order rules alphabetically by `name`
7. **One Azure OpenAI call per scope. Short-circuit on first scope with any failure.**
8. On `FAIL` (rule violation): return error payload + call `INotificationService`
9. On `ERROR` (system/infra failure): return error payload. Do **not** call `INotificationService` or `IIngestionService`.
10. On `PASS`: call `IIngestionService` (pass file name)
11. Log every run to `validation_audit_log` (including `ERROR` outcomes)

Concurrent validations allowed. No queuing/locks. Log each step: start, scope result, AI call duration, batch progress, overall result.

## File Parsing

- **CSV:** CsvHelper. UTF-8 assumed (BOM respected).
- **XLSX:** ClosedXML (`.xlsx` only — legacy `.xls` rejected pre-parse).
- **Headers:** First Y rows (config: `FileProcessing:HeaderRowCount`, default `1`).
- **Footers:** Last X rows (config: `FileProcessing:FooterRowCount`, default `1`).
- **Max size:** 10 MB (`FileProcessing:MaxFileSizeBytes`).

## Azure OpenAI Integration

- NuGet: `Azure.AI.OpenAI` + `Azure.Identity`
- Auth: `DefaultAzureCredential` (Managed Identity in prod, dev creds locally)
- Model: configurable via `AzureOpenAI:DeploymentName` (not hardcoded)
- Abstract behind `IValidationAiService` interface for testability

## Prompt Design

**System prompt:**
```
You are a data file validation assistant. Evaluate the provided file content against each rule
and return ONLY a valid JSON object matching this schema exactly:
{ "results": [ { "rule_id": string, "passed": boolean, "reason": string } ] }
Do not include any text outside the JSON object.
```

**User prompt:**
```
File type: {file_type}. Row count: {row_count}. Headers: {headers}. Footer rows: {footer_rows}.

File content (scope: {scope}):
{file_content_sample}

Rules to evaluate:
1. [rule_id: {uuid_1}] "{rule_text_1}"
2. [rule_id: {uuid_2}] "{rule_text_2}"
```

Rule UUIDs are included in the prompt so the model echoes them back in the response.

**Content per scope:**
- `FILE` — full file (headers + data + footers)
- `HEADER` — first Y rows only
- `FOOTER` — last X rows only
- `RECORD` — data rows only (excluding headers/footers), batched if needed

**Expected response:**
```json
{
  "results": [
    { "rule_id": "<uuid>", "passed": true, "reason": "<string>" }
  ]
}
```

## RECORD-Scope Batching

- All data rows must be validated, not just the first batch.
- Before each call, estimate token count (content + rules + system prompt).
- If estimated tokens exceed context window, split rows into batches.
- Default batch threshold: 500 rows (`FileProcessing:BatchRowThreshold`). Dynamically reduced if 500 rows exceed token budget.
- Each batch = separate Azure OpenAI call with same rules.
- All batches always processed (no inter-batch short-circuit). Failures aggregated.
- Any batch failure = overall RECORD scope failure.
- FILE/HEADER/FOOTER scopes are never batched.
- If FILE scope content exceeds the token budget (after the 20% safety margin), return `ERROR`: _"File is too large for full-file validation. Reduce the file size or split it into smaller uploads."_ Do not fall back to batching or truncation.

### Token Estimation

- Use a heuristic of **~4 characters per token** to estimate prompt size.
- Sum: system prompt tokens + user prompt template tokens + file content tokens + rule text tokens.
- Compare against `AzureOpenAI:MaxTokensPerRequest` (default: 128,000) with a 20% safety margin.
- If estimate exceeds budget, reduce batch row count proportionally.

## Email Notification

On validation failure, call `INotificationService.SendValidationFailureEmailAsync` with:
- Recipient email (from EasyAuth claims)
- Subject: `CDS Upload Validation Failed — <filename> <timestamp>`
- Body: filename, timestamp, failed rules (Name, Scope, Reason), upload page link

Not triggered on success. Stub implementation logs payload.
