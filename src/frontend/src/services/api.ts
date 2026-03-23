import log from 'loglevel';

const API_BASE = '/api';

log.setLevel(import.meta.env.DEV ? 'debug' : 'warn');

export interface UserInfo {
  userId: string;
  email: string;
  role: 'Admin' | 'FinanceUser';
}

export interface ValidationRule {
  id: string;
  name: string;
  description?: string;
  ruleText: string;
  scope: string;
  fileType: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface RuleCreateRequest {
  name: string;
  description?: string;
  ruleText: string;
  scope: string;
  fileType: string;
  isActive: boolean;
}

export interface RuleFailureDetail {
  ruleId: string;
  ruleName: string;
  scope: string;
  reason: string;
}

export interface ValidationResponse {
  status: 'PASS' | 'FAIL' | 'ERROR';
  rulesEvaluated: number;
  rulesFailed: number;
  scopeShortCircuitedAt?: string | null;
  failures: RuleFailureDetail[];
}

export interface DatabaseInitializationResponse {
  databaseCreated: boolean;
  tablesCreated: number;
  sampleRulesInserted: number;
  sampleRulesSkipped: number;
  sampleRulesTotal: number;
}

export interface DatabaseStatusResponse {
  exists: boolean;
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text();
    log.error(`API error ${res.status}: ${text}`);
    throw new Error(text || `HTTP ${res.status}`);
  }
  return res.json();
}

export async function getMe(): Promise<UserInfo> {
  const res = await fetch(`${API_BASE}/me`);
  return handleResponse<UserInfo>(res);
}

export async function getRules(isActive?: boolean): Promise<ValidationRule[]> {
  const params = isActive !== undefined ? `?is_active=${isActive}` : '';
  const res = await fetch(`${API_BASE}/validation-rules${params}`);
  return handleResponse<ValidationRule[]>(res);
}

export async function createRule(rule: RuleCreateRequest): Promise<ValidationRule> {
  const res = await fetch(`${API_BASE}/validation-rules`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(rule),
  });
  return handleResponse<ValidationRule>(res);
}

export async function updateRule(id: string, rule: RuleCreateRequest): Promise<ValidationRule> {
  const res = await fetch(`${API_BASE}/validation-rules/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(rule),
  });
  return handleResponse<ValidationRule>(res);
}

export async function deleteRule(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/validation-rules/${id}`, { method: 'DELETE' });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
}

export async function initializeDatabase(): Promise<DatabaseInitializationResponse> {
  const res = await fetch(`${API_BASE}/admin/database/initialize`, {
    method: 'POST',
  });
  return handleResponse<DatabaseInitializationResponse>(res);
}

export async function getDatabaseStatus(): Promise<DatabaseStatusResponse> {
  const res = await fetch(`${API_BASE}/database/status`);
  return handleResponse<DatabaseStatusResponse>(res);
}

export async function uploadAndValidate(file: File): Promise<ValidationResponse> {
  const formData = new FormData();
  formData.append('file', file);
  const res = await fetch(`${API_BASE}/cds/upload/validate`, {
    method: 'POST',
    body: formData,
  });
  return handleResponse<ValidationResponse>(res);
}
