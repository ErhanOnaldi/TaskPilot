import type { AuthResponse, ServiceResult } from './contracts';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:18080';
const REFRESH_TOKEN_KEY = 'taskpilot-refresh-token';
const SESSION_USER_KEY = 'taskpilot-session-user';

function readSessionValue(key: string): string | null {
  try {
    return window.sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

function readSessionUser(): AuthResponse['user'] | null {
  const raw = readSessionValue(SESSION_USER_KEY);
  if (!raw) return null;
  try {
    const value = JSON.parse(raw) as Partial<AuthResponse['user']>;
    return typeof value.id === 'number' && typeof value.email === 'string'
      ? { id: value.id, email: value.email }
      : null;
  } catch {
    return null;
  }
}

let accessToken: string | null = null;
let refreshToken: string | null = readSessionValue(REFRESH_TOKEN_KEY);
let sessionUser: AuthResponse['user'] | null = readSessionUser();
let refreshInFlight: Promise<boolean> | null = null;
const sessionListeners = new Set<() => void>();

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly correlationId?: string,
    readonly retryAfter?: number,
    readonly details?: string[],
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function setSession(auth: AuthResponse | null) {
  accessToken = auth?.accessToken ?? null;
  refreshToken = auth?.refreshToken ?? null;
  sessionUser = auth?.user ?? null;
  try {
    if (auth) {
      window.sessionStorage.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
      window.sessionStorage.setItem(SESSION_USER_KEY, JSON.stringify(auth.user));
    } else {
      window.sessionStorage.removeItem(REFRESH_TOKEN_KEY);
      window.sessionStorage.removeItem(SESSION_USER_KEY);
    }
  } catch {
    // Memory-only auth remains usable when browser storage is unavailable.
  }
  sessionListeners.forEach((listener) => listener());
}

export function getSessionUser() { return sessionUser; }

export function hasSession() {
  return Boolean(accessToken || refreshToken);
}

export function subscribeSession(listener: () => void) {
  sessionListeners.add(listener);
  return () => {
    sessionListeners.delete(listener);
  };
}

export async function checkApiHealth(signal?: AbortSignal): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/health/ready`, {
      headers: { Accept: 'text/plain' },
      ...(signal ? { signal } : {}),
    });
    if (!response.ok) return false;
    return (await response.text()).trim().toLowerCase() === 'healthy';
  } catch {
    return false;
  }
}

async function refreshSession(): Promise<boolean> {
  if (!refreshToken) return false;
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = fetch(`${API_BASE_URL}/api/auth/refresh-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
    .then(async (response) => {
      if (!response.ok) {
        setSession(null);
        return false;
      }
      const result = (await response.json()) as ServiceResult<AuthResponse>;
      if (!result.data) {
        setSession(null);
        return false;
      }
      setSession(result.data);
      return true;
    })
    .finally(() => {
      refreshInFlight = null;
    });

  return refreshInFlight;
}

async function mapError(response: Response): Promise<ApiError> {
  const correlationId = response.headers.get('x-correlation-id') ?? undefined;
  const retryHeader = response.headers.get('retry-after');
  const retryAfter = retryHeader ? Number.parseInt(retryHeader, 10) : undefined;
  let title = response.statusText || 'İstek tamamlanamadı';
  let details: string[] | undefined;

  try {
    const payload = (await response.json()) as {
      title?: string;
      detail?: string;
      message?: string;
      errorMessages?: string[];
      errors?: Record<string, string[]>;
    };
    title = payload.title ?? payload.message ?? payload.detail ?? payload.errorMessages?.[0] ?? title;
    details = payload.errorMessages ?? (payload.errors ? Object.values(payload.errors).flat() : undefined);
  } catch {
    // Non-JSON failures still receive a safe, status-based message.
  }

  return new ApiError(title, response.status, correlationId, retryAfter, details);
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  retryAuth = true,
): Promise<T> {
  const headers = new Headers(init.headers);
  if (!headers.has('Content-Type') && init.body) headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });
  if (response.status === 401 && retryAuth && (await refreshSession())) {
    return apiRequest<T>(path, init, false);
  }
  if (!response.ok) throw await mapError(response);
  if (response.status === 204) return undefined as T;

  const payload = (await response.json()) as ServiceResult<T> | T;
  if (typeof payload === 'object' && payload !== null && 'data' in payload) {
    const result = payload as ServiceResult<T>;
    if (result.errorMessages?.length) {
      throw new ApiError(result.errorMessages[0] ?? 'İstek tamamlanamadı', response.status, undefined, undefined, result.errorMessages);
    }
    return result.data as T;
  }
  return payload as T;
}

export async function apiSse(
  path: string,
  body: unknown,
  signal: AbortSignal,
  onEvent: (event: string, data: unknown) => void,
  retryAuth = true,
): Promise<void> {
  const headers = new Headers({ 'Content-Type': 'application/json', Accept: 'text/event-stream' });
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(`${API_BASE_URL}${path}`, { method: 'POST', headers, body: JSON.stringify(body), signal });
  if (response.status === 401 && retryAuth && (await refreshSession())) return apiSse(path, body, signal, onEvent, false);
  if (!response.ok) throw await mapError(response);
  if (!response.body) throw new ApiError('Streaming yanıtı alınamadı.', 502);

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  while (true) {
    const { value, done } = await reader.read();
    buffer += decoder.decode(value, { stream: !done });
    const frames = buffer.split('\n\n');
    buffer = frames.pop() ?? '';
    for (const frame of frames) {
      let event = 'message';
      let data = '';
      frame.split('\n').forEach((line) => {
        if (line.startsWith('event:')) event = line.slice(6).trim();
        if (line.startsWith('data:')) data += line.slice(5).trim();
      });
      if (data) onEvent(event, JSON.parse(data) as unknown);
    }
    if (done) break;
  }
}

export const apiConfig = { baseUrl: API_BASE_URL };
