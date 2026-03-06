# Architecture & Configuration

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 18 + Vite + TypeScript |
| UI Components | MUI (Material UI) |
| Routing | React Router |
| CSS | Tailwind CSS |
| Backend | .NET 10 Minimal API |
| Database | SQL Server (Dapper, no ORM) |
| AI | Azure OpenAI (Chat Completions API) |
| AI Auth | `Azure.AI.OpenAI` + `Azure.Identity` → `DefaultAzureCredential` |
| App Auth | Azure EasyAuth (headers: `X-MS-CLIENT-PRINCIPAL`) |
| CSV Parsing | CsvHelper |
| XLSX Parsing | ClosedXML (MIT, `.xlsx` only — legacy `.xls` not supported) |
| Backend Logging | `ILogger<T>` — console + structured output |
| Frontend Logging | `loglevel` — lightweight structured browser logging |
| Backend Tests | xUnit + NSubstitute |
| Frontend Tests | Vitest + React Testing Library |

## Project Structure

```
data-validation-engine/
├── src/
│   ├── backend/
│   │   ├── DataValidationEngine.Api/         # Minimal API host (Program.cs, Endpoints/, appsettings.json)
│   │   ├── DataValidationEngine.Core/        # Domain logic
│   │   │   ├── Models/
│   │   │   ├── Interfaces/                   # IValidationAiService, INotificationService, etc.
│   │   │   ├── Services/                     # ValidationEngine, RuleService, FileParser
│   │   │   └── Stubs/                        # Stub implementations
│   │   └── DataValidationEngine.Tests/
│   │       └── Fixtures/                     # Pre-recorded AI response JSON files
│   └── frontend/
│       ├── src/
│       │   ├── components/
│       │   ├── pages/                        # Admin rules page, Upload page
│       │   ├── services/                     # API client
│       │   └── App.tsx
│       ├── package.json
│       └── vite.config.ts
├── db/                                       # SQL migration scripts (001_, 002_, ...)
├── docs/
└── README.md
```

## Stub Interfaces

Stubs log calls only.

```csharp
public interface INotificationService
{
    Task SendValidationFailureEmailAsync(
        string recipientEmail,
        string fileName,
        DateTime uploadTimestamp,
        List<RuleFailureDetail> failures,
        string uploadPageUrl);
}

public interface IIngestionService
{
    Task IngestFileAsync(string fileName);
}
```

Stub `IIngestionService` logs the file name. No `IFileUploadService` — files arrive as multipart uploads directly to the API.

## Core Service Interfaces

```csharp
public interface IValidationAiService
{
    Task<AiValidationResponse> ValidateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

public record AiValidationResponse(List<AiRuleResult> Results);
public record AiRuleResult(Guid RuleId, bool Passed, string Reason);
public record RuleFailureDetail(Guid RuleId, string RuleName, string Scope, string Reason);
```

## Configuration

### `appsettings.json` (committed, non-sensitive)

```json
{
  "AzureOpenAI": {
    "Endpoint": "",
    "DeploymentName": "gpt-4o",
    "MaxTokensPerRequest": 128000
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "FileProcessing": {
    "HeaderRowCount": 1,
    "FooterRowCount": 1,
    "BatchRowThreshold": 500,
    "MaxFileSizeBytes": 10485760
  },
  "Validation": {
    "TimeoutSeconds": 30
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

`appsettings.Development.json` (gitignored): overrides `AzureOpenAI:Endpoint`, `AzureOpenAI:DeploymentName`, `ConnectionStrings:DefaultConnection`.

Production: `AzureOpenAI:Endpoint` and `ConnectionStrings:DefaultConnection` via App Service Configuration. AI auth via Managed Identity (`DefaultAzureCredential`).

## Logging

### Backend
- `ILogger<T>` throughout. No third-party logging frameworks.
- `Information`: flow milestones. `Warning`: recoverable issues. `Error`: failures.
- Structured message templates (rule IDs, file names, scopes, durations). No string interpolation.
- Console locally; production via App Service (e.g., Application Insights).

### Frontend
- `loglevel`: `warn` in production, `debug` in development.
