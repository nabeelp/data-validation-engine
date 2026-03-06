# CDS AI-Driven Validation Engine

Greenfield. Stubs for external dependencies (notification, ingestion). AI-driven validation for CDS file uploads — Finance users upload CSV/XLSX, natural-language rules evaluated at runtime by Azure OpenAI, failing files blocked from ingestion.

## Goals

- Azure OpenAI interprets natural-language rules at runtime (no hard-coded/SQL validation)
- Admin UI for rule CRUD without code deploys
- Block entire file on any validation failure
- Structured error reports inline + email notification

## Non-Goals

- Stage-2 / business-logic validations
- Partial upload of valid rows
- Email to anyone other than the uploading user
- Changes to downstream ingestion after validation passes
- Custom auth logic (EasyAuth handles this)
- AI-generated rule suggestions or auto-remediation
- File formats beyond CSV and XLSX (legacy `.xls` is not supported)
- Multi-tenant rule isolation
- Cost guardrails (Azure OpenAI quotas are sufficient)

## Roles

| Role | Permissions |
|---|---|
| Finance User | Upload files, view inline errors, receive failure emails |
| Admin | CRUD validation rules |

Auth: Azure EasyAuth. App reads `X-MS-CLIENT-PRINCIPAL` headers. No custom auth middleware.

### EasyAuth Claim Mapping

`X-MS-CLIENT-PRINCIPAL` is a base64-encoded JSON payload with a `claims` array.

| Field | Claim Type |
|---|---|
| `userId` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` |
| `email` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` |
| `role` | `roles` — Entra ID app role: `Admin` or `FinanceUser` |

## Spec Files

| File | Load When |
|---|---|
| [specification.md](specification.md) | Always |
| [architecture.md](architecture.md) | Scaffolding, config, DI, project structure |
| [validation-engine.md](validation-engine.md) | Core validation logic, AI integration, file parsing |
| [data-model-api.md](data-model-api.md) | Database schema, API endpoints, error handling |
| [frontend.md](frontend.md) | React UI |
| [testing.md](testing.md) | Test strategy, sample data, acceptance criteria |
