# Testing & Acceptance

## Backend Testing

- **Framework:** xUnit + NSubstitute
- **AI mocking:** Abstract Azure OpenAI behind `IValidationAiService`. Tests inject mock returning pre-recorded JSON fixtures from `Tests/Fixtures/`.
- **Unit tests:** Validation engine logic, file parsing, rule filtering, prompt construction, response parsing, error handling.
- **Integration tests:** API endpoints with test DB + mocked AI service. No live Azure OpenAI calls in CI.
- **Optional E2E:** Run locally against real Azure OpenAI by setting `ENABLE_LIVE_AI_TESTS=true`.

## Frontend Testing

- **Framework:** Vitest + React Testing Library
- **Coverage:** Component rendering, form validation, API call mocking, error panel display.

## Sample Rules

| Scope | Name | Rule Text | File Type |
|---|---|---|---|
| FILE | Minimum data rows | "The file must contain at least one data row beyond the header" | ALL |
| HEADER | Required columns | "The first row must contain these exact column headers: Date, Account, Amount, Currency, Description" | CSV |
| FOOTER | Footer total check | "The last row must contain a total that equals the sum of the Amount column" | CSV |
| RECORD | No negative amounts | "The Amount column must not contain negative values" | ALL |
| RECORD | Valid currency codes | "The Currency column must contain only valid ISO 4217 codes (e.g., USD, EUR, GBP)" | ALL |

## Sample Valid CSV

```csv
Date,Account,Amount,Currency,Description
2026-01-15,ACC001,1500.00,USD,Q1 Revenue
2026-01-16,ACC002,2300.50,EUR,Q1 Expenses
TOTAL,,,3800.50,
```

## Sample Invalid CSV — RECORD Failure

```csv
Date,Account,Amount,Currency,Description
2026-01-15,ACC001,-500.00,USD,Q1 Adjustment
2026-01-16,ACC002,2300.50,EUR,Q1 Expenses
TOTAL,,,1800.50,
```

**Expected AI response (RECORD scope):**

```json
{
  "results": [
    { "rule_id": "<no-negative-amounts-uuid>", "passed": false, "reason": "Row 2 contains a negative Amount value: -500.00" },
    { "rule_id": "<valid-currency-codes-uuid>", "passed": true, "reason": "All Currency values are valid ISO 4217 codes." }
  ]
}
```

## Sample Invalid CSV — FOOTER Failure (Short-Circuit)

```csv
Date,Account,Amount,Currency,Description
2026-01-15,ACC001,1500.00,USD,Q1 Revenue
2026-01-16,ACC002,2300.50,EUR,Q1 Expenses
TOTAL,,,9999.00,
```

**Expected AI response (FOOTER scope — RECORD skipped):**

```json
{
  "results": [
    { "rule_id": "<footer-total-check-uuid>", "passed": false, "reason": "Footer total 9999.00 does not match the sum of the Amount column (3800.50)." }
  ]
}
```

## Acceptance Criteria

- [ ] Admin creates rule in UI → applied to next upload without deploy
- [ ] Finance user upload violating rules → blocked, inline per-rule error report shown
- [ ] `INotificationService` called with correct payload on failure
- [ ] Passing file → `IIngestionService` called
- [ ] Every validation run logged to audit table with AI response snapshot
- [ ] Disabling rule → excluded from subsequent validations immediately
- [ ] Short-circuit: FILE failure skips HEADER/FOOTER/RECORD
- [ ] File >10 MB rejected before parsing
- [ ] RECORD scope validates all rows via batching
- [ ] AI deployment name is configurable, not hardcoded
