# Frontend

React 18 + Vite + TypeScript. MUI for components. Tailwind for utility CSS. React Router for client-side routing. `loglevel` for structured browser logging (`warn` in prod, `debug` in dev).

## Dev Proxy & CORS

- **Local dev:** Vite dev server proxies `/api/*` requests to the .NET backend (configured in `vite.config.ts`).
- **Production:** Frontend is served from the same origin (or CORS is configured on the backend to allow the frontend origin).
- **Backend CORS:** The .NET API registers CORS policy for the frontend origin. Configured via `appsettings.json` (not hardcoded).

## Role Resolution

- On app load, the frontend calls `GET /api/me` to retrieve the current user's identity and role.
- The response determines which routes are accessible:
  - `Admin` → `/admin/validation-rules` and `/upload`
  - `FinanceUser` → `/upload` only
- If the call fails (401), redirect to a "not authenticated" message.
- Store role in React context for route guards.

## Admin Rule Management UI

- Route: `/admin/validation-rules`
- Role-gated to Admin (check EasyAuth claims)
- CRUD for validation rules via API (`/api/validation-rules`)
- Rule fields:

| Field | Type | UI Notes |
|---|---|---|
| `name` | string | Required. Text input. |
| `description` | string | Optional. Text area. |
| `rule_text` | string | Required. Text area. |
| `scope` | enum | Required. Select: `FILE`, `HEADER`, `FOOTER`, `RECORD`. |
| `file_type` | enum | Required. Select: `CSV`, `XLSX`, `ALL`. |
| `is_active` | boolean | Toggle switch. |

- Table view of all rules with edit/delete actions and active toggle.
- Inline validation of required fields before save.
## Upload & Validation UI

- Route: `/upload`
- Role-gated to Finance User
- File upload form: accepts `.csv` and `.xlsx` (legacy `.xls` not supported)
- Calls `POST /api/cds/upload/validate` with file as multipart form data
- Show loading state during validation

### Validation Success

- Display success confirmation message.

### Validation Failure — Error Panel

- Display inline (do not navigate away).
- **Summary:** rules evaluated count, failed count, which scope failed.
- **Failed rules table:** Rule Name | Scope | Reason (from Azure OpenAI).
### Validation Error — System Error Banner

- Displayed when response `status` is `ERROR` (AI timeout, parse failure, rate limit, etc.).
- Show a warning-styled banner (not the failed-rules table): _"Validation could not be completed. Please retry."_
- Include the error message from the response if available.
- File not ingested. User may retry without modifying the file.
