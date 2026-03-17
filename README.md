# Data Validation Engine

An AI-driven validation engine for CDS (Corporate Data System) file uploads. Finance users upload CSV or XLSX files, and Azure OpenAI evaluates natural-language validation rules at runtime. Files that fail validation are blocked from downstream ingestion, with structured error reports displayed inline and failure notifications sent to the uploading user by email.

## Table of Contents

- [Features](#features)
- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Database Setup](#database-setup)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Testing](#testing)
- [API Reference](#api-reference)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **AI-interpreted validation rules** — Natural-language rules are evaluated by Azure OpenAI at runtime; no hardcoded logic required.
- **Admin rule management** — Create, update, and delete validation rules through the Admin UI without any code deployment.
- **All-or-nothing validation** — A single failing rule blocks the entire file from ingestion.
- **Structured error reports** — Validation results are surfaced inline in the UI.
- **Email notifications** — The uploading user is notified by email when a file fails validation.
- **Supported file formats** — CSV and XLSX (`.xls` legacy format is not supported).

---

## Architecture Overview

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10 Minimal API (C#) |
| Database | SQL Server via Dapper |
| AI Service | Azure OpenAI (Chat Completions API) |
| Authentication | Azure EasyAuth (`X-MS-CLIENT-PRINCIPAL` header) |
| Frontend | React 18 + TypeScript + Vite |
| UI Components | Material UI (MUI) v7 |
| Styling | Tailwind CSS v4 |

**Validation flow:**
1. File upload received and pre-validated (size ≤ 10 MB, supported type).
2. File parsed into rows (CSV via CsvHelper, XLSX via ClosedXML).
3. Active rules loaded and filtered by file type (`CSV`, `XLSX`, `ALL`).
4. Rules grouped and evaluated per scope: `FILE` → `HEADER` → `FOOTER` → `RECORD`.
5. Azure OpenAI receives one API call per scope (RECORD scope is batched if the token budget is exceeded).
6. **Short-circuit:** The first failing scope stops evaluation of subsequent scopes.
7. Results are written to the `validation_audit_log` table.
8. On `PASS` the file is forwarded to the ingestion service; on `FAIL` the user is notified by email.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) with npm
- SQL Server (local instance or a connection string to a remote server)
- Azure OpenAI resource (endpoint + deployment name) — required for live validation; a stub is used automatically when the endpoint is not configured.

---

## Getting Started

### Database Setup

Apply the migration scripts in the `db/` directory in order against your SQL Server database:

```bash
# Using sqlcmd or SQL Server Management Studio
sqlcmd -S <server> -d <database> -i db/001_create_validation_rules.sql
sqlcmd -S <server> -d <database> -i db/002_create_validation_audit_log.sql
```

### Backend Setup

```bash
cd src/backend

# Restore and build
dotnet build DataValidationEngine.Api/DataValidationEngine.Api.csproj
```

Create a local configuration override at `src/backend/DataValidationEngine.Api/appsettings.Development.json` (this file is git-ignored):

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DataValidationEngine;Trusted_Connection=true;"
  }
}
```

> **Note:** If `AzureOpenAI:Endpoint` is not set, the application uses a stub AI service and no real Azure OpenAI calls are made — useful for local UI development without Azure credentials.

### Frontend Setup

```bash
cd src/frontend

# Install dependencies
npm install
```

---

## Configuration

The following settings can be customised in `appsettings.json` (or overridden via `appsettings.Development.json` / environment variables):

| Key | Default | Description |
|-----|---------|-------------|
| `AzureOpenAI:Endpoint` | _(empty)_ | Azure OpenAI resource endpoint URL |
| `AzureOpenAI:DeploymentName` | `gpt-4o` | Model deployment name |
| `AzureOpenAI:MaxTokensPerRequest` | `128000` | Token budget per AI call |
| `ConnectionStrings:DefaultConnection` | _(empty)_ | SQL Server connection string |
| `FileProcessing:MaxFileSizeBytes` | `10485760` | Maximum upload size (10 MB) |
| `FileProcessing:BatchRowThreshold` | `500` | Rows per RECORD-scope batch |
| `FileProcessing:HeaderRowCount` | `1` | Rows to treat as header |
| `FileProcessing:FooterRowCount` | `1` | Rows to treat as footer |
| `Validation:TimeoutSeconds` | `30` | Per-scope AI call timeout |
| `Cors:AllowedOrigins` | `["http://localhost:5173"]` | Frontend origin(s) allowed by CORS |

In production, configure secrets via Azure App Service Configuration settings. The application uses `DefaultAzureCredential` (Managed Identity) to authenticate with Azure OpenAI — no credentials need to be stored in the configuration.

---

## Running the Application

### Backend

```bash
cd src/backend/DataValidationEngine.Api
dotnet run
```

The API listens on **http://localhost:5225**.

In development mode, EasyAuth is simulated automatically: a fake `X-MS-CLIENT-PRINCIPAL` header is injected with an `Admin` role, so you can use all endpoints without setting up Azure AD.

### Frontend

```bash
cd src/frontend
npm run dev
```

The Vite dev server starts on **http://localhost:5173** and proxies all `/api/*` requests to the backend at `http://localhost:5225`.

Open [http://localhost:5173](http://localhost:5173) in your browser. You will be logged in automatically with the `Admin` role in development mode.

### Production Build (Frontend)

```bash
cd src/frontend
npm run build
# Optimized output is written to src/frontend/dist/
```

---

## Testing

### Backend Tests

```bash
cd src/backend/DataValidationEngine.Tests
dotnet test
```

To run tests that call the real Azure OpenAI service (requires valid credentials in your environment):

```bash
ENABLE_LIVE_AI_TESTS=true dotnet test
```

### Frontend Tests

```bash
cd src/frontend
npm run test
```

### Frontend Linting

```bash
cd src/frontend
npm run lint
```

---

## API Reference

| Method | Path | Role | Description |
|--------|------|------|-------------|
| `GET` | `/api/me` | Any | Returns the current user's identity and role |
| `GET` | `/api/validation-rules` | Admin | Lists all rules (`?is_active=true` to filter) |
| `POST` | `/api/validation-rules` | Admin | Creates a new validation rule |
| `PUT` | `/api/validation-rules/{id}` | Admin | Updates an existing rule |
| `DELETE` | `/api/validation-rules/{id}` | Admin | Deletes a rule |
| `POST` | `/api/cds/upload/validate` | Finance User | Uploads a CSV/XLSX file and runs validation |

Full request/response schemas are documented in [`docs/data-model-api.md`](docs/data-model-api.md).

---

## Project Structure

```
data-validation-engine/
├── db/                          # SQL Server migration scripts
├── docs/                        # Architecture and specification docs
│   ├── specification.md
│   ├── architecture.md
│   ├── validation-engine.md
│   ├── data-model-api.md
│   ├── frontend.md
│   └── testing.md
├── src/
│   ├── backend/
│   │   ├── DataValidationEngine.Api/     # Minimal API entry point & endpoints
│   │   ├── DataValidationEngine.Core/    # Domain services, models, interfaces
│   │   └── DataValidationEngine.Tests/   # xUnit unit & integration tests
│   └── frontend/                         # React 18 + Vite + TypeScript UI
└── tests/                       # Sample test data and helper scripts
```

---

## Contributing

1. Fork the repository and create a feature branch from `main`.
2. Follow the existing coding conventions (C# Minimal API patterns, React functional components with TypeScript).
3. Add or update tests for every behavior change.
4. Ensure all existing tests pass before opening a pull request:
   - Backend: `dotnet test src/backend/DataValidationEngine.Tests`
   - Frontend: `npm run lint && npm run test` (from `src/frontend`)
5. Open a pull request with a clear description of the changes and the problem they solve.

---

## License

This project is licensed under the [MIT License](LICENSE).
