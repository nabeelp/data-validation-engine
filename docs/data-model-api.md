# Data Model & API

SQL Server. Dapper. Migrations in `db/`.

## Tables

### `validation_rules`

| Column | Type | Notes |
|---|---|---|
| `id` | UNIQUEIDENTIFIER | PK. Default `NEWID()`. |
| `name` | NVARCHAR(255) | Required. |
| `description` | NVARCHAR(MAX) | Optional. |
| `rule_text` | NVARCHAR(MAX) | Natural-language rule. Required. |
| `scope` | NVARCHAR(20) | CHECK: `FILE`, `HEADER`, `FOOTER`, `RECORD`. |
| `file_type` | NVARCHAR(10) | CHECK: `CSV`, `XLSX`, `ALL`. |
| `is_active` | BIT | Default `1`. |
| `created_at` | DATETIME2 | Default `SYSUTCDATETIME()`. |
| `updated_at` | DATETIME2 | Default `SYSUTCDATETIME()`. Updated by application code on every write. |

### `validation_audit_log`

| Column | Type | Notes |
|---|---|---|
| `id` | UNIQUEIDENTIFIER | PK. Default `NEWID()`. |
| `file_name` | NVARCHAR(255) | Uploaded file name. |
| `user_id` | NVARCHAR(255) | From EasyAuth claims. |
| `user_email` | NVARCHAR(255) | From EasyAuth claims. |
| `validated_at` | DATETIME2 | Default `SYSUTCDATETIME()`. |
| `overall_result` | NVARCHAR(10) | CHECK: `PASS`, `FAIL`, `ERROR`. |
| `rules_evaluated` | NVARCHAR(MAX) | JSON snapshot of rules at validation time. |
| `ai_response` | NVARCHAR(MAX) | Raw Azure OpenAI response(s). |
| `scopes_evaluated` | NVARCHAR(100) | Comma-separated scopes evaluated (reflects short-circuit). |

#### `rules_evaluated` JSON Schema

```json
[
  {
    "id": "<uuid>",
    "name": "<string>",
    "rule_text": "<string>",
    "scope": "FILE | HEADER | FOOTER | RECORD",
    "file_type": "CSV | XLSX | ALL"
  }
]
```

#### `ai_response` JSON Schema

```json
[
  {
    "scope": "FILE | HEADER | FOOTER | RECORD",
    "batchIndex": 0,
    "response": { "results": [ { "rule_id": "<uuid>", "passed": true, "reason": "<string>" } ] }
  }
]
```

## API Endpoints

| Method | Path | Role | Description |
|---|---|---|---|
| GET | `/api/validation-rules` | Admin | List rules. Supports `?is_active=true`. |
| POST | `/api/validation-rules` | Admin | Create rule. |
| PUT | `/api/validation-rules/{id}` | Admin | Update rule. |
| DELETE | `/api/validation-rules/{id}` | Admin | Delete rule. |
| GET | `/api/me` | Any authenticated | Returns current user identity and role from EasyAuth claims. |
| POST | `/api/cds/upload/validate` | Finance User | Upload file (multipart) + trigger validation. Returns JSON below. |

### HTTP Status Codes

| Code | Meaning |
|---|---|
| `200 OK` | Validation completed. Check `status` for `PASS`, `FAIL`, or `ERROR`. |
| `400 Bad Request` | File too large, unsupported type, or missing file. |
| `401 Unauthorized` | EasyAuth headers missing or invalid. |
| `403 Forbidden` | User role does not match required role for endpoint. |
| `500 Internal Server Error` | Unhandled server error. |

CRUD: `POST` → `201` + body; `PUT` → `200` + body; `DELETE` → `204`; `GET` → `200` + array; `404` if ID not found (PUT/DELETE).

### Rule Create/Update Request Body

`id`, `created_at`, `updated_at` are server-generated — omit from request.

```json
{
  "name": "<string, required, max 255>",
  "description": "<string, optional>",
  "rule_text": "<string, required>",
  "scope": "FILE | HEADER | FOOTER | RECORD",
  "file_type": "CSV | XLSX | ALL",
  "is_active": true
}
```

Validation:
- `name`, `rule_text`, `scope`, `file_type` are required. Return `400` if missing or empty.
- `scope` must be one of `FILE`, `HEADER`, `FOOTER`, `RECORD`. Return `400` otherwise.
- `file_type` must be one of `CSV`, `XLSX`, `ALL`. Return `400` otherwise.
- `is_active` defaults to `true` if omitted on create.

### `/api/me` Response Contract

```json
{
  "userId": "<string>",
  "email": "<string>",
  "role": "Admin | FinanceUser"
}
```

### Validation Response Contract

```json
{
  "status": "PASS | FAIL | ERROR",
  "rulesEvaluated": 5,
  "rulesFailed": 2,
  "scopeShortCircuitedAt": "RECORD | null",
  "failures": [
    {
      "ruleId": "<uuid>",
      "ruleName": "No negative amounts",
      "scope": "RECORD",
      "reason": "Row 2 contains a negative Amount value: -500.00"
    }
  ]
}
```

## Error Handling

| Scenario | Behaviour |
|---|---|
| Azure OpenAI timeout (default >30s) | `ERROR`. _"Validation service timeout. Please retry."_ |
| Malformed JSON response | `ERROR`. _"Validation service returned an unexpected response."_ |
| No active rules for file type | `PASS`, proceed with ingestion, log warning. |
| Rate-limit / quota error | `ERROR`. _"Validation service is temporarily unavailable. Please retry."_ Do not ingest. |
| File >10 MB | Reject pre-parse (`400`). _"File exceeds the maximum allowed size of 10 MB."_ |
| Unsupported file type (including legacy `.xls`) | Reject pre-parse (`400`). _"Only CSV and XLSX files are supported."_ |
| File parse error | `ERROR`. _"The uploaded file could not be parsed. Please verify the file format."_ |
