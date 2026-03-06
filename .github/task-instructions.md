# Task Instructions

## Task
Scaffold and implement the React frontend with Vite, TypeScript, MUI, Tailwind CSS, and React Router — including Admin rule management, file upload/validation, and role-based routing.

## Scope
- `src/frontend/` — Vite + React 18 + TypeScript scaffold
- `src/frontend/src/services/api.ts` — API client for all backend endpoints
- `src/frontend/src/context/AuthContext.tsx` — Auth context fetching `/api/me` on app load
- `src/frontend/src/pages/AdminRulesPage.tsx` — CRUD for validation rules (table + form)
- `src/frontend/src/pages/UploadPage.tsx` — File upload with validation result display
- `src/frontend/src/pages/NotAuthenticatedPage.tsx` — 401 fallback
- `src/frontend/src/components/RuleForm.tsx` — Rule create/edit form with validation
- `src/frontend/src/components/ValidationResult.tsx` — Inline error panel / success message
- `src/frontend/src/App.tsx` — React Router with role-based route guards
- `vite.config.ts` — Proxy `/api/*` to backend

## Out of Scope
- Frontend tests (next task)
- Production deployment config
- EasyAuth integration testing

## Acceptance Criteria
- [ ] `npm install && npm run build` succeeds
- [ ] Admin rules page: table with all rules, create/edit/delete, active toggle
- [ ] Upload page: file picker, multipart upload, loading state, inline error panel
- [ ] Role-based routing: Admin sees rules + upload, FinanceUser sees upload only
- [ ] Auth context loads user from `/api/me`; 401 shows not-authenticated page
- [ ] Vite proxy config routes `/api/*` to `http://localhost:5000`
- [ ] SUCCESS: green confirmation; FAIL: red table with rule failures; ERROR: yellow warning banner

## Validation
- [ ] `npm run build` exits 0 in `src/frontend/`
